<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="InstructorDashboard.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.InstructorDashboard" %>
<section class="instructor-dashboard">
  <h1 id="page-title" class="id-sr-only" tabindex="-1">Instructor dashboard</h1>
  <nav class="id-rail" aria-label="Instructor quick navigation"><%= Rail %></nav>
  <div class="id-main">
    <div class="id-metrics">
      <section class="id-metric id-enrolled">
        <img class="id-metric-surface" src="assets/instructor/283-2114-imgIncomeNeonGreenBarChart.svg" alt="">
        <header><h2>Enrolled learners</h2><a class="id-income-menu" href="Learners" aria-label="View all enrolled learners"><%= More %></a></header>
        <div class="id-figure"><strong class="id-big"><%= Learners %></strong><span class="id-delta"><%= (NewLearners > 0 ? "+" : "") + NewLearners %></span></div>
        <p class="id-metric-note">Compared to <%= StartCount %> a month ago &middot; <%= EnrolmentCount %> enrolments</p>
        <div class="id-enrolment-chart" role="img" aria-label="Learning sessions by your learners per day, last 30 days"><%= DailyBars %></div>
        <div class="id-chart-axis"><%= Axis %></div>
      </section>
      <section class="id-metric id-completion">
        <h2>Course completion</h2>
        <strong class="id-big id-mono"><%= Completion %>%</strong>
        <p class="id-metric-note"><%= Completed %> of <%= EnrolmentCount %> enrolments completed</p>
        <figure class="id-area-wrap"><span class="id-area-badge"><%= GrowthBadge %></span><%= CumulativeChart %><figcaption>Learner sessions, cumulative &middot; last 30 days</figcaption></figure>
      </section>
      <section class="id-metric id-quiz">
        <h2>Average Quiz Score</h2>
        <div class="id-quiz-figure"><span class="id-big id-mono"><%= QuizAverage == null ? "—" : QuizAverage + "%" %></span><small><%= Ui.Plural(QuizCount, "graded quiz attempt") %></small></div>
        <figure class="id-area-wrap"><%= QuizChart %><figcaption>Practice quiz average &middot; 7-day rolling</figcaption></figure>
        <a href="Analytics" class="id-quiz-link">View assessment analytics</a>
      </section>
    </div>
    <section class="id-workspace">
      <header class="id-workspace-header">
        <nav aria-label="Teaching workspace"><a href="Dashboard" class="active" aria-current="page">My Courses</a></nav>
        <a class="id-create-course" href="CourseEditor"><img src="assets/instructor/283-870-imgCiAddPlusCircle.svg" alt="">Create Course</a>
      </header>
      <div class="id-program-scroll" tabindex="0" aria-label="Courses grouped by program">
        <div class="id-program-grid">
          <asp:Repeater ID="Programs" runat="server">
            <ItemTemplate>
              <section class="id-program">
                <h2><%#: Eval("Name") %></h2>
                <div class="id-program-meta"><span class="id-program-people"><%# Eval("FirstLearner") %><b><%# Eval("LearnerCount") %></b></span><a href="CourseEditor?program=<%# Eval("Id") %>">&#65291; New Course</a></div>
                <div class="id-course-grid"><%# Eval("CardsHtml") %></div>
              </section>
            </ItemTemplate>
          </asp:Repeater>
          <asp:PlaceHolder ID="NoCourses" runat="server"><div class="id-empty"><h2>No courses assigned yet</h2><p>Create your first course and submit it for Program Leader review.</p><a href="CourseEditor" class="button">Create a course</a></div></asp:PlaceHolder>
        </div>
      </div>
    </section>
  </div>
</section>
