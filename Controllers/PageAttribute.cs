using SkillUp.Services;

namespace SkillUp.Controllers;

/// <summary>
/// Describes the page an action renders. It replaces the per-page properties of the old Web Forms SkillPage class.
/// <code>[HttpGet("Catalog"), Page("catalog", Secondary = true)]</code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class PageAttribute : Attribute
{
    public PageAttribute(string route) => Route = route;

    /// <summary>The page's name in the navigation, for example "dashboard". It highlights the nav link and becomes main[data-view].</summary>
    public string Route { get; }

    /// <summary>
    /// Who may open the page: null for anyone (visitors too), "*" for any signed-in account, or role names separated by commas
    /// ("Instructor,Admin"). Other roles see "Access restricted"; visitors are sent to Login.
    /// </summary>
    public string Roles { get; set; }

    public PageLayout Layout { get; set; } = PageLayout.Auto;

    /// <summary>Adds figma-secondary to main, which the secondary page styles target.</summary>
    public bool Secondary { get; set; }

    /// <summary>Extra classes for body, used by page-specific styles.</summary>
    public string BodyClass { get; set; } = "";

    /// <summary>Signing in through the Log in pop-up on this page comes back to it instead of going to the dashboard.</summary>
    public bool StayAfterSignIn { get; set; }

    /// <summary>Pages with the same group are steps of one task, so a back link skips over them. By default each page is its own group.</summary>
    public string Group { get; set; }

    public string[] RoleList => Roles == null || Roles == "*" ? [] : Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
