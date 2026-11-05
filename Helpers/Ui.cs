using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using SkillUp.Models;

namespace SkillUp.Helpers;

/// <summary>Small display helpers shared by the views: avatars, dates, counts and status pills.</summary>
public static class Ui
{
    static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    public static string H(object value) => WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");

    public static string Initials(UserRow user) => user == null ? "?" : Initials(user.Name);

    public static string Initials(string name) =>
        string.Concat((name ?? "?").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    /// <summary>The account's picture, or its initials, inside span.avatar.</summary>
    public static HtmlString Avatar(UserRow user, string extraClass = "")
    {
        var cls = ("avatar " + extraClass).Trim();
        if (!string.IsNullOrEmpty(user?.Avatar)) return new HtmlString("<span class=\"" + cls + "\"><img src=\"" + H(user.Avatar) + "\" alt=\"" + H(user.Name) + "\"></span>");
        return new HtmlString("<span class=\"" + cls + "\">" + H(Initials(user)) + "</span>");
    }

    /// <summary>15 Oct 2026</summary>
    public static string Date(DateTime? time) => time?.ToString("d MMM yyyy", Gb) ?? "Not recorded";

    /// <summary>15 Oct</summary>
    public static string Day(DateTime? time) => time?.ToString("d MMM", Gb) ?? "";

    /// <summary>16:00</summary>
    public static string Time(DateTime time) => time.ToString("HH:mm", Gb);

    /// <summary>15 Oct 2026, 16:00</summary>
    public static string Stamp(DateTime time) => time.ToString("d MMM yyyy, HH:mm", Gb);

    /// <summary>"just now", "13 hours ago", "2 days ago", then the date.</summary>
    public static string Ago(DateTime time)
    {
        var span = DateTime.Now - time;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalHours < 1) return Plural((int)span.TotalMinutes, "minute") + " ago";
        if (span.TotalDays < 1) return Plural((int)span.TotalHours, "hour") + " ago";
        if (span.TotalDays < 30) return Plural((int)span.TotalDays, "day") + " ago";
        return Date(time);
    }

    /// <summary>Status pill: green for done or live states, amber for waiting ones, red for stopped ones.</summary>
    public static HtmlString Tag(string status)
    {
        var s = status ?? "";
        var tone = Regex.IsMatch(s, "Published|Active|Approved|Completed|Visible") ? "green"
                 : Regex.IsMatch(s, "Pending|Draft|Changes|Flagged") ? "amber"
                 : Regex.IsMatch(s, "Inactive|Rejected|Removed") ? "red" : "";
        return new HtmlString("<span class=\"tag " + tone + "\">" + H(s) + "</span>");
    }

    /// <summary>Labelled progress bar.</summary>
    public static HtmlString Bar(int value, string label = "Completion") => new(
        "<div class=\"progress-label\"><span>" + H(label) + "</span><span>" + value + "%</span></div><div class=\"progress\" role=\"progressbar\" aria-label=\"" + H(label)
        + "\" aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"" + value + "\"><span style=\"width:" + value + "%\"></span></div>");

    public static string Plural(int count, string word, string plural = null) =>
        count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? word : plural ?? word + "s");

    /// <summary>Round percentage, 0 when there is nothing to divide by.</summary>
    public static int Percent(double part, double whole) => whole <= 0 ? 0 : (int)Math.Round(100 * part / whole);

    /// <summary>
    /// A message time for the companion chat: the time today, the day and time before that. js/site.js rewrites it in the viewer's own
    /// clock and style; this text is what shows without it.
    /// </summary>
    public static HtmlString ChatTime(DateTime t) => new(
        "<time class=\"ai-time\" datetime=\"" + t.ToString("s", CultureInfo.InvariantCulture) + "\" title=\"" + t.ToString("d MMM yyyy, HH:mm:ss", Gb) + "\">"
        + (t.Date == DateTime.Today ? t.ToString("HH:mm", Gb) : t.ToString("d MMM, HH:mm", Gb)) + "</time>");
}
