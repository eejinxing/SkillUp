<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AdminDashboard.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.AdminDashboard" %>
<section class="admin-dashboard">
  <header class="ad-header"><h1>Admin overview</h1><nav aria-label="Admin quick navigation"><a href="Users">User accounts</a><a href="#admin-activity">Activity</a></nav></header>
  <section class="ad-banner ad-banner-live" aria-labelledby="ad-pulse-title"><img src="assets/admin-dashboard/imgFrame54.svg" alt=""><div class="ad-banner-data"><%= Pulse %></div></section>
  <div class="ad-top-grid">
    <section class="ad-summary" aria-label="Platform summary"><%= Summary %></section>
    <section class="ad-metric ad-active"><div class="ad-material" aria-hidden="true"></div><h2>Active learners</h2><strong class="ad-metric-value" data-admin-metric="active"><%= Active.ToString("N0") %></strong><p class="ad-growth"><%= Growth %></p>
      <label class="ad-window"><span class="sr-only">Activity window</span><asp:DropDownList ID="ActivityWindow" runat="server" ClientIDMode="Static" aria-label="Activity window" AutoPostBack="true" OnSelectedIndexChanged="Window_Changed"><asp:ListItem Value="7">Last 7 days</asp:ListItem><asp:ListItem Value="30">Last 30 days</asp:ListItem><asp:ListItem Value="90">Last 90 days</asp:ListItem></asp:DropDownList></label>
      <%= ActiveChart %></section>
    <section class="ad-metric ad-completion"><div class="ad-material" aria-hidden="true"></div><h2>Course completions</h2><strong class="ad-metric-value" data-admin-metric="completed"><%= Completed.ToString("N0") %></strong><p class="ad-growth"><%= Completed %> of <%= Enrolments %> enrolments completed</p><%= CompletionChart %></section>
  </div>
  <div class="ad-lower-grid">
    <aside class="ad-action-card"><div><h2>Needs attention</h2><p>Keep your learning workspace moving.</p><a href="Users?status=Pending"><strong><%= PendingUsers %></strong><span>Accounts awaiting verification</span></a><a href="#admin-activity"><strong><%= PendingCourses %></strong><span>Courses awaiting Program Leader review</span></a><a href="#admin-activity"><strong><%= PendingDeletions %></strong><span>Pending deletion requests</span></a><a class="ad-audit-link" href="#admin-activity">Open platform activity</a></div></aside>
    <section class="ad-course-panel ad-logs-panel" id="admin-activity">
      <header><div><h2>User activity logs</h2><p>Recorded activity across all users and roles</p></div><skill:SubmitButton runat="server" CssClass="ad-export-logs" CausesValidation="false" OnClick="Export_Click"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 4v11m-5-5 5 5 5-5M5 20h14"/></svg>Export CSV</skill:SubmitButton></header>
      <asp:Panel runat="server" CssClass="ad-course-filters ad-log-filters" DefaultButton="SearchButton">
        <label><span class="sr-only">Search user activity</span><asp:TextBox ID="LogSearch" runat="server" TextMode="Search" aria-label="Search user activity" placeholder="Search activity by name, action or details" /></label>
        <label><span class="sr-only">Filter activity role</span><asp:DropDownList ID="LogRole" runat="server" aria-label="Filter activity role" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed"><asp:ListItem Value="">All roles</asp:ListItem><asp:ListItem>Admin</asp:ListItem><asp:ListItem>Program Leader</asp:ListItem><asp:ListItem>Instructor</asp:ListItem><asp:ListItem>Learner</asp:ListItem><asp:ListItem>System</asp:ListItem></asp:DropDownList></label>
        <label><span class="sr-only">Filter activity category</span><asp:DropDownList ID="LogSource" runat="server" aria-label="Filter activity category" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed"><asp:ListItem Value="">All activity</asp:ListItem><asp:ListItem>Audit</asp:ListItem><asp:ListItem>Learning</asp:ListItem><asp:ListItem>Community</asp:ListItem><asp:ListItem>Messages</asp:ListItem></asp:DropDownList></label>
        <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Filter_Changed" />
      </asp:Panel>
      <div class="ad-table-scroll ad-tl-scroll" tabindex="0" aria-label="All user activity logs"><p class="sr-only">All recorded user activity, newest first. Times are local.</p><ol class="ad-timeline" id="ad-log-rows"><%= LogRows %></ol></div>
      <footer class="ad-log-footer"><p id="ad-log-count" role="status"><%= LogCount %></p><span>Newest first &middot; Local time</span></footer>
    </section>
  </div>
</section>
