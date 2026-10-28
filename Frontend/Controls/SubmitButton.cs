using System.Web.UI;
using System.Web.UI.WebControls;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// An asp:Button that renders as a &lt;button&gt; element, so it can hold icons and match the stylesheets,
    /// which style button elements. It posts back without JavaScript: the browser sends the clicked button's
    /// name, and Web Forms raises Click/Command for it exactly as for asp:Button.
    /// Markup between the tags replaces the Text, for example an icon followed by a label.
    /// </summary>
    [ParseChildren(false)]
    [PersistChildren(true)]
    public class SubmitButton : Button
    {
        protected override void Render(HtmlTextWriter writer)
        {
            AddAttributesToRender(writer);
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            RenderContents(writer);
            writer.RenderEndTag();
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            // Inner markup (child controls or <%= %> blocks) wins; RenderChildren draws both.
            if (HasControls() || string.IsNullOrEmpty(Text)) RenderChildren(writer);
            else writer.WriteEncodedText(Text);
        }
    }
}
