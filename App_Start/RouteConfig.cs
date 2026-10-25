using System.Web;
using System.Web.Routing;
using System.Web.UI;
using Microsoft.AspNet.FriendlyUrls;
using Microsoft.AspNet.FriendlyUrls.Resolvers;

namespace SKILL_UP
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            var settings = new FriendlyUrlSettings { AutoRedirectMode = RedirectMode.Permanent };
            routes.EnableFriendlyUrls(settings, new DesktopUrlResolver());
            var switchView = routes["AspNet.FriendlyUrls.SwitchView"];
            if (switchView != null) routes.Remove(switchView);
        }

        private sealed class DesktopUrlResolver : WebFormsFriendlyUrlResolver
        {
            protected override bool TrySetMobileMasterPage(HttpContextBase context, Page page, string suffix)
            {
                return false;
            }
        }
    }
}
