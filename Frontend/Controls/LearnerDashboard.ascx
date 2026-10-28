<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LearnerDashboard.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.LearnerDashboard" %>
<div class="registered-home">
<div class="learning-console lc-x lc-velo">
  <header class="lx-top"><a class="lx-logo" href="Dashboard" aria-label="Skill UP overview"><img class="skill-logo" src="assets/skill-logo.png" alt="Skill UP" width="930" height="420"></a></header>
  <section class="vd-hero" aria-labelledby="vd-title">
    <div class="vd-hero-top">
      <div>
        <time><%= DateTime.Now.ToString("dddd, d MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-GB")) %></time>
        <h1 id="vd-title"><%= Greeting %>,<br><em><%: Me.Name %></em></h1>
        <div class="vd-hero-pills"><span><%= Courses.Count %> active courses</span><span><%= Done %> / <%= Total %> activities completed</span></div>
      </div>
      <aside class="vd-hero-update" aria-label="Latest course announcement">
        <span><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 4h18v13H9l-6 4Z"/></svg> Announcements <b><%= AnnouncementCount %></b></span>
        <strong><%: LatestTitle %></strong>
        <small><%: LatestDate %></small>
      </aside>
    </div>
    <div class="vd-hero-bottom">
      <nav aria-label="Dashboard sections"><button type="button" class="is-active" data-vd-scroll="vd-metrics">Overview</button><button type="button" data-vd-scroll="vd-active-courses">Active courses</button></nav>
      <a class="vd-resume" href="<%= ResumeUrl %>"><%= ResumeUrl == "Catalog" ? "Explore courses" : "Continue learning" %><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 17 17 7M8 7h9v9"/></svg></a>
    </div>
  </section>
  <div class="vd-body">
    <section class="vd-metrics" id="vd-metrics" aria-label="Learning overview">
      <article class="vd-card vd-level vd-rank" aria-label="<%= MyRank == null ? "Not ranked yet" : "Ranking " + MyRank + " of " + Field %>">
        <header><span>Ranking</span><small class="vd-metric-chip"><%= RankChip %></small></header>
        <div class="vd-level-track" role="img" aria-label="<%= MyRank == null ? "No ranking yet" : "Rank " + MyRank + " of " + Field + " learners" %>"><i style="width:<%= RankFill %>%"></i></div>
        <%= RankVisual %>
        <p><%: RankNote %></p>
        <a class="vd-level-badge" href="Leaderboard">View leaderboard <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></a>
        <dl class="vd-card-summary">
          <div><dt>All-time points</dt><dd><%= MyPoints.ToString("N0") %></dd></div>
          <div><dt>This week</dt><dd><%= WeekRank == null ? "Not yet" : "#" + WeekRank %></dd></div>
          <div><dt>Learners</dt><dd><%= Field %></dd></div>
        </dl>
      </article>
      <article class="vd-card vd-xp vd-active" aria-label="Active rate <%= Rate %>%">
        <header><span>Active rate</span><small class="vd-metric-chip" title="Change from the previous 30 days"><%= (RateDelta >= 0 ? "+" : "") + RateDelta %>%</small></header>
        <div class="vd-metric-rule" aria-hidden="true"><i style="width:<%= Rate %>%"></i></div>
        <div class="vd-main-value"><%= Rate %><small>%</small></div>
        <p>Active on <%= Active30 %> of the last 30 days</p>
        <div class="vd-toggle" role="group" aria-label="Chart range">
          <skill:SubmitButton ID="MonthlyButton" runat="server" CausesValidation="false" Text="Monthly" CommandArgument="month" OnCommand="Range_Command" />
          <skill:SubmitButton ID="YearlyButton" runat="server" CausesValidation="false" Text="Yearly" CommandArgument="year" OnCommand="Range_Command" />
        </div>
        <%= RateChart %>
      </article>
      <article class="vd-card vd-progress" aria-label="Activity progress">
        <header><span>Progress</span><small class="vd-metric-chip"><%= Percent %>%</small></header>
        <div class="vd-metric-rule" aria-hidden="true"><i style="width:<%= Percent %>%"></i></div>
        <div class="vd-progress-visual">
          <div class="vd-main-value"><%= Done %><small>/ <%= Total %></small></div>
          <div class="vd-mini-bars" role="img" aria-label="XP activity over the last 30 days"><%= MiniBars %></div>
        </div>
        <p><%= Percent %>% of your activities completed</p>
        <dl class="vd-progress-details">
          <div><dt>Completed</dt><dd><%= Done %></dd></div>
          <div><dt>Remaining</dt><dd><%= Math.Max(0, Total - Done) %></dd></div>
          <div><dt>Courses</dt><dd><%= Courses.Count %></dd></div>
          <button type="button" data-vd-scroll="vd-active-courses">View course progress <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></button>
        </dl>
      </article>
    </section>
    <section class="vd-courses vd-card" id="vd-active-courses" aria-labelledby="vd-courses-title">
      <header>
        <h2 id="vd-courses-title">Active Courses <small><%= Shown.Count %></small></h2>
        <div class="vd-table-actions">
          <label><span class="sr-only">Filter courses by status</span>
            <asp:DropDownList ID="StatusFilter" runat="server" AutoPostBack="true">
              <asp:ListItem Value="all">All statuses</asp:ListItem>
              <asp:ListItem Value="progress">In progress</asp:ListItem>
              <asp:ListItem Value="new">Not started</asp:ListItem>
              <asp:ListItem Value="complete">Complete</asp:ListItem>
              <asp:ListItem Value="suggested">Suggested</asp:ListItem>
            </asp:DropDownList>
          </label>
          <skill:SubmitButton ID="SortButton" runat="server" CausesValidation="false" OnClick="Sort_Click"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 6h16M7 12h10M10 18h4"/></svg> Sort</skill:SubmitButton>
          <a href="MyCourses" class="vd-all-courses">All courses <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 17 17 7M8 7h9v9"/></svg></a>
          <span class="vd-course-avatar" aria-label="<%: Me.Name %>"><%: Ui.Initials(Me) %></span>
        </div>
      </header>
      <ul class="vd-course-list">
        <asp:Repeater ID="CourseRows" runat="server">
          <ItemTemplate>
            <li><a class="vd-course-row" href="Course?id=<%# Eval("Id") %>"><span class="vd-course-info"><span class="vd-subject"><%# Eval("Icon") %></span><span><strong><%#: Eval("Title") %></strong><small><%#: Eval("Detail") %></small></span></span><span class="vd-course-meter" role="progressbar" aria-label="<%#: Eval("Title") %> completion" aria-valuemin="0" aria-valuemax="100" aria-valuenow="<%# Eval("Percent") %>"><i style="width:<%# Eval("Percent") %>%"></i></span><b class="vd-course-percent"><%# Eval("Percent") %>%</b></a></li>
          </ItemTemplate>
        </asp:Repeater>
      </ul>
      <% if (Shown.Count == 0) { %><p class="vd-filter-empty">No courses in this view. <a href="Catalog">Explore courses</a></p><% } %>
      <footer><span>Progress updates as you complete activities.</span><a href="Catalog">Explore courses <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></a></footer>
    </section>
  </div>
</div>
</div>
