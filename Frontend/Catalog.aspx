<%@ Page Title="Explore courses" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Catalog.aspx.cs" Inherits="SKILL_UP.Frontend.CatalogPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="catalog-page">
  <%-- Picks the compact layout (see js/site.js, which keeps it right on resize) before the page is first drawn, so the cards do not visibly change size once the scripts load. --%>
  <script>(function (page) { var zoom = parseFloat(getComputedStyle(document.documentElement).zoom) || 1; page.classList.toggle('is-compact', innerHeight / zoom < 800); })(document.currentScript.parentElement);</script>
  <header class="cat-head">
    <div>
      <h1 id="page-title" tabindex="-1">Find your next skill</h1>
      <p>Practical courses. Clear milestones. Learn at your own pace.</p>
    </div>
    <dl class="cat-stats">
      <div><dt>Courses</dt><dd><%= Published.Count %></dd></div>
      <asp:PlaceHolder ID="EnrolledStat" runat="server"><div><dt>Enrolled</dt><dd><%= EnrolledCount %></dd></div></asp:PlaceHolder>
      <div><dt>Activities</dt><dd><%= ActivityCount %></dd></div>
    </dl>
  </header>
  <asp:Panel runat="server" CssClass="toolbar cat-toolbar" DefaultButton="SearchButton">
    <label class="cat-search">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>
      <asp:TextBox ID="SearchBox" runat="server" TextMode="Search" aria-label="Search courses" placeholder="Search courses, topics or instructors…" />
      <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Search_Click" />
    </label>
    <div class="cat-chips" role="group" aria-label="Filter by level">
      <asp:Repeater ID="Levels" runat="server">
        <ItemTemplate><skill:SubmitButton runat="server" CausesValidation="false" CommandArgument='<%# Eval("Level") %>' OnCommand="Level_Command" aria-pressed='<%# (bool)Eval("Pressed") ? "true" : "false" %>'><%#: Eval("Label") %><em><%# Eval("Count") %></em></skill:SubmitButton></ItemTemplate>
      </asp:Repeater>
    </div>
  </asp:Panel>
  <p class="cat-count" id="cat-count" role="status"><%= Ui.Plural(Shown.Count, "course") %></p>
  <div id="results" class="grid cat-grid">
    <asp:Repeater ID="Results" runat="server">
      <ItemTemplate><skill:CourseCard runat="server" Course='<%# Container.DataItem %>' /></ItemTemplate>
    </asp:Repeater>
    <asp:PlaceHolder ID="NoResults" runat="server"><div class="empty"><h3>No courses match</h3><p>Try another search or level.</p></div></asp:PlaceHolder>
  </div>
</section>
</asp:Content>
