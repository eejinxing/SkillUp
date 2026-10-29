<%@ Page Title="Leaderboard" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Leaderboard.aspx.cs" Inherits="SKILL_UP.Frontend.LeaderboardPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="skill-leaderboard lb-v2 lb-timing lb-sk">
  <header class="lb-header">
    <span class="lb-brand-mark" aria-hidden="true"><%= Star %></span>
    <h1 id="page-title" tabindex="-1">Leaderboard</h1>
    <asp:Panel runat="server" CssClass="lb-search" DefaultButton="SearchButton">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 4 4"/></svg>
      <asp:TextBox ID="SearchBox" runat="server" ClientIDMode="Static" TextMode="Search" placeholder="Search a learner" aria-label="Search learners" autocomplete="off" />
      <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Search_Click" />
    </asp:Panel>
    <div class="lb-header-pills">
      <span class="lb-pill is-gold"><%= Trophy %><%= TimeLeft %></span>
      <span class="lb-pill" aria-label="Your points"><%= AllMine?.Points.ToString("N0") ?? "0" %><small>pts</small></span>
      <span class="lb-pill is-green" aria-label="Your rank"><small>Rank</small><%= RankText(Mine) %>/<%= Rows.Count %></span>
      <span class="lb-live">Updated <%= DateTime.Now.ToString("HH:mm") %></span>
    </div>
    <a class="lb-header-profile" href="Profile"><span><%: (Me.Username ?? Me.Name).ToUpperInvariant() %></span><%= Ui.Avatar(Me) %></a>
  </header>
  <div class="lb-board">
    <div class="lb-hero">
    <section class="lb-profile" aria-label="Your ranking">
      <div class="lb-profile-avatar"><%= Ui.Avatar(Me) %></div>
      <div class="lb-profile-copy">
        <h2><%: Me.Name %><svg class="lb-verified" viewBox="0 0 24 24" aria-label="Verified learner"><circle cx="12" cy="12" r="10" fill="currentColor"/><path d="m7.5 12.3 3 3 6-6.3" fill="none" stroke="#14141a" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"/></svg></h2>
        <div class="lb-profile-stats">
          <span class="lb-personal-score"><%= Star %><strong><%= Mine?.Points.ToString("N0") ?? "0" %></strong></span>
          <span><b><%= MyQuizCount %></b> quiz attempts</span>
          <span><b><%= MyCourseCount %></b> courses</span>
          <span>rank <strong class="lb-personal-rank"><%= RankText(Mine) %></strong></span>
        </div>
      </div>
    </section>
    <nav class="lb-period-nav" aria-label="Leaderboard period">
      <skill:SubmitButton ID="WeekButton" runat="server" CausesValidation="false" CommandArgument="week" OnCommand="Period_Command" aria-label="This-week ranking">This week<sup><%= WeekMine?.Points.ToString("N0") ?? "0" %> pts<em><%= Standing(Board.Week) %></em></sup></skill:SubmitButton>
      <skill:SubmitButton ID="AllButton" runat="server" CausesValidation="false" CommandArgument="all" OnCommand="Period_Command" aria-label="All-time ranking">All time<sup><%= AllMine?.Points.ToString("N0") ?? "0" %> pts<em><%= Standing(Board.All) %></em></sup></skill:SubmitButton>
      <span class="lb-period-caption"><%= Period == "week" ? Ui.Day(Board.Start) + " – " + Ui.Day(Board.End.AddMilliseconds(-1)) : "Lifetime learning points" %></span>
    </nav>
    </div>
    <% if (PodiumHtml.Length > 0) { %>
    <section class="lb-sk-podium" aria-label="Top learners">
      <header><span>Top learners</span><span class="lb-count-pill">Ranked by score</span></header>
      <div class="lb-sk-pods"><%= PodiumHtml %></div>
    </section>
    <% } %>
    <section class="lb-rankings" aria-label="<%= PeriodLabel %> learner rankings">
      <div class="lb-timing-session"><span class="lb-session-name">LEARNING STANDINGS</span><span><%= PeriodLabel %> <b>&middot;</b> <%= Rows.Count %> learners</span><i class="lb-checkered" aria-hidden="true"></i></div>
      <% if (Rows.Count > 0 && Shown.Count > 0) { %>
      <section class="lb-row-group" aria-labelledby="lb-standings">
        <header><h2 id="lb-standings">Top 10</h2><span class="lb-group-note is-gold"><%= Trophy %><%= TimeLeft %> in this week's ranking</span><span class="lb-group-note"><%= Gap > 0 ? Gap.ToString("N0") + " points more to reach #1" : Mine?.Rank == 1 ? "You are leading" : "Complete a quiz to get ranked" %></span><span class="lb-count-pill"><%= Shown.Count %>/<%= Rows.Count %></span></header>
        <div class="lb-timing-labels" aria-hidden="true"><span>POS</span><span>LEARNER / COURSE</span><span title="Courses completed / enrolled">COURSES</span><span title="Best quiz score (%)">BEST</span><span title="Average quiz score (%)">AVG</span><span title="Time taken for the best quiz score; faster ranks higher on a tie">QUIZ TIME</span><span title="Total time from enrolling to completing the finished courses">COURSE TIME</span><span>STATUS</span></div>
        <div class="lb-row-list">
          <asp:Repeater ID="RankRows" runat="server">
            <ItemTemplate>
              <article class="lb-rank-card <%# Eval("PodiumClass") %> <%# (bool)Eval("IsMe") ? "lb-you" : "" %>" data-rank="<%# Eval("Rank") %>" data-score="<%# Eval("Points") %>" style="--i:<%# Container.ItemIndex %>;--learner-accent:<%# Eval("Accent") %>">
                <span class="lb-timing-position <%# Eval("Rank") == null ? "is-unranked" : "" %>" aria-label="Position <%# Container.ItemIndex + 1 %><%# Eval("Rank") == null ? ", not ranked yet" : ", rank " + Eval("Rank") %>"><%# (Container.ItemIndex + 1).ToString("00") %></span>
                <div class="lb-timing-identity">
                  <span class="lb-rank-person"><%# Eval("AvatarHtml") %><i class="lb-presence <%# (bool)Eval("Online") ? "is-on" : "" %>" aria-hidden="true"></i></span>
                  <%# Eval("BadgesHtml") %>
                  <div class="lb-driver-copy">
                    <h2 title="<%#: Eval("Name") %>"><%#: Eval("Name") %><%# (bool)Eval("IsMe") ? "<span class=\"lb-you-label\">YOU</span>" : "" %></h2>
                    <p><span class="lb-chip lb-level-chip">LV <%# Eval("Level") %></span><span class="lb-row-context" title="<%#: Eval("CourseTitles") %>"><%#: Eval("CourseContext") %></span><span class="lb-row-points" title="<%# Eval("BestTitle") %>"><%# Eval("PointsText") %> pts</span><span class="lb-date-chip" title="Last active"><%# Eval("LastActive") %></span></p>
                  </div>
                </div>
                <%# Eval("TimingHtml") %>
                <span class="lb-row-status <%# Eval("StatusClass") %>"><%# Eval("Status") %></span>
              </article>
            </ItemTemplate>
          </asp:Repeater>
          <%= EmptySlots %>
        </div>
      </section>
      <% } %>
      <% if (Rows.Count == 0) { %><div class="lb-empty"><h2>No learners yet</h2><p>Rankings appear when active learners join the platform.</p></div><% } %>
      <% if (Rows.Count > 0 && Shown.Count == 0) { %><p class="lb-no-matches">No matching learners.</p><% } %>
      <p class="lb-list-note" id="lb-results-count" role="status"><%= Term.Length > 0 ? Shown.Count + " of " + Rows.Count + " learners" : Rows.Count + " active learners · Courses completed first, then quiz score, then time taken. Learners with no points are unranked." %></p>
    </section>
    <footer class="lb-footer">
      <details><summary>How points work</summary><p>All time: learners are ranked by courses completed first, so finishing a course always beats a higher quiz score without the course. Learners with the same number of completed courses are ordered by best quiz score, then quiz average. If those are equal too, the learner who took less time on the quiz with the best score ranks higher, then the one who took less time to complete their courses. Quiz time is measured from opening the quiz to submitting it. This week uses the best and average of this week's quizzes and their time. Learners with no points are unranked.</p></details>
      <a href="MyCourses">Continue learning <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 17 17 7M7 7h10v10"/></svg></a>
    </footer>
  </div>
</section>
</asp:Content>
