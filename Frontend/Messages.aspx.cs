using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Communication workspace: course announcement channels (?channel=, ?announcement=) with class comments,
    /// and private messages with one contact (?id=). Staff publish announcements for the courses they manage.
    /// </summary>
    public partial class MessagesPage : SkillPage
    {
        public override string Route => "messages";
        protected override string[] Roles => new string[0];

        static readonly string[] ChannelColours = { "#a3b0ce", "#b8a1c6", "#9fb6ae", "#baa38e" };
        protected const string SendIcon = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12h13M13 6l6 6-6 6\"/></svg>";

        protected List<CourseRow> Courses;
        protected List<AnnouncementRow> ChannelAnnouncements;
        protected AnnouncementRow Announcement;
        protected CourseRow ActiveCourse;
        protected UserRow Author, Selected;
        protected int Channel, AnnouncementId, SelectedId, AllAnnouncementUnread, TotalUnread, Unread, ContactCount, CommentCount, ActivityCount;
        protected string Thread;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Course channels: every course for Admin, otherwise enrolled (learner) or managed (staff) courses.
            Courses = Me.Role == "Admin" ? Data.Courses : Me.Role == "Learner" ? AccessibleCourses : OwnCourses;
            var ids = new HashSet<int>(Courses.Select(c => c.Id));
            Channel = ids.Contains(QueryInt("channel") ?? 0) ? QueryInt("channel").Value : 0;
            var all = Data.Announcements.Where(a => a.CourseId != null && ids.Contains(a.CourseId.Value)).ToList();
            ChannelAnnouncements = all.Where(a => Channel == 0 || a.CourseId == Channel).OrderByDescending(a => a.CreatedAt).ToList();
            Announcement = ChannelAnnouncements.FirstOrDefault(a => a.Id == QueryInt("announcement")) ?? ChannelAnnouncements.FirstOrDefault();
            AnnouncementId = Announcement?.Id ?? 0;
            // Showing an announcement reads it, so its badge goes away and stays away (the page's background refresh reads one that
            // arrives while the page is open). Your own announcements are never unread for you.
            if (!IsPostBack && Announcement != null && Announcement.PostedBy != Me.Id && !Data.HasRead(Me.Id, Announcement.Id))
                TryRun("announcementRead", new { id = Announcement.Id.ToString() });
            Func<int?, int> unread = course => all.Count(a => (course == null || a.CourseId == course) && a.PostedBy != Me.Id && !Data.HasRead(Me.Id, a.Id));
            AllAnnouncementUnread = unread(null);
            ActiveCourse = Courses.FirstOrDefault(c => c.Id == (Announcement?.CourseId ?? Channel));
            Author = Announcement == null ? null : Data.User(Announcement.PostedBy);

            var contacts = Data.Users.Where(u => u.Id != Me.Id && u.Status == "Active").ToList();
            Selected = contacts.FirstOrDefault(u => u.Id == QueryInt("id")) ?? contacts.FirstOrDefault();
            SelectedId = Selected?.Id ?? 0;
            // Opening a conversation reads it, and so does the page's background refresh while it stays open. Otherwise the
            // unread badges and "Mark as read" would stay on messages the person is already looking at.
            if (!IsPostBack && Selected != null && QueryInt("id") != null && Data.Messages.Any(m => m.ReceiverId == Me.Id && m.SenderId == Selected.Id && !m.Read))
                TryRun("readMessages", new { from = SelectedId.ToString() });
            TotalUnread = Data.Messages.Count(m => m.ReceiverId == Me.Id && !m.Read && contacts.Any(c => c.Id == m.SenderId));

            Channels.DataSource = Courses.Select((c, i) => new { Url = Url(c.Id, 0, SelectedId), c.Title, Colour = ChannelColours[i % 4], Count = unread(c.Id), Active = Channel == c.Id });
            Channels.DataBind();
            NewAnnouncementLink.Visible = Me.Role != "Learner" && Courses.Count > 0;
            if (Request.QueryString["compose"] == "1" && NewAnnouncementLink.Visible)
            {
                ComposePanel.Visible = true;
                if (!IsPostBack)
                {
                    ComposeCourse.DataSource = CourseOptions(Courses);
                    ComposeCourse.DataBind();
                    if (Channel != 0) ComposeCourse.SelectedValue = Channel.ToString();
                }
            }
            if (!IsPostBack) ContactSearch.Text = Request.QueryString["q"] ?? "";

            BindContacts(contacts);
            BindAnnouncement();
            BindConversation();
        }

        /// <summary>The page address for a channel, announcement and contact, keeping the contact search.</summary>
        protected string Url(int channel, int announcement, int contact)
        {
            var parts = new List<string> { "channel=" + channel };
            if (announcement != 0) parts.Add("announcement=" + announcement);
            if (contact != 0) parts.Add("id=" + contact);
            var q = Request.QueryString["q"];
            if (!string.IsNullOrEmpty(q)) parts.Add("q=" + HttpUtility.UrlEncode(q));
            return "Messages?" + string.Join("&", parts);
        }

        static int Hue(int id) => id.ToString(CultureInfo.InvariantCulture).Sum(c => c) % 5;

        protected string Face(UserRow u, string size) => "<span class=\"sc-face sc-hue-" + Hue(u.Id) + (size.Length > 0 ? " sc-face-" + size : "") + "\">" + Ui.Avatar(u) + "</span>";

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
            Contacts.DataSource = rows.Select(x => new
            {
                Url = Url(Channel, AnnouncementId, x.c.Id),
                Active = x.c.Id == SelectedId,
                Unread = x.unread,
                FaceHtml = Face(x.c, ""),
                x.c.Name,
                TimeHtml = x.last == null ? "" : "<time>" + (x.last.SentAt.Date == DateTime.Today ? Ui.Time(x.last.SentAt) : x.last.SentAt.ToString("MMM d", CultureInfo.GetCultureInfo("en-US"))) + "</time>",
                PreviewHtml = x.last == null ? "<span class=\"sc-role\">" + Ui.H(x.c.Role) + "</span>" : (x.last.SenderId == Me.Id ? "<span class=\"sc-you\">You: </span>" : "") + Ui.H(x.last.Content)
            });
            Contacts.DataBind();
        }

        void BindAnnouncement()
        {
            if (Announcement == null) return;
            var comments = Data.AnnouncementComments.Where(c => c.ParentId == Announcement.Id).ToList();
            CommentCount = comments.Count;
            Comments.DataSource = comments.Select(c =>
            {
                var u = Data.User(c.UserId);
                return new { c.Id, FaceHtml = u == null ? "" : Face(u, ""), Name = u?.Name ?? "Former user", Role = u?.Role ?? "Member", When = c.CreatedAt.ToString("d MMM, HH:mm", CultureInfo.GetCultureInfo("en-GB")), Text = c.Text, Mine = c.UserId == Me.Id };
            });
            Comments.DataBind();
            CommentBox.Attributes["placeholder"] = comments.Count > 0 ? "Add a comment…" : "Start the discussion…";
            AnnouncementList.DataSource = ChannelAnnouncements.Select(a => new
            {
                Url = Url(Channel, a.Id, SelectedId), a.Title, Course = Courses.FirstOrDefault(c => c.Id == a.CourseId)?.Title ?? "", Date = Ui.Date(a.CreatedAt), Active = a.Id == Announcement.Id
            });
            AnnouncementList.DataBind();
            var lessons = ActiveCourse == null ? new List<LessonRow>() : Data.LessonsOf(ActiveCourse.Id).Take(3).ToList();
            ActivityCount = lessons.Count;
            CourseActivities.DataSource = lessons.Select(l => new { l.CourseId, l.Title, l.Type, Minutes = l.Minutes > 0 ? l.Minutes : 15 });
            CourseActivities.DataBind();
        }

        /// <summary>The thread: a divider per day; consecutive messages from one sender within five minutes are grouped.</summary>
        void BindConversation()
        {
            if (Selected == null) return;
            var messages = Conversation(Selected.Id);
            Unread = messages.Count(m => m.ReceiverId == Me.Id && !m.Read);
            MarkReadButton.Visible = Unread > 0;
            MessageBox.Attributes["placeholder"] = "Write a message to " + Selected.Name.Split(' ')[0];
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

        protected void Search_Click(object sender, EventArgs e)
        {
            var q = ContactSearch.Text.Trim();
            Go("Messages?channel=" + Channel + (AnnouncementId != 0 ? "&announcement=" + AnnouncementId : "") + (q.Length > 0 ? "&q=" + HttpUtility.UrlEncode(q) : ""));
        }

        protected void Send_Click(object sender, EventArgs e)
        {
            if (MessageBox.Text.Trim().Length == 0) return;
            if (TryRun("message", new { to = SelectedId.ToString(), text = MessageBox.Text.Trim() })) Go(Url(Channel, AnnouncementId, SelectedId));
        }

        protected void MarkRead_Click(object sender, EventArgs e)
        {
            if (TryRun("readMessages", new { from = SelectedId.ToString() })) Go(Url(Channel, AnnouncementId, SelectedId), "Messages marked as read");
        }

        protected void PostComment_Click(object sender, EventArgs e)
        {
            if (CommentBox.Text.Trim().Length == 0) return;
            if (TryRun("announcementComment", new { id = AnnouncementId.ToString(), comment = CommentBox.Text.Trim() })) Go(Url(Channel, AnnouncementId, SelectedId), "Comment posted");
        }

        protected void DeleteComment_Command(object sender, CommandEventArgs e)
        {
            if (TryRun("announcementCommentDelete", new { id = AnnouncementId.ToString(), commentId = (string)e.CommandArgument })) Go(Url(Channel, AnnouncementId, SelectedId), "Comment deleted");
        }

        protected void Publish_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            var payload = new { courseId = ComposeCourse.SelectedValue, title = ComposeTitle.Text.Trim(), body = ComposeBody.Text.Trim() };
            if (TryRun("announcementSave", payload)) Go("Messages?channel=" + ComposeCourse.SelectedValue + (SelectedId != 0 ? "&id=" + SelectedId : ""), "Announcement published");
        }
    }
}
