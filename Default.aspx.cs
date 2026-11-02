using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // A signed-in account opens its own role's overview; only visitors see the landing page.
            DatabaseSetup.EnsureReady(Context);
            UserRow user;
            using (var db = new Db()) user = AuthCookie.Resolve(Context, Model.Load(db));
            var page = user != null && user.Status == "Active" ? "Dashboard" : "Home";
            Response.Redirect(ResolveUrl("~/Frontend/" + page + ".aspx"), false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}