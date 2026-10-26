using System;
using System.Web;
using System.Web.UI;

namespace SKILL_UP.Frontend
{
    public partial class SkillUpMaster : MasterPage
    {
        private string initialRoute = "home";

        public void SetInitialRoute(string route)
        {
            initialRoute = string.IsNullOrWhiteSpace(route) ? "home" : route;
        }

        protected override void OnPreRender(EventArgs e)
        {
            // The lecturer's file structure is Web Forms. Existing client-side behaviour is intentionally retained.
            // js/route-bootstrap.js reads this attribute. ASP.NET HTML-encodes attribute values, so
            // query-derived IDs are safe here.
            SkillBody.Attributes["data-initial-route"] = initialRoute;
            base.OnPreRender(e);
        }
    }
}
