<%@ Page Title="Learning analytics" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Analytics.aspx.cs" Inherits="SKILL_UP.Frontend.AnalyticsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<div class="la-page la-kit" data-filter="<%= Filter %>" data-expanded="<%= Expanded ? "true" : "false" %>">
  <nav class="kx-crumbs" aria-label="Breadcrumb"><a href="Dashboard"><%= Icons.KitHome %>Main</a><span aria-hidden="true">&rsaquo;</span><span><%= Icons.KitChart %>Analytics</span><div class="kx-crumb-actions"><a href="Learners"><%= Icons.KitPeople %>Learners</a><button type="button" data-action="print"><%= Icons.KitPrint %>Print</button></div></nav>
  <header class="kx-title"><span class="kx-title-icon"><%= Icons.KitChart %></span><h1 tabindex="-1" id="page-title">Learning analytics</h1>
    <div class="kx-seg" role="group" aria-label="Filter courses">
      <skill:SubmitButton ID="FilterAll" runat="server" Text="All" CausesValidation="false" CommandArgument="all" OnCommand="Filter_Command" />
      <skill:SubmitButton ID="FilterActive" runat="server" Text="Active" CausesValidation="false" CommandArgument="active" OnCommand="Filter_Command" />
      <skill:SubmitButton ID="FilterEmpty" runat="server" Text="No enrolments" CausesValidation="false" CommandArgument="empty" OnCommand="Filter_Command" />
    </div>
    <div class="kx-title-actions"><skill:SubmitButton runat="server" CssClass="kx-btn kx-btn-lg" Text="↓ Export CSV" CausesValidation="false" OnClick="Export_Click" /></div>
  </header>
  <div class="kx-stats"><%= Stats %></div>
  <section class="kx-panel"><table class="kx-table"><caption class="sr-only">Programs</caption><thead><tr><th scope="col">Program</th><th scope="col">Courses</th><th scope="col">Enrolled</th><th scope="col">Completion</th><th scope="col">Quiz average</th><th scope="col">Action</th></tr></thead><tbody><%= ProgramRows %></tbody></table></section>
  <section class="kx-panel"><table class="kx-table"><caption class="sr-only">Courses</caption><thead><tr><th scope="col">Course</th><th scope="col">Enrolled</th><th scope="col">Participation</th><th scope="col">Completion</th><th scope="col">Quiz average</th><th scope="col">Action</th></tr></thead><tbody><%= CourseRows %></tbody></table></section>
  <skill:SubmitButton ID="MoreButton" runat="server" CssClass="kx-more" CausesValidation="false" OnClick="More_Click">Load More <span aria-hidden="true">&rsaquo;</span></skill:SubmitButton>
  <p class="kx-foot">Calculated from the current enrolments and submitted attempts. Participation counts enrolments with at least one completed activity.</p>
</div>
</asp:Content>
