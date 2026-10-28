<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CourseCard.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.CourseCard" %>
<article class="course-card figma-course-tile">
  <a class="figma-course-cover" href="<%= Url %>" aria-label="<%= Manage ? "Manage" : "View" %> <%: Course.Title %>">
    <img src="<%: Art %>" alt="" loading="eager">
    <span class="cc-level" data-level="<%: (Course.Difficulty ?? "").ToLowerInvariant() %>" role="img" aria-label="<%: Course.Difficulty %> difficulty" title="<%: Course.Difficulty %> difficulty"><% for (var i = 0; i < 3; i++) { %><span class="cc-level-bar<%= i < Level ? " is-active" : "" %>" style="--i:<%= i %>" aria-hidden="true"></span><% } %></span>
  </a>
  <div class="course-body">
    <div class="row between">
      <small><%: Course.Difficulty %> &middot; <%= Course.Duration %> weeks</small>
      <%= Ui.Tag(Manage ? Course.Status : Enrolled ? "Enrolled" : "Open") %>
    </div>
    <h3><%: Course.Title %></h3>
    <p><%: Course.Description %></p>
    <small class="cc-by"><span class="cc-face" aria-hidden="true"><%: Ui.Initials(InstructorName) %></span>By <%: InstructorName %><span class="cc-dot" aria-hidden="true"></span><%= Activities %> activities</small>
    <%= Enrolled && !Manage ? Ui.Bar(Progress) : "" %>
    <div class="actions"><a class="button <%= Enrolled ? "primary" : "" %>" href="<%= Url %>"><%= Manage ? "Manage course" : Enrolled ? "Continue learning" : "View course" %> &rarr;</a></div>
  </div>
</article>
