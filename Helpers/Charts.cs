using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SkillUp.Helpers
{
    /// <summary>Small SVG charts drawn on the server for the dashboards.</summary>
    public static class Charts
    {
        static string N(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>A smooth line through the values (Catmull-Rom curves) inside a width x height box, and the same line closed into an area.</summary>
        public static Tuple<string, string> Area(IList<double> values, double width, double height, double? max = null)
        {
            var top = Math.Max(1, max ?? (values.Count == 0 ? 1 : values.Max()));
            var pts = values.Select((v, i) => new[] { values.Count > 1 ? i / (double)(values.Count - 1) * width : width / 2, height - v / top * (height * .86) - 4 }).ToList();
            if (pts.Count == 0) pts.Add(new[] { 0.0, height - 4 });
            var d = new StringBuilder("M" + pts[0][0].ToString(CultureInfo.InvariantCulture) + "," + N(pts[0][1]));
            for (var i = 0; i < pts.Count - 1; i++)
            {
                var p0 = i > 0 ? pts[i - 1] : pts[i];
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var p3 = i + 2 < pts.Count ? pts[i + 2] : p2;
                d.Append(" C" + N(p1[0] + (p2[0] - p0[0]) / 6) + "," + N(p1[1] + (p2[1] - p0[1]) / 6) + " " + N(p2[0] - (p3[0] - p1[0]) / 6) + "," + N(p2[1] - (p3[1] - p1[1]) / 6) + " " + N(p2[0]) + "," + N(p2[1]));
            }
            var line = d.ToString();
            return Tuple.Create(line, line + " L" + width.ToString(CultureInfo.InvariantCulture) + "," + height.ToString(CultureInfo.InvariantCulture) + " L0," + height.ToString(CultureInfo.InvariantCulture) + " Z");
        }

        /// <summary>The green area chart of the instructor dashboard cards.</summary>
        public static HtmlString GreenArea(string id, IList<double> values, string label, double? max = null)
        {
            var a = Area(values, 300, 120, max);
            return new HtmlString("<svg class=\"id-area\" viewBox=\"0 0 300 120\" preserveAspectRatio=\"none\" role=\"img\" aria-label=\"" + Ui.H(label) + "\"><defs><linearGradient id=\"" + id + "\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\">"
                + "<stop offset=\"0\" stop-color=\"#5fe03a\"/><stop offset=\".55\" stop-color=\"#9ff07e\" stop-opacity=\".55\"/><stop offset=\"1\" stop-color=\"#ffffff\" stop-opacity=\"0\"/></linearGradient></defs>"
                + "<path class=\"id-area-fill\" d=\"" + a.Item2 + "\" fill=\"url(#" + id + ")\"/><path class=\"id-area-line\" d=\"" + a.Item1 + "\" fill=\"none\" stroke=\"#56d832\" stroke-width=\"2\" vector-effect=\"non-scaling-stroke\"/></svg>");
        }

        /// <summary>Rounded average, the way the dashboards show percentages (0 for no values).</summary>
        public static int Avg(IEnumerable<int> values)
        {
            var list = values.ToList();
            return list.Count == 0 ? 0 : (int)Math.Round(list.Average(), MidpointRounding.AwayFromZero);
        }
    }
}
