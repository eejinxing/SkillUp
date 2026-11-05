using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SkillUp.Services
{
    /// <summary>
    /// Call signalling for js/call-preview.js: a small in-memory mailbox per signed-in user. A browser sends setup messages
    /// (invite, accept, offer, answer, ice, hangup...) to another user and polls for its own. The audio and video then go
    /// straight between the two browsers over WebRTC, so none of it passes through, or is stored by, the server.
    ///   POST /api/call/send  { to, type, call, data, video }
    ///   POST /api/call/poll  -> { messages: [ { from, type, call, data, video, name, role } ] }
    /// Nothing is kept in the database; a restart of the site simply drops any call in progress.
    /// </summary>
    static class CallSignals
    {
        static readonly string[] Types = { "invite", "accept", "decline", "offer", "answer", "ice", "hangup", "chat" };
        static readonly Dictionary<int, List<Letter>> Mailboxes = new Dictionary<int, List<Letter>>();
        static readonly TimeSpan Keep = TimeSpan.FromSeconds(90);
        const int MaxBody = 64 * 1024, MaxQueued = 200;

        sealed class Letter
        {
            public DateTime At = DateTime.UtcNow;
            public JObject Body;
        }

        public static async Task<JObject> Handle(HttpContext context, string op)
        {
            var source = context.RequestServices.GetRequiredService<NpgsqlDataSource>();
            int? me;
            using (var db = new Db(source)) me = AuthCookie.Resolve(context, db);
            if (me == null) throw new ApiException(401, "Sign in to make calls.");
            if (op == "poll") return new JObject { ["messages"] = new JArray(Take(me.Value)) };
            if (op != "send") throw new ApiException(404, "Not found.");

            // Read at most MaxBody characters, whatever the request claims its length is.
            JObject payload;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                var buffer = new char[MaxBody + 1];
                var count = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                if (count > MaxBody) throw new ApiException(413, "That call message is too large.");
                try { payload = JObject.Parse(new string(buffer, 0, count)); }
                catch (JsonReaderException) { throw new ApiException(400, "The request body is not valid JSON."); }
            }
            var type = (string)payload["type"];
            var to = (int?)payload["to"];
            if (to == null || to == me || !Types.Contains(type)) throw new ApiException(400, "That call message is not valid.");

            var message = new JObject
            {
                ["from"] = me.Value,
                ["type"] = type,
                ["call"] = Clip((string)payload["call"], 40),
                ["video"] = (bool?)payload["video"] ?? false,
                ["data"] = type == "offer" || type == "answer" || type == "ice" ? payload["data"] : type == "chat" ? Clip((string)payload["data"], 1000) : null
            };
            using (var db = new Db(source))
            {
                var model = Snapshot.Load(db);
                var caller = model.User(me.Value);
                var callee = model.User(to.Value);
                if (caller == null || caller.Status != "Active" || callee == null || callee.Status != "Active") throw new ApiException(404, "That person is not available.");
                // The name on an incoming call comes from the server, never from the caller's browser.
                if (type == "invite")
                {
                    message["name"] = caller.Name;
                    message["role"] = caller.Role;
                }
            }
            Put(to.Value, message);
            return new JObject { ["ok"] = true };
        }

        static string Clip(string text, int length) => text == null ? "" : text.Length > length ? text.Substring(0, length) : text;

        static void Put(int user, JObject body)
        {
            lock (Mailboxes)
            {
                List<Letter> box;
                if (!Mailboxes.TryGetValue(user, out box)) Mailboxes[user] = box = new List<Letter>();
                var cutoff = DateTime.UtcNow - Keep;
                box.RemoveAll(l => l.At < cutoff);
                if (box.Count < MaxQueued) box.Add(new Letter { Body = body });
            }
        }

        static List<JObject> Take(int user)
        {
            lock (Mailboxes)
            {
                List<Letter> box;
                if (!Mailboxes.TryGetValue(user, out box)) return new List<JObject>();
                var cutoff = DateTime.UtcNow - Keep;
                var fresh = box.Where(l => l.At >= cutoff).Select(l => l.Body).ToList();
                box.Clear();
                return fresh;
            }
        }
    }
}
