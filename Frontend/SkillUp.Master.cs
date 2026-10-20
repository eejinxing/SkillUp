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
            // Encode query-derived IDs before putting the initial hash in the document.
            var routeJson = HttpUtility.JavaScriptStringEncode(initialRoute, true);
            RouteBootstrap.Text = "<script>if(!location.hash){history.replaceState(null,'','#/'+"
                + routeJson + ");}</script>";
            base.OnPreRender(e);
        }
    }
}
