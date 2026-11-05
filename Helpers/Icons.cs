using Microsoft.AspNetCore.Html;
using System.Collections.Generic;

namespace SkillUp.Helpers
{
    /// <summary>Line icons drawn inline (24px grid, currentColor), shared by the staff pages.</summary>
    public static class Icons
    {
        static HtmlString Svg(string paths, int size = 18) =>
            new HtmlString("<svg viewBox=\"0 0 24 24\" width=\"" + size + "\" height=\"" + size + "\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + paths + "</svg>");

        public static readonly HtmlString Book = Svg("<path d=\"M4 5.5A2.5 2.5 0 0 1 6.5 3H20v15H6.5A2.5 2.5 0 0 0 4 20.5z\"/><path d=\"M4 20.5A2.5 2.5 0 0 0 6.5 23H20v-5\"/>");
        public static readonly HtmlString Folder = Svg("<path d=\"M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z\"/>");
        public static readonly HtmlString Check = Svg("<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><path d=\"m8.5 12 2.5 2.5 4.5-5\"/>");

        static readonly Dictionary<string, string> AppPaths = new Dictionary<string, string>
        {
            ["grid"] = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/>",
            ["book"] = "<path d=\"M12 5v16M3 4c4-1 6 0 9 2 3-2 5-3 9-2v15c-4-1-6 0-9 2-3-2-5-3-9-2Z\"/>",
            ["users"] = "<circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M2 21v-3a7 7 0 0 1 14 0v3M17 3a4 4 0 0 1 0 8m2 3c3 1 3 4 3 7\"/>",
            ["chart"] = "<path d=\"M4 3v18h17M8 17v-5m5 5V7m5 10V4\"/>",
            ["chat"] = "<path d=\"M4 4h16v12H9l-5 4Z\"/>",
            ["check"] = "<path d=\"m5 12 4 4L20 5\"/><path d=\"M21 12v8H3V3h12\"/>",
            ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v3m0 14v3M2 12h3m14 0h3M5 5l2 2m10 10 2 2M5 19l2-2M17 7l2-2\"/>",
            ["bell"] = "<path d=\"M6 17V9a6 6 0 0 1 12 0v8l2 2H4Zm4 5h4\"/>"
        };

        /// <summary>The workspace icon set (the old app.js icon()): grid, book, users, chart, chat, check, settings, bell.</summary>
        public static HtmlString App(string name)
        {
            string paths;
            if (!AppPaths.TryGetValue(name ?? "", out paths)) paths = "<circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"M12 8v8m-4-4h8\"/>";
            return new HtmlString("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" aria-hidden=\"true\">" + paths + "</svg>");
        }

        /// <summary>Breadcrumb and title icons of the analytics-style staff pages (1.7 stroke, round caps).</summary>
        public static HtmlString Kit(string paths) =>
            new HtmlString("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + paths + "</svg>");

        public static readonly HtmlString KitHome = Kit("<path d=\"m3 11 9-7 9 7\"/><path d=\"M5 10v10h14V10\"/>");
        public static readonly HtmlString KitPeople = Kit("<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2.5 19a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18 13.5a6.5 6.5 0 0 1 3.5 5.5\"/>");
        public static readonly HtmlString KitChart = Kit("<path d=\"M4 19V5M4 19h16M8 15l3-4 3 2 5-6\"/>");
        public static readonly HtmlString KitPrint = Kit("<path d=\"M7 9V3h10v6M7 17H4v-7h16v7h-3\"/><path d=\"M7 14h10v7H7z\"/>");

        public static readonly HtmlString Search = new HtmlString("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"m20 20-3.5-3.5\"/></svg>");
    }
}
