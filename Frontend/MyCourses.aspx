<%@ Page Title="My courses" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="MyCourses.aspx.cs" Inherits="SKILL_UP.Frontend.MyCoursesPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<% if (Selected == null) { %>
<section class="my-courses mc-empty">
  <h1 id="page-title" tabindex="-1"><img class="mc-pixel-title" src="assets/my-courses/my-course-pixel.png" alt="My course"></h1>
  <h2>No programs available yet</h2>
  <p>Your learning programs will appear here once published.</p>
  <a href="Catalog" class="button">Explore courses</a>
</section>
<% } else { %>
<section class="my-courses">
  <header class="mc-page-header"><h1 id="page-title" tabindex="-1"><img class="mc-pixel-title" src="assets/my-courses/my-course-pixel.png" alt="My course"></h1></header>
  <div class="mc-layout">
    <aside class="mc-programs" aria-label="Programs">
      <div class="mc-program-scroll" role="tablist" aria-label="Choose a program" aria-orientation="vertical">
        <asp:Repeater ID="ProgramTabs" runat="server">
          <ItemTemplate>
            <div class="mc-program" data-selected="<%# (bool)Eval("Selected") ? "true" : "false" %>" data-kit="<%# (bool)Eval("Enrolled") ? "open" : (bool)Eval("HasCourses") ? "closed" : "empty" %>"<%# (bool)Eval("JustOpened") ? " data-just-opened=\"true\"" : "" %>>
              <a class="mc-program-link" id="mc-tab-<%# Container.ItemIndex %>" role="tab" aria-selected="<%# (bool)Eval("Selected") ? "true" : "false" %>" aria-controls="mc-courses-panel" href="MyCourses?program=<%# Eval("Id") %>" aria-label="<%#: Eval("Name") %><%# (bool)Eval("Enrolled") ? ", enrolled" : ", not enrolled" %>"><img src="assets/my-courses/skillup-kit-<%# (bool)Eval("Enrolled") ? "open" : "closed" %>.jpg" alt=""><span class="mc-program-caption"><%#: Eval("Name") %></span></a>
              <%# (bool)Eval("Enrolled") ? "<span class=\"mc-program-cta is-enrolled\" aria-hidden=\"true\"><span class=\"mc-cta-label\">Enrolled</span><span class=\"mc-cta-icon\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m5 12.5 4.5 4.5L19 7.5\"/></svg></span></span>" : "" %>
              <skill:SubmitButton runat="server" CssClass="mc-program-cta" CausesValidation="false" Visible='<%# !(bool)Eval("Enrolled") %>' CommandArgument='<%# Eval("Id") %>' OnCommand="EnrolProgram_Command" aria-label='<%# "Enroll in " + Eval("Name") %>'><span class="mc-cta-label">Enroll</span><span class="mc-cta-icon" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4.5 4.5 20 12 4.5 19.5 7.5 12Z"/><path d="M7.5 12H13"/></svg></span></skill:SubmitButton>
            </div>
          </ItemTemplate>
        </asp:Repeater>
      </div>
    </aside>
    <section class="mc-courses-panel" id="mc-courses-panel" role="tabpanel">
      <header class="mc-panel-header"><div><small>Selected program</small><h2 id="mc-program-title"><%: Selected.Name %></h2></div></header>
      <div class="mc-course-scroll" tabindex="0" aria-label="Courses in <%: Selected.Name %>">
        <asp:Repeater ID="CourseList" runat="server">
          <ItemTemplate>
            <article class="mc-course" data-course-id="<%# Eval("Id") %>">
              <div class="mc-course-metrics">
                <div class="mc-course-index"><span class="mc-number"><%# (Container.ItemIndex + 1).ToString("00") %></span></div>
                <div class="mc-course-gauge"><%# Eval("Ring") %></div>
              </div>
              <a class="mc-course-art" href="Course?id=<%# Eval("Id") %>" aria-label="View course: <%#: Eval("Title") %>"><img src="<%#: Eval("Art") %>" alt="" loading="eager"></a>
              <section class="mc-course-summary" aria-label="<%#: Eval("Title") %> progress">
                <div class="mc-market-accent">
                  <div class="mc-market-marks" aria-hidden="true"><span><svg class="mc-ico" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 5h6c1 0 1.5 1 1.5 1S13 5 14 5h5v14h-5c-1 0-1.5 1-1.5 1s-.5-1-1.5-1H5Z"/><path d="M12.5 6v14"/></svg></span><span><svg class="mc-ico" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m6 12 4 4 8-8"/></svg></span></div>
                  <strong><%# Eval("Percent") %><sup>%</sup></strong><p>Completed</p>
                </div>
                <div class="mc-market-main">
                  <header><h3><%#: Eval("Title") %></h3><div class="mc-market-tags"><span><%# Eval("Status") %></span><span><%# Eval("Done") %>/<%# Eval("Lessons") %></span></div></header>
                  <p class="mc-market-subtitle"><%#: Eval("Difficulty") %> &middot; <%# Eval("Lessons") %> activities</p>
                  <p class="mc-market-date"><span>Due date</span><time><%# Eval("Due") %></time></p>
                  <footer>
                    <a href="Course?id=<%# Eval("Id") %>" class="mc-market-details" aria-label="Course details: <%#: Eval("Title") %>"><svg class="mc-ico" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="5" width="4" height="4" rx="1"/><rect x="15" y="5" width="4" height="4" rx="1"/><rect x="5" y="15" width="4" height="4" rx="1"/><rect x="15" y="15" width="4" height="4" rx="1"/></svg></a>
                    <a class="mc-market-action" href="<%# Eval("Href") %>" aria-label="<%# Eval("Action") %>: <%#: Eval("Title") %>" title="<%# Eval("Action") %>"><svg class="mc-ico" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 17 17 7M9 7h8v8"/></svg></a>
                  </footer>
                </div>
              </section>
            </article>
          </ItemTemplate>
        </asp:Repeater>
        <asp:PlaceHolder ID="NoCourses" runat="server"><div class="mc-empty"><h2>No courses here yet</h2><p>Published courses for this program will appear here.</p><a class="button" href="Catalog">Explore courses</a></div></asp:PlaceHolder>
      </div>
    </section>
  </div>
</section>
<% } %>
</asp:Content>
<asp:Content ContentPlaceHolderID="PageScripts" runat="server">
  <script src="js/pack-opening.js" defer></script>
</asp:Content>
