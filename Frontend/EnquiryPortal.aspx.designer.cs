// This file describes the controls declared in EnquiryPortal.aspx.
namespace SKILL_UP.Frontend
{
    public partial class EnquiryPortalPage
    {
        protected global::System.Web.UI.UpdatePanel PortalPanel;
        protected global::System.Web.UI.WebControls.Repeater EnquiryList;
        protected global::System.Web.UI.WebControls.PlaceHolder EmptyList;
        protected global::System.Web.UI.WebControls.PlaceHolder StatusBadge;
        protected global::System.Web.UI.WebControls.TextBox NameBox;
        protected global::System.Web.UI.WebControls.TextBox EmailBox;
        protected global::System.Web.UI.WebControls.TextBox SubjectBox;
        protected global::System.Web.UI.WebControls.TextBox MessageBox;
        protected global::System.Web.UI.WebControls.Literal EditorError;
        protected global::System.Web.UI.WebControls.Literal EditorNotice;
        protected global::System.Web.UI.WebControls.PlaceHolder SavedPanel;
        protected global::System.Web.UI.WebControls.Panel EditorPanel;
        protected global::System.Web.UI.WebControls.PlaceHolder Topics;
        protected global::System.Web.UI.WebControls.RadioButton TopicCourse;
        protected global::System.Web.UI.WebControls.RadioButton TopicActivity;
        protected global::System.Web.UI.WebControls.RadioButton TopicAccount;
        protected global::System.Web.UI.WebControls.RadioButton TopicOther;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton SaveButton;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton DeleteButton;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl DeleteRow;
        protected global::System.Web.UI.WebControls.Panel DeleteConfirm;
    }
}
