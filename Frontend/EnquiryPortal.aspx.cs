using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The visitor's enquiries: those linked to this browser (ENQUIRY_ACCESS) or sent from the signed-in account.
    /// </summary>
    public partial class EnquiryPortalPage : SkillPage
    {
        public override string Route => "enquiries";

        protected EnquiryRow Selected;
        protected int Count;
        /// <summary>Right after a new enquiry was sent: the saved message replaces the form.</summary>
        protected bool Saved;

        List<EnquiryRow> mine;

        /// <summary>Enquiries this browser or account may open, newest first. The rule lives in the enquiryList command.</summary>
        List<EnquiryRow> Mine()
        {
            if (mine != null) return mine;
            var ids = new HashSet<int>(((IEnumerable<JObject>)Run("enquiryList")).Select(q => int.Parse((string)q["id"])));
            return mine = Data.Enquiries.Where(q => ids.Contains(q.Id)).OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.Id).ToList();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = QueryInt("id");
            Selected = id == null ? null : Mine().FirstOrDefault(q => q.Id == id);
            Saved = !IsPostBack && Selected != null && Request.QueryString["saved"] == "1";
            if (IsPostBack) return;
            if (Selected != null)
            {
                NameBox.Text = Selected.Name;
                EmailBox.Text = Selected.Email;
                SubjectBox.Text = Selected.Subject;
                MessageBox.Text = Selected.Message;
            }
            else if (Me != null)
            {
                NameBox.Text = Me.Name;
                EmailBox.Text = Me.Email;
            }
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            var wasNew = Selected == null;
            // Like the landing page form: picking a topic is enough for the subject of a new enquiry.
            var subject = SubjectBox.Text.Trim();
            if (subject.Length == 0 && wasNew)
                subject = TopicCourse.Checked ? "Choosing a course" : TopicActivity.Checked ? "A practical activity" : TopicAccount.Checked ? "My account" : TopicOther.Checked ? "Something else" : "";
            if (subject.Length == 0)
            {
                EditorError.Text = "Add a short subject so we can route your message.";
                return;
            }
            var payload = new { id = Selected?.Id.ToString(), name = NameBox.Text.Trim(), email = EmailBox.Text.Trim(), subject, message = MessageBox.Text.Trim() };
            object result;
            if (!TryRun("enquirySave", payload, out result)) return;
            // The page is redrawn in place: refresh the list, show the saved enquiry, and say what happened.
            var id = int.Parse((string)((JObject)result)["id"]);
            mine = null;
            Selected = Mine().FirstOrDefault(q => q.Id == id);
            if (wasNew) Saved = true;
            else EditorNotice.Text = "Changes saved.";
        }

        protected void Delete_Click(object sender, EventArgs e) => DeleteConfirm.Visible = true;

        protected void Cancel_Click(object sender, EventArgs e) => DeleteConfirm.Visible = false;

        protected void ConfirmDelete_Click(object sender, EventArgs e)
        {
            if (Selected == null || !TryRun("enquiryDelete", new { id = Selected.Id.ToString() })) return;
            // Back to an empty form for a new enquiry; the name and email stay.
            mine = null;
            Selected = null;
            DeleteConfirm.Visible = false;
            TopicCourse.Checked = TopicActivity.Checked = TopicAccount.Checked = TopicOther.Checked = false;
            SubjectBox.Text = MessageBox.Text = "";
            EditorNotice.Text = "Enquiry deleted.";
        }

        public override void ShowError(string message) => EditorError.Text = Server.HtmlEncode(message);

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            var list = Mine();
            Count = list.Count;
            EnquiryList.DataSource = list.Select(q => new { q.Id, q.Subject, q.Status, Date = Ui.Date(q.CreatedAt), Selected = q.Id == Selected?.Id });
            EnquiryList.DataBind();
            EmptyList.Visible = list.Count == 0;
            StatusBadge.Visible = DeleteButton.Visible = Selected != null;
            Topics.Visible = Selected == null;
            SavedPanel.Visible = Saved;
            EditorPanel.Visible = DeleteRow.Visible = !Saved;
        }
    }
}
