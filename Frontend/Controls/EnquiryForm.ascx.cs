using System;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// "Ready for your next step?" enquiry form, shared by Contact, Help and the landing page.
    /// Saving creates the enquiry, links it to this browser (or account) and shows its ID and access code once.
    /// </summary>
    public partial class EnquiryForm : UserControl
    {
        /// <summary>h1 when the form is the page's main content, h3 inside the landing page.</summary>
        public string HeadingTag { get; set; } = "h1";

        SkillPage Host => (SkillPage)Page;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Host.Me != null)
            {
                NameBox.Text = Host.Me.Name;
                EmailBox.Text = Host.Me.Email;
            }
        }

        protected void Send_Click(object sender, EventArgs e)
        {
            Page.Validate("enquiry");
            if (!Page.IsValid) return;
            var topic = TopicCourse.Checked ? "Choosing a course" : TopicActivity.Checked ? "A practical activity"
                      : TopicAccount.Checked ? "My account" : TopicOther.Checked ? "Something else" : null;
            // Picking a topic is enough for the subject, as it was before.
            var subject = SubjectBox.Text.Trim();
            if (subject.Length == 0) subject = topic ?? "";
            if (subject.Length == 0)
            {
                FormError.Text = "Add a short subject so we can route your message.";
                return;
            }
            try
            {
                var saved = (JObject)Host.Run("enquirySave", new { name = NameBox.Text.Trim(), email = EmailBox.Text.Trim(), subject, message = MessageBox.Text.Trim() });
                SavedId.Text = Server.HtmlEncode((string)saved["id"]);
                SavedToken.Text = Server.HtmlEncode((string)saved["token"]);
                Steps.ActiveViewIndex = 1;
            }
            catch (ApiException ex)
            {
                FormError.Text = Server.HtmlEncode(ex.Message);
            }
        }
    }
}
