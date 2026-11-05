using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// Communication workspace: course announcement channels (?channel=, ?announcement=) with class comments,
    /// and private messages with one contact (?id=). Staff publish announcements for the courses they manage.
    /// </summary>
    public sealed class MessagesModel : PageModel
    {
        public MessagesModel(SkillContext ctx) : base(ctx) { }
        public IEnumerable<dynamic> ChannelList, ContactList, CommentList, AnnouncementList, CourseActivities;
        public IEnumerable<object> ComposeCourses;
        public bool CanCompose, ShowCompose;
        public string SearchTerm = "", CommentPlaceholder, MessagePlaceholder;


        static readonly string[] ChannelColours = { "#a3b0ce", "#b8a1c6", "#9fb6ae", "#baa38e" };
        public const string SendIcon = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12h13M13 6l6 6-6 6\"/></svg>";

        public List<CourseRow> Courses;
        public List<AnnouncementRow> ChannelAnnouncements;
        public AnnouncementRow Announcement;
        public CourseRow ActiveCourse;
        public UserRow Author, Selected;
        public int Channel, AnnouncementId, SelectedId, AllAnnouncementUnread, TotalUnread, Unread, ContactCount, CommentCount, ActivityCount;
        public string Thread;

        public void Load()
        {
            // Course channels: every course for Admin, otherwise enrolled (learner) or managed (staff) courses.
            Courses = Me.Role == "Admin" ? Data.Courses : Me.Role == "Learner" ? AccessibleCourses : OwnCourses;
            var ids = new HashSet<int>(Courses.Select(c => c.Id));
            Channel = ids.Contains(QueryInt("channel") ?? 0) ? QueryInt("channel").Value : 0;
            var all = Data.Announcements.Where(a => a.CourseId != null && ids.Contains(a.CourseId.Value)).ToList();
            ChannelAnnouncements = all.Where(a => Channel == 0 || a.CourseId == Channel).OrderByDescending(a => a.CreatedAt).ToList();
            Announcement = ChannelAnnouncements.FirstOrDefault(a => a.Id == QueryInt("announcement")) ?? ChannelAnnouncements.FirstOrDefault();
            AnnouncementId = Announcement?.Id ?? 0;
            Func<int?, int> unread = course => all.Count(a => (course == null || a.CourseId == course) && a.PostedBy != Me.Id && !Data.HasRead(Me.Id, a.Id));
            AllAnnouncementUnread = unread(null);
            ActiveCourse = Courses.FirstOrDefault(c => c.Id == (Announcement?.CourseId ?? Channel));
            Author = Announcement == null ? null : Data.User(Announcement.PostedBy);

            var contacts = Data.Users.Where(u => u.Id != Me.Id && u.Status == "Active").ToList();
            Selected = contacts.FirstOrDefault(u => u.Id == QueryInt("id")) ?? contacts.FirstOrDefault();
            SelectedId = Selected?.Id ?? 0;
            TotalUnread = Data.Messages.Count(m => m.ReceiverId == Me.Id && !m.Read && contacts.Any(c => c.Id == m.SenderId));

            ChannelList = Courses.Select((c, i) => new { Url = Url(c.Id, 0, SelectedId), c.Title, Colour = ChannelColours[i % 4], Count = unread(c.Id), Active = Channel == c.Id });
            CanCompose = Me.Role != "Learner" && Courses.Count > 0;
            if (Request.QueryString["compose"] == "1" && CanCompose)
            {
                ShowCompose = true;
                ComposeCourses = CourseOptions(Courses);
            }
            SearchTerm = Request.QueryString["q"] ?? "";

            BindContacts(contacts);
            BindAnnouncement();
            BindConversation();
        }

        /// <summary>The page address for a channel, announcement and contact, keeping the contact search.</summary>
        public string Url(int channel, int announcement, int contact)
        {
            var parts = new List<string> { "channel=" + channel };
            if (announcement != 0) parts.Add("announcement=" + announcement);
            if (contact != 0) parts.Add("id=" + contact);
            var q = Request.QueryString["q"];
            if (!string.IsNullOrEmpty(q)) parts.Add("q=" + System.Net.WebUtility.UrlEncode(q));
            return "/Messages?" + string.Join("&", parts);
        }

        static int Hue(int id) => id.ToString(CultureInfo.InvariantCulture).Sum(c => c) % 5;

        public string Face(UserRow u, string size) => "<span class=\"sc-face sc-hue-" + Hue(u.Id) + (size.Length > 0 ? " sc-face-" + size : "") + "\">" + Ui.Avatar(u) + "</span>";

        List<ChatRow> Conversation(int other) => Data.Messages.Where(m => (m.SenderId == Me.Id && m.ReceiverId == other) || (m.ReceiverId == Me.Id && m.SenderId == other)).OrderBy(m => m.SentAt).ToList();

        /// <summary>Contacts: most recent conversation first, then the rest alphabetically.</summary>
        void BindContacts(List<UserRow> contacts)
        {
            var term = (Request.QueryString["q"] ?? "").Trim().ToLowerInvariant();
            var rows = contacts.Where(c => SearchMatch.Matches(term, c.Name, c.Role)).Select(c =>
            {
                var last = Conversation(c.Id).LastOrDefault();
                return new { c, last, unread = Data.Messages.Count(m => m.ReceiverId == Me.Id && m.SenderId == c.Id && !m.Read) };
            }).OrderByDescending(x => x.last != null).ThenByDescending(x => x.last?.SentAt).ThenBy(x => x.c.Name).ToList();
            ContactCount = rows.Count;
            ContactList = rows.Select(x => new
            {
                Url = Url(Channel, AnnouncementId, x.c.Id),
                Active = x.c.Id == SelectedId,
                Unread = x.unread,
                FaceHtml = Face(x.c, ""),
                x.c.Name,
                TimeHtml = x.last == null ? "" : "<time>" + (x.last.SentAt.Date == DateTime.Today ? Ui.Time(x.last.SentAt) : x.last.SentAt.ToString("MMM d", CultureInfo.GetCultureInfo("en-US"))) + "</time>",
                PreviewHtml = x.last == null ? "<span class=\"sc-role\">" + Ui.H(x.c.Role) + "</span>" : (x.last.SenderId == Me.Id ? "<span class=\"sc-you\">You: </span>" : "") + Ui.H(x.last.Content)
            }).ToList();
        }

        void BindAnnouncement()
        {
            if (Announcement == null) return;
            var comments = Data.AnnouncementComments.Where(c => c.ParentId == Announcement.Id).ToList();
            CommentCount = comments.Count;
            CommentList = comments.Select(c =>
            {
                var u = Data.User(c.UserId);
                return new { c.Id, FaceHtml = u == null ? "" : Face(u, ""), Name = u?.Name ?? "Former user", Role = u?.Role ?? "Member", When = c.CreatedAt.ToString("d MMM, HH:mm", CultureInfo.GetCultureInfo("en-GB")), Text = c.Text, Mine = c.UserId == Me.Id };
            }).ToList();
            CommentPlaceholder = comments.Count > 0 ? "Add a comment…" : "Start the discussion…";
            AnnouncementList = ChannelAnnouncements.Select(a => new
            {
                Url = Url(Channel, a.Id, SelectedId), a.Title, Course = Courses.FirstOrDefault(c => c.Id == a.CourseId)?.Title ?? "", Date = Ui.Date(a.CreatedAt), Active = a.Id == Announcement.Id
            }).ToList();
            var lessons = ActiveCourse == null ? new List<LessonRow>() : Data.LessonsOf(ActiveCourse.Id).Take(3).ToList();
            ActivityCount = lessons.Count;
            CourseActivities = lessons.Select(l => new { l.CourseId, l.Title, l.Type, Minutes = l.Minutes > 0 ? l.Minutes : 15 }).ToList();
        }

        /// <summary>The thread: a divider per day; consecutive messages from one sender within five minutes are grouped.</summary>
        void BindConversation()
        {
            if (Selected == null) return;
            var messages = Conversation(Selected.Id);
            Unread = messages.Count(m => m.ReceiverId == Me.Id && !m.Read);
            MessagePlaceholder = "Write a message to " + Selected.Name.Split(' ')[0];
            if (messages.Count == 0)
            {
                Thread = "<div class=\"sc-empty\">" + Face(Selected, "lg") + "<h3>Say hello</h3><p>Start a conversation with " + Ui.H(Selected.Name) + ". Messages you send appear here.</p></div>";
                return;
            }
            var html = new StringBuilder();
            ChatRow previous = null;
            foreach (var m in messages)
            {
                var mine = m.SenderId == Me.Id;
                var newDay = previous == null || previous.SentAt.Date != m.SentAt.Date;
                var grouped = !newDay && previous.SenderId == m.SenderId && (m.SentAt - previous.SentAt).TotalMinutes < 5;
                previous = m;
                if (newDay) html.Append("<div class=\"sc-day\" role=\"separator\"><span>" + DayLabel(m.SentAt) + "</span></div>");
                var status = mine ? (m.Read ? "Read" : "Sent") : (m.Read ? "Read" : "Received");
                var tick = !mine ? "" : m.Read
                    ? "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"m2.5 12.5 4.5 4.5 9-9.5\"/><path d=\"m11.5 16.5.5.5 9.5-9.5\"/></svg>"
                    : "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"m5 12.5 4.5 4.5L19 7.5\"/></svg>";
                html.Append("<article class=\"sc-bubble bubble " + (mine ? "mine" : "") + " " + (grouped ? "sc-grouped" : "") + "\"><p class=\"sc-bubble-copy\">" + Ui.H(m.Content)
                    + "</p><footer class=\"sc-bubble-meta\"><time title=\"" + Ui.Stamp(m.SentAt) + "\">" + Ui.Time(m.SentAt) + "</time><span class=\"sc-status\">" + tick + status + "</span></footer></article>");
            }
            Thread = html.ToString();
        }

        static string DayLabel(DateTime when)
        {
            if (when.Date == DateTime.Today) return "Today";
            if (when.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
            return when.ToString(when.Year == DateTime.Today.Year ? "ddd, MMM d" : "ddd, MMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));
        }

    }
}
