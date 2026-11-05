using Newtonsoft.Json.Linq;
using Npgsql;
using SkillUp.Models;

namespace SkillUp.Services;

/// <summary>What a command left behind: its result, and the data and account as they are after it ran.</summary>
public sealed record CommandOutcome(object Data, Snapshot Snapshot, UserRow Me);

/// <summary>
/// Runs one command (Commands.Run) the way every caller needs it: one SQL transaction, one command at a time, and the sign-in or
/// visitor cookie the command asked for issued after it committed. A refused command throws ApiException and writes nothing.
/// </summary>
public static class CommandRunner
{
    /// <summary>The site is a single process, so commands run one at a time and never race. Several servers would need database-level checks instead.</summary>
    static readonly object Gate = new();

    public static async Task<CommandOutcome> Run(HttpContext http, Snapshot data, UserRow me, string action, JObject body)
    {
        var source = http.RequestServices.GetRequiredService<NpgsqlDataSource>();
        // Gemini answers in seconds, so ask it before taking the gate that serialises every command.
        var aiReply = action == "ai" ? Companion.Reply(data, me, (string)body["text"], Companion.LessonOf(body)) : null;

        Snapshot reloaded;
        Commands commands;
        object result;
        lock (Gate)
        {
            using var db = new Db(source);
            db.Begin();
            commands = new Commands(db, Snapshot.Load(db), me)
            {
                VisitorKey = VisitorCookie.Read(http),
                AiReply = aiReply,
                ClientIp = http.Connection.RemoteIpAddress?.ToString()
            };
            result = commands.Run(action, body);
            db.Commit();
            reloaded = Snapshot.Load(db);
        }

        if (commands.NewVisitorKey != null) VisitorCookie.Issue(http, commands.NewVisitorKey);
        if (commands.SignOut) await AuthCookie.Clear(http);
        if (commands.SignIn != null) await AuthCookie.Issue(http, reloaded.User(commands.SignIn.Value));

        var after = commands.SignOut ? null : reloaded.User(commands.SignIn ?? commands.Me?.Id);
        if (result is Deferred deferred) result = deferred(reloaded);
        return new CommandOutcome(result, reloaded, after);
    }
}
