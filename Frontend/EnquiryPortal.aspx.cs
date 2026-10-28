using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The visitor's enquiries: those linked to this browser (ENQUIRY_ACCESS) or sent from the signed-in account.
    /// An enquiry from another device opens with its ID and access code. The code is shown once, after sending or finding it.
    /// </summary>
    public partial class EnquiryPortalPage : SkillPage
    {
        public override string Route => "enquiries";

        protected EnquiryRow Selected;
        protected string Token;
        protected int Count;
        protected bool LookupOpen;

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
            if (Selected != null)
            {
                Token = Session[TokenKey(Selected.Id)] as string;
                Session.Remove(TokenKey(Selected.Id));
            }
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

        static string TokenKey(int id) => "SkillUp.EnquiryToken." + id;

        protected void Save_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            var payload = new { id = Selected?.Id.ToString(), name = NameBox.Text.Trim(), email = EmailBox.Text.Trim(), subject = SubjectBox.Text.Trim(), message = MessageBox.Text.Trim() };
            object result;
            if (!TryRun("enquirySave", payload, out result)) return;
            var saved = (JObject)result;
            var token = (string)saved["token"];
            if (token != null) Session[TokenKey(int.Parse((string)saved["id"]))] = token;
            Go("EnquiryPortal?id=" + (string)saved["id"], Selected == null ? "Enquiry sent." : "Changes saved.");
        }

        protected void Delete_Click(object sender, EventArgs e) => DeleteConfirm.Visible = true;

        protected void ConfirmDelete_Click(object sender, EventArgs e)
        {
            if (Selected != null && TryRun("enquiryDelete", new { id = Selected.Id.ToString() })) Go("EnquiryPortal", "Enquiry deleted.");
        }

        protected void Find_Click(object sender, EventArgs e)
        {
            LookupOpen = true;
            if (!IsValid) return;
            try
            {
                var found = (JObject)Run("enquiryLookup", new { id = LookupId.Text.Trim(), token = LookupToken.Text.Trim() });
                Session[TokenKey(int.Parse((string)found["id"]))] = (string)found["token"];
                Go("EnquiryPortal?id=" + (string)found["id"]);
            }
            catch (ApiException ex)
            {
                LookupError.Text = Server.HtmlEncode(ex.Message);
            }
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
            StatusBadge.Visible = AccessPanel.Visible = DeleteButton.Visible = Selected != null;
        }
    }
}
