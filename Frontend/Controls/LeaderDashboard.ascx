<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LeaderDashboard.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.LeaderDashboard" %>
<section class="leader-dashboard"><h1 id="page-title" class="sr-only" tabindex="-1">Program overview</h1>
  <nav class="ld-rail" aria-label="Program Leader quick navigation"><%= Rail %></nav>
  <div class="ld-main">
    <div class="ld-metrics"><%= Metrics %></div>
    <div class="ld-board">
      <div class="ld-board-main">
        <section class="ld-activity"><%= Native("250-5174-imgVector.svg", "ld-surface") %>
          <header class="ld-chart-header"><%= Native("250-5175-imgVector.svg", "ld-surface") %><h2>Weekly Platform Activity</h2><p><%= Change %> <span>vs prior <%= Ui.Plural(Days, "day") %></span></p></header>
          <div class="ld-chart-controls">
            <div class="ld-periods" role="group" aria-label="Activity period">
              <asp:Repeater ID="Periods" runat="server"><ItemTemplate><skill:SubmitButton runat="server" CausesValidation="false" CommandArgument='<%# Eval("Value") %>' OnCommand="Period_Command" aria-label='<%# Eval("Title") %>' aria-pressed='<%# Eval("Pressed") %>'><%# Native((string)Eval("File"), "") %><span><%# Eval("Label") %></span></skill:SubmitButton></ItemTemplate></asp:Repeater>
            </div>
            <div class="ld-chart-legend"><span><%= Native("250-5186-imgVector.svg", "") %>Prior period</span><span><%= Native("250-5189-imgVector.svg", "") %>Selected period</span></div>
          </div>
          <%= Chart %>
        </section>
        <asp:PlaceHolder ID="NoCourses" runat="server"><section class="ld-reach ld-card"><h2>Course performance</h2><p class="ld-empty">No managed courses yet.</p></section></asp:PlaceHolder>
        <asp:PlaceHolder ID="Reach" runat="server">
        <section class="ld-reach" aria-label="Program performance">
          <section class="ld-course-panel ld-monitor-course" data-current-course="<%= Selected?.Id %>">
            <header class="ld-panel-heading"><h2>Course performance</h2><a href="Analytics" aria-label="Open program analytics"><%= Arrow %></a></header>
            <div class="ld-monitor-course-body">
              <div class="ld-monitor-course-primary">
                <label class="ld-course-picker"><span class="sr-only">Select course</span><asp:DropDownList ID="PerformanceCourse" runat="server" AutoPostBack="true" OnSelectedIndexChanged="Course_Changed" DataTextField="Title" DataValueField="Id" /></label>
                <section class="ld-monitor-inspector"><header><span>Course details</span><details class="ld-monitor-actions"><summary aria-label="Course actions"><%= MonitorMore %></summary><nav aria-label="Course actions"><a href="Course?id=<%= Selected?.Id %>">View course</a><a href="CourseEditor?id=<%= Selected?.Id %>">Edit course</a><a href="Analytics">Program analytics</a></nav></details></header>
                  <dl class="ld-monitor-course-fields"><%= CourseFields %></dl></section>
              </div>
              <aside class="ld-course-trends ld-monitor-trends"><h3>Learner breakdown</h3><dl><%= Breakdown %></dl><a class="ld-panel-more" href="Course?id=<%= Selected?.Id %>">View course <%= Arrow %></a></aside>
            </div>
            <footer class="ld-course-foot"><span><%= CourseFoot %></span><span>Completion <strong><%= CourseRate %></strong></span></footer>
          </section>
          <section class="ld-health-panel ld-monitor-health"><header class="ld-panel-heading"><h2>Learning summary</h2><a href="Analytics" aria-label="View learning analytics"><%= Arrow %></a></header>
            <div class="ld-monitor-health-body"><%= Health %></div></section>
        </section>
        </asp:PlaceHolder>
      </div>
      <section class="ld-pending ld-notifications ld-card ld-monitor-alerts" aria-labelledby="ld-notes-title">
        <header class="ld-notes-head"><h2 id="ld-notes-title">Alerts &amp; notifications</h2><span class="ld-monitor-current-filter"><%= TabLabel %> <%= TabCount(Tab) %></span>
          <details class="ld-monitor-filter"><summary aria-label="Filter notifications: <%= TabLabel %>" title="Filter notifications"><%= FilterIcon %></summary>
            <div class="ld-note-tabs" role="tablist" aria-label="Notification filter" aria-orientation="vertical">
              <skill:SubmitButton ID="TabUnread" runat="server" CssClass="ld-note-tab" role="tab" CausesValidation="false" CommandArgument="unread" OnCommand="Tab_Command">Unread<em><%= TabCount("unread") %></em></skill:SubmitButton>
              <skill:SubmitButton ID="TabRead" runat="server" CssClass="ld-note-tab" role="tab" CausesValidation="false" CommandArgument="read" OnCommand="Tab_Command">Read<em><%= TabCount("read") %></em></skill:SubmitButton>
              <skill:SubmitButton ID="TabArchived" runat="server" CssClass="ld-note-tab" role="tab" CausesValidation="false" CommandArgument="archived" OnCommand="Tab_Command">Archived<em><%= TabCount("archived") %></em></skill:SubmitButton>
            </div>
          </details>
        </header>
        <div class="ld-note-scroll">
          <div class="ld-note-panel" role="tabpanel" id="ld-panel-<%= Tab %>" tabindex="0" data-note-panel="<%= Tab %>">
            <asp:Repeater ID="Notes" runat="server">
              <ItemTemplate>
                <details class="ld-note ld-monitor-note" data-note="<%#: Eval("Id") %>" data-kind="<%# Eval("Kind") %>" data-tab="<%# Eval("Tab") %>"><summary title="Show notification actions"><%# Eval("Summary") %></summary>
                  <div class="ld-note-actions"><skill:SubmitButton runat="server" CssClass="ld-note-ghost" CausesValidation="false" Text='<%# Eval("ActionLabel") %>' CommandArgument='<%# Eval("Action") + "|" + Eval("Id") %>' OnCommand="Note_Command" /><%# Eval("Link") %></div>
                </details>
              </ItemTemplate>
            </asp:Repeater>
            <% if (Shown == 0) { %><p class="ld-caught-up"><%= EmptyNote %></p><% } %>
          </div>
        </div>
        <footer class="ld-monitor-alert-foot"><a href="Approvals"><%= Ui.Plural(PendingCount, "pending review") %></a><button type="button" data-leader-note-scroll aria-label="Scroll to more notifications" title="More updates"><%= ScrollIcon %></button></footer>
      </section>
    </div>
  </div>
</section>
