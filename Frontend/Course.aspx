<%@ Page Title="Course" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Course.aspx.cs" Inherits="SKILL_UP.Frontend.CoursePage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:PlaceHolder ID="Unavailable" runat="server" Visible="false">
  <div class="empty"><h3>Course unavailable</h3><p>This course is not currently open for enrolment.</p></div>
</asp:PlaceHolder>
<asp:PlaceHolder ID="Detail" runat="server">
<article class="course-enroll" data-course-id="<%= C.Id %>">
  <nav class="ce-breadcrumb" aria-label="Course navigation">
    <a href="<%= BackUrl %>" class="ce-back"><svg viewBox="0 0 16 16" aria-hidden="true"><path d="M10 3.5 5.5 8l4.5 4.5" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg><%: BackLabel %></a>
    <span aria-hidden="true">/</span><span><%: C.Title %></span>
    <% if (Me?.Role == "Learner") { %><a href="Dashboard" class="ce-dashboard-link">My dashboard</a><% } %>
  </nav>
  <header class="ce-banner">
    <skill:CourseHero runat="server" />
    <div class="ce-banner-copy">
      <p class="ce-banner-program"><%: ProgramName %></p>
      <h1><%: C.Title %></h1>
      <p class="ce-banner-desc"><%: C.Description %></p>
      <ul class="ce-banner-facts">
        <li><span>Level</span><%: C.Difficulty %></li>
        <li><span>Activities</span><%= Lessons.Count %></li>
        <li><span>Learning time</span><%= TotalMinutes %> min</li>
        <li><span>Rating</span><%= Rating == null ? "New" : "&#9733; " + Rating %></li>
      </ul>
    </div>
  </header>
  <div class="ce-content">
    <div class="ce-main">
      <section class="ce-about">
        <h2>About this course</h2>
        <p class="ce-course-name"><%: C.Title %></p>
        <p>Build practical skills through <%= Lessons.Count %> guided activities taught by <%: InstructorName %>. Read the explanations, practise what you learn and check your understanding in the assessments.</p>
        <details class="ce-show-more">
          <summary><span class="ce-more-label">Show more</span><span class="ce-less-label">Show less</span></summary>
          <p><%: C.Description %></p>
          <p>Complete activities in order. Pass each quiz with at least 70% to complete the course and earn your certificate.</p>
        </details>
      </section>
      <section class="ce-outcomes">
        <h2>What you will learn</h2>
        <ul>
          <asp:Repeater ID="Outcomes" runat="server"><ItemTemplate><li><span class="ce-tick"><svg viewBox="0 0 16 16" aria-hidden="true"><path d="M3.5 8.5l3 3 6-7" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg></span><%#: Container.DataItem %></li></ItemTemplate></asp:Repeater>
        </ul>
      </section>
      <section class="ce-curriculum">
        <div class="ce-section-heading"><h2>Course curriculum</h2><small><%= Lessons.Count %> activities &middot; <%= TotalMinutes %> min<%= Enrollment != null ? " &middot; " + Completed.Count + " completed" : "" %></small></div>
        <% if (Enrollment != null) { %><div class="ce-curriculum-progress" role="progressbar" aria-label="Course progress" aria-valuenow="<%= Percent %>" aria-valuemin="0" aria-valuemax="100"><i style="width:<%= Percent %>%"></i></div><% } %>
        <div class="ce-lesson-list">
          <asp:Repeater ID="Curriculum" runat="server">
            <ItemTemplate>
              <details class="ce-lesson" data-lesson-id="<%# Eval("Id") %>" data-state="<%# Eval("State") %>">
                <summary>
                  <span class="ce-lesson-index"><%# (string)Eval("State") == "done" ? "<svg viewBox=\"0 0 16 16\" aria-hidden=\"true\"><path d=\"M3.5 8.5l3 3 6-7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>" : Eval("Number") %></span>
                  <span class="ce-lesson-copy"><span class="ce-lesson-title"><%#: Eval("Title") %></span><span class="ce-lesson-meta"><%#: Eval("Type") %> &middot; <%# Eval("Minutes") %> min</span></span>
                  <span class="ce-lesson-state"><%# (string)Eval("State") == "locked" ? "<svg viewBox=\"0 0 16 16\" aria-hidden=\"true\"><rect x=\"3.5\" y=\"7\" width=\"9\" height=\"6.5\" rx=\"1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.4\"/><path d=\"M5.5 7V5.2a2.5 2.5 0 0 1 5 0V7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.4\"/></svg>" : "" %><%#: Eval("Label") %></span>
                  <span class="ce-chevron"><svg viewBox="0 0 16 16" aria-hidden="true"><path d="M4 6l4 4 4-4" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg></span>
                </summary>
                <div class="ce-lesson-body">
                  <div><p><%#: Eval("Note") %></p><%# Eval("PreviewHtml") %></div>
                  <div><%# Eval("ActionHtml") %></div>
                </div>
              </details>
            </ItemTemplate>
          </asp:Repeater>
          <asp:PlaceHolder ID="NoLessons" runat="server"><p class="ce-course-empty">The instructor is preparing the course activities.</p></asp:PlaceHolder>
        </div>
      </section>
      <skill:CourseFiles ID="Files" runat="server" />
      <section class="ce-course-information">
        <h2 class="ce-info-title">Course details</h2>
        <div class="ce-info-grid">
          <div class="ce-info-card"><h3>Requirements</h3><ul><li><%= C.Difficulty == "Beginner" ? "No prior experience required" : "Basic familiarity with the subject" %></li><li>A computer with internet access</li><li>A code editor and a curious mind</li></ul></div>
          <div class="ce-info-card"><h3>Who this course is for</h3><ul><li>Students building practical tech skills</li><li>Learners exploring <%: C.Title %></li><li>Anyone ready to learn by doing</li></ul></div>
          <div class="ce-info-card"><h3>This course includes</h3><ul class="ce-includes"><li><%= Lessons.Count %> guided activities</li><li><%= TotalMinutes %> minutes of learning content</li><li>Completion certificate</li><li>70% quiz pass mark</li></ul></div>
          <div class="ce-info-card"><h3>Tags</h3><div class="ce-tags"><span><%: string.IsNullOrEmpty(C.Icon) ? "Tech" : C.Icon %></span><span><%: C.Difficulty %></span><span><%: ProgramName %></span></div></div>
        </div>
      </section>
    </div>
    <aside class="ce-sidebar">
      <section class="ce-enrol-card">
        <small><%= Enrollment != null ? "Your course progress" : CanManage ? "You manage this course" : "Start your next chapter" %></small>
        <strong><%: Enrollment != null ? Percent + "% complete" : CanManage ? C.Status : "Free enrolment" %></strong>
        <% if (Enrollment != null) { %><div class="ce-enrol-progress" aria-hidden="true"><i style="width:<%= Percent %>%"></i></div><% } %>
        <asp:HyperLink ID="ActionLink" runat="server" CssClass="ce-enrol-button" />
        <skill:SubmitButton ID="EnrolButton" runat="server" CssClass="ce-enrol-button" Text="Enrol in course" CausesValidation="false" OnClick="Enrol_Click" />
        <p><%= Enrollment != null ? Completed.Count + " of " + Lessons.Count + " activities completed" : CanManage ? "Edit activities, files and quizzes from the course editor." : "All course activities included &middot; Learn at your own pace" %></p>
        <% if (Me == null) { %><a class="ce-create-account" href="#register">Create a free account</a><% } %>
        <dl>
          <div><dt>Level</dt><dd><%: C.Difficulty %></dd></div>
          <div><dt>Duration</dt><dd><%= C.Duration %> weeks</dd></div>
          <div><dt>Deadline</dt><dd><%= Ui.Date(C.Deadline) %></dd></div>
          <div><dt>Enrolled learners</dt><dd><%= LearnerCount %></dd></div>
        </dl>
      </section>
      <section class="ce-instructors">
        <h2>Your instructor</h2>
        <div><%= Ui.Avatar(Instructor) %><span><strong><%: InstructorName %></strong><small>Instructor</small></span></div>
        <p><%: string.IsNullOrWhiteSpace(Instructor?.Bio) ? "Practical lessons and guided learning." : Instructor.Bio %></p>
        <% if (Me != null && Me.Id != C.InstructorId && Instructor != null) { %><a class="ce-instructor-link" href="Messages?id=<%= Instructor.Id %>">Send a message</a><% } %>
      </section>
    </aside>
  </div>
  <section class="ce-reviews" aria-label="Learner reviews" id="course-reviews">
    <aside class="ce-rating">
      <h2>Learner reviews<span>across <%: AllReviews ? "Skill UP" : C.Title %></span></h2>
      <strong class="ce-average">&#9733; <%= ReviewAverage %></strong>
      <p><%= Ui.Plural(Reviews.Count, "review") %> &middot; <%= AllReviews ? "all courses" : "this course" %></p>
      <div class="ce-rating-bars">
        <asp:Repeater ID="RatingBars" runat="server"><ItemTemplate><div><span><%# Eval("Label") %></span><meter min="0" max="100" value="<%# Eval("Percent") %>" aria-label="<%# Eval("Aria") %>"></meter><small><%# Eval("Rounded") %>%</small></div></ItemTemplate></asp:Repeater>
      </div>
      <asp:PlaceHolder ID="WriteReview" runat="server">
        <details class="ce-review-form">
          <summary class="ce-review-write">Write a review</summary>
          <asp:Panel runat="server" CssClass="stack" DefaultButton="SaveReviewButton">
            <label>Rating
              <asp:DropDownList ID="ReviewRating" runat="server"><asp:ListItem>5</asp:ListItem><asp:ListItem>4</asp:ListItem><asp:ListItem>3</asp:ListItem><asp:ListItem>2</asp:ListItem><asp:ListItem>1</asp:ListItem></asp:DropDownList>
            </label>
            <label>Your experience
              <asp:TextBox ID="ReviewText" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" />
              <asp:RequiredFieldValidator runat="server" ControlToValidate="ReviewText" ValidationGroup="review" ErrorMessage="Write a few words about the course." Display="Dynamic" CssClass="field-error" />
            </label>
            <skill:SubmitButton ID="SaveReviewButton" runat="server" CssClass="primary" Text="Save review" ValidationGroup="review" OnClick="SaveReview_Click" />
          </asp:Panel>
        </details>
      </asp:PlaceHolder>
    </aside>
    <div class="ce-review-list">
      <div class="ce-review-toolbar">
        <h3><%= Ui.Plural(Reviews.Count, "review") %></h3>
        <label class="ce-filter"><span class="sr-only">Filter learner reviews</span>
          <asp:DropDownList ID="ReviewScope" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ReviewScope_Changed">
            <asp:ListItem Value="course">This course</asp:ListItem>
            <asp:ListItem Value="all">All courses</asp:ListItem>
          </asp:DropDownList>
        </label>
      </div>
      <asp:Repeater ID="ReviewList" runat="server">
        <ItemTemplate>
          <article class="ce-review" data-review-id="<%# Eval("Id") %>">
            <div class="ce-review-identity"><%# Eval("AvatarHtml") %><strong><%#: Eval("Author") %></strong></div>
            <div class="ce-review-copy">
              <h3>Course: <%#: Eval("Course") %></h3>
              <p class="ce-review-stars"><span class="ce-stars" aria-label="<%# Eval("Rating") %> out of 5 stars"><%# Eval("Stars") %></span> <span><%# Eval("Rating") %>.0 &middot; Reviewed on <%# Eval("Date") %></span></p>
              <p class="ce-review-text"><%#: Eval("Text") %></p>
            </div>
            <asp:Repeater runat="server" DataSource='<%# Eval("Replies") %>'>
              <ItemTemplate>
                <div class="ce-thread"><%# Eval("AvatarHtml") %><div><div class="ce-reply-name"><strong><%#: Eval("Author") %></strong><span><%#: Eval("Role") %></span></div><small><%# Eval("Date") %> &middot; Replying to <%#: Eval("To") %></small><p><%#: Eval("Text") %></p></div></div>
              </ItemTemplate>
            </asp:Repeater>
            <asp:PlaceHolder runat="server" Visible='<%# Eval("CanReply") %>'>
              <details class="ce-reply-form">
                <summary>Reply</summary>
                <div class="stack">
                  <asp:TextBox ID="ReplyText" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" placeholder="Write a reply" aria-label="Write a reply" />
                  <skill:SubmitButton runat="server" CssClass="primary" Text="Post reply" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Reply_Command" />
                </div>
              </details>
            </asp:PlaceHolder>
          </article>
        </ItemTemplate>
      </asp:Repeater>
      <asp:PlaceHolder ID="NoReviews" runat="server"><div class="ce-no-reviews"><h3>No reviews yet</h3><p>Be the first to share what you learned.</p></div></asp:PlaceHolder>
    </div>
  </section>
</article>
</asp:PlaceHolder>
</asp:Content>
