using Microsoft.AspNetCore.Mvc.Razor;

namespace SkillUp.Views;

/// <summary>Base class of every Razor view (see _ViewImports.cshtml): gives views the request's SkillContext without passing it around.</summary>
public abstract class SkillView<TModel> : RazorPage<TModel>
{
    public SkillContext Ctx => (SkillContext)Context.Items[SkillContext.Key];
    public UserRow Me => Ctx.Me;
    public Snapshot Data => Ctx.Data;
}
