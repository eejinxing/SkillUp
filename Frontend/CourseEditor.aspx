<%@ Page Title="Course editor" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="CourseEditor.aspx.cs" Inherits="SKILL_UP.Frontend.CourseEditorPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:PlaceHolder ID="Denied" runat="server" Visible="false"><div class="empty"><h3>Course access denied</h3><p>New items will appear here when they are available.</p></div></asp:PlaceHolder>
<asp:PlaceHolder ID="Editor" runat="server">
<section class="course-info-page" data-locked="<%= Locked ? "true" : "false" %>">
  <div class="ci-top-links"><a class="ci-back" href="Courses">&larr; My courses</a><% if (Me.Role == "Instructor") { %><a class="ci-back" href="CourseRequests">My requests &rarr;</a><% } %></div>
  <header class="ci-header">
    <span class="ci-title-icon"><img src="assets/course-editor/imgVector.svg" alt=""><span><%= Icons.Book %></span></span>
    <h1><%: Fresh ? "Create a course" : C.Title %></h1>
    <skill:SubmitButton ID="HeaderSave" runat="server" CssClass="ci-save" ValidationGroup="course" OnClick="Save_Click" aria-label="Save draft"><img src="assets/course-editor/imgAddLine.svg" alt="">Save</skill:SubmitButton>
  </header>
  <div class="ci-layout">
    <aside class="ci-summary">
      <img class="ci-summary-surface" src="assets/course-editor/imgVector1.svg" alt="">
      <span class="ci-course-mark" role="img" aria-label="Course icon"><%: Mark %></span>
      <a class="ci-program-link" href="Courses"><span><%= Icons.Folder %></span><span><%: ProgramName ?? "Select a program" %></span><span>&rsaquo;</span></a>
      <dl>
        <div><dt>Assigned instructor</dt><dd><%: InstructorName %></dd></div>
        <div><dt>Program</dt><dd><%: ProgramName ?? "Not selected" %></dd></div>
        <div><dt>Difficulty</dt><dd><%: Fresh ? Difficulty.SelectedValue : C.Difficulty %></dd></div>
        <div><dt>Course status</dt><dd><%: Status %></dd></div>
        <div><dt>Deadline</dt><dd><%: C?.Deadline?.ToString("yyyy-MM-dd") ?? "Not set" %></dd></div>
      </dl>
      <skill:SubmitButton ID="AssignToggle" runat="server" CssClass="ci-assignment" Text="Change assignment" CausesValidation="false" OnClick="AssignToggle_Click" />
      <asp:Panel ID="AssignForm" runat="server" CssClass="ci-assign-form" Visible="false" DefaultButton="AssignSave">
        <label>Instructor<asp:DropDownList ID="AssignInstructor" runat="server" DataTextField="Name" DataValueField="Id" /></label>
        <div class="actions"><skill:SubmitButton ID="AssignSave" runat="server" CssClass="primary" Text="Save assignment" CausesValidation="false" OnClick="Assign_Click" /><a class="button ghost" href="<%= EditorUrl(null) %>">Cancel</a></div>
      </asp:Panel>
    </aside>
    <div class="ci-content">
      <div class="ci-tabs" role="tablist" aria-label="Course editor sections">
        <img src="assets/course-editor/imgVector2.svg" alt="">
        <skill:SubmitButton ID="TabInformation" runat="server" role="tab" CausesValidation="false" Text="Information" CommandArgument="0" OnCommand="Tab_Command" />
        <skill:SubmitButton ID="TabLearners" runat="server" role="tab" CausesValidation="false" Text="Learners" CommandArgument="1" OnCommand="Tab_Command" />
        <skill:SubmitButton ID="TabResources" runat="server" role="tab" CausesValidation="false" Text="Learning Resources" CommandArgument="2" OnCommand="Tab_Command" />
      </div>
      <% if (Fresh) { %><p class="ci-new-help">Save the course to enable Learners and Learning Resources.</p><% } %>
      <% if (Deleting) { %><p class="ci-lock" role="status">Deletion pending review &middot; Withdraw the request from My requests before editing.</p><% } else if (Locked) { %><p class="ci-lock" role="status">Pending review &middot; Withdraw the submission before editing course information or activities.</p><% } %>
      <asp:MultiView ID="Panels" runat="server" ActiveViewIndex="0">
        <asp:View runat="server">
          <div role="tabpanel" data-ci-panel="information" aria-labelledby="<%= TabInformation.ClientID %>">
          <asp:Panel runat="server" DefaultButton="SaveButton">
            <fieldset class="ci-fields"<%= Locked ? " disabled" : "" %>>
              <legend class="sr-only">Course information</legend>
              <div class="ci-field-grid">
                <label>Course Name *<asp:TextBox ID="CourseTitle" runat="server" MaxLength="180" /><asp:RequiredFieldValidator runat="server" ControlToValidate="CourseTitle" ValidationGroup="course" ErrorMessage="Give the course a name." Display="Dynamic" CssClass="field-error" /></label>
                <label>Program *<asp:DropDownList ID="ProgramList" runat="server" DataTextField="Name" DataValueField="Id" /></label>
                <label>Deadline<asp:TextBox ID="Deadline" runat="server" TextMode="Date" /></label>
                <label>Assigned Instructor<input value="<%: InstructorName %>" readonly aria-readonly="true"></label>
                <label>Difficulty<asp:DropDownList ID="Difficulty" runat="server"><asp:ListItem>Beginner</asp:ListItem><asp:ListItem>Intermediate</asp:ListItem><asp:ListItem>Advanced</asp:ListItem></asp:DropDownList></label>
                <label>Course Status<input value="<%: Status %>" readonly aria-readonly="true"></label>
              </div>
              <label class="ci-rich-label">Course Description *<span class="ci-capsule ci-description"><span class="ci-capsule-art" aria-hidden="true"><%= CourseEditorArt.Description %></span><asp:TextBox ID="Description" runat="server" TextMode="MultiLine" MaxLength="2000" placeholder="Describe what learners will study and practise…" aria-label="Course description" /></span>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="Description" ValidationGroup="course" ErrorMessage="Describe the course." Display="Dynamic" CssClass="field-error" /></label>
              <label class="ci-rich-label">Learning Outcomes *<span class="ci-capsule ci-outcomes"><span class="ci-capsule-art" aria-hidden="true"><%= CourseEditorArt.Outcomes %></span><asp:TextBox ID="Outcomes" runat="server" TextMode="MultiLine" MaxLength="1000" placeholder="Write one learning outcome per line…" aria-label="Learning outcomes (one per line)" /></span>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="Outcomes" ValidationGroup="course" ErrorMessage="Add at least one learning outcome." Display="Dynamic" CssClass="field-error" /></label>
              <div class="ci-field-grid ci-schedule">
                <label>Estimated Duration (weeks)<asp:TextBox ID="Duration" runat="server" TextMode="Number" min="1" max="104" step="1" />
                  <asp:RequiredFieldValidator runat="server" ControlToValidate="Duration" ValidationGroup="course" ErrorMessage="Enter the duration in weeks." Display="Dynamic" CssClass="field-error" />
                  <asp:RangeValidator runat="server" ControlToValidate="Duration" Type="Integer" MinimumValue="1" MaximumValue="104" ValidationGroup="course" ErrorMessage="Use 1 to 104 weeks." Display="Dynamic" CssClass="field-error" /></label>
                <label>Start Date<asp:TextBox ID="StartDate" runat="server" TextMode="Date" /></label>
                <label>Visibility<input value="<%= C?.Status == "Published" ? "Visible to learners" : "Hidden until published" %>" readonly aria-readonly="true"></label>
              </div>
            </fieldset>
            <p class="ci-save-state" role="status">Changes require Program Leader review before publication.</p>
            <div class="ci-form-actions"><skill:SubmitButton ID="SaveButton" runat="server" CssClass="ci-save" Text="Save changes" ValidationGroup="course" OnClick="Save_Click" /><a class="ci-discard button" href="<%= EditorUrl(null) %>">Discard changes</a></div>
          </asp:Panel>
          <section class="ci-capsule ci-review" aria-label="Program Leader review">
            <span class="ci-capsule-art" aria-hidden="true"><%= CourseEditorArt.Review %></span>
            <div class="ci-review-content">
              <div><small>PROGRAM LEADER REVIEW</small><p class="ci-review-status" data-tone="<%= Deleting ? "warn" : Status == "Published" ? "ok" : Status == "Pending review" ? "warn" : "idle" %>"><%= Status == "Pending review" ? "Awaiting approval" : Status == "Published" ? "Approved and published" : "Save your course and submit it for review." %></p></div>
              <% if (Deleting) { %><a class="button" href="CourseRequests">View deletion request &rarr;</a><% } else if (Fresh) { %><span>Save draft first</span><% } %>
              <skill:SubmitButton ID="WithdrawReview" runat="server" Text="Withdraw submission" CausesValidation="false" CommandArgument="withdraw" OnCommand="CourseAction_Command" data-confirm="Withdraw this submission? The course returns to Draft." />
              <skill:SubmitButton ID="SubmitReview" runat="server" CssClass="ci-submit-review" Text="Submit for approval" CausesValidation="false" CommandArgument="submit" OnCommand="CourseAction_Command" data-confirm="Submit this course and its current materials to the Program Leader for review?" />
            </div>
          </section>
          <asp:PlaceHolder ID="SavedParts" runat="server">
          <section class="ci-activities">
            <header><div><h2>Learning activities</h2><p><%= Ui.Plural(Lessons.Count, "activity", "activities") %> &middot; <%= TotalMinutes %> min total</p></div><fieldset<%= Locked ? " disabled" : "" %>><skill:SubmitButton runat="server" CssClass="ci-add-activity" Text="+ Add activity" CausesValidation="false" CommandArgument="new" OnCommand="EditLesson_Command" /></fieldset></header>
            <asp:Panel ID="LessonEditor" runat="server" CssClass="ci-lesson-editor" DefaultButton="SaveLessonButton" Visible="false">
              <h3 id="lesson-editor"><%= EditingLesson == null ? "Add learning activity" : "Edit activity" %></h3>
              <div class="form-grid">
                <label class="full">Activity title<asp:TextBox ID="LessonTitle" runat="server" MaxLength="200" /><asp:RequiredFieldValidator runat="server" ControlToValidate="LessonTitle" ValidationGroup="lesson" ErrorMessage="Give the activity a title." Display="Dynamic" CssClass="field-error" /></label>
                <label>Activity type<asp:DropDownList ID="LessonType" runat="server"><asp:ListItem>article</asp:ListItem><asp:ListItem>lab</asp:ListItem><asp:ListItem>quiz</asp:ListItem></asp:DropDownList></label>
                <label>Estimated minutes<asp:TextBox ID="LessonMinutes" runat="server" TextMode="Number" min="1" max="600" /></label>
                <label class="full">Learning content / instructions<asp:TextBox ID="LessonBody" runat="server" TextMode="MultiLine" Rows="6" /><asp:RequiredFieldValidator runat="server" ControlToValidate="LessonBody" ValidationGroup="lesson" ErrorMessage="Add the content or instructions." Display="Dynamic" CssClass="field-error" /></label>
                <label class="full">Expected answer (required for labs)<asp:TextBox ID="LessonAnswer" runat="server" MaxLength="1000" /></label>
                <label class="check"><asp:CheckBox ID="LessonPreview" runat="server" /> Allow public article preview</label>
              </div>
              <div class="actions"><skill:SubmitButton ID="SaveLessonButton" runat="server" CssClass="primary" Text="Save activity" ValidationGroup="lesson" OnClick="SaveLesson_Click" /><a class="button ghost" href="<%= EditorUrl(null) %>">Cancel</a></div>
            </asp:Panel>
            <asp:Repeater ID="LessonList" runat="server">
              <HeaderTemplate><ol class="ci-activity-list"></HeaderTemplate>
              <ItemTemplate>
                <li class="ci-activity"><span><%# (Container.ItemIndex + 1).ToString("00") %></span><div><strong><%#: Eval("Title") %></strong><p><em class="ci-kind" data-kind="<%#: Eval("Type") %>"><%#: Eval("Type") %></em><%# Eval("Minutes") %> min<%# (bool)Eval("Preview") ? " &middot; Public preview" : "" %></p></div>
                  <fieldset<%# Locked ? " disabled" : "" %>><skill:SubmitButton runat="server" Text="Edit" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="EditLesson_Command" /><skill:SubmitButton runat="server" CssClass="danger" Text="Delete" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="DeleteLesson_Command" data-confirm="Delete this activity? Existing progress will be recalculated from the remaining activities." /></fieldset></li>
              </ItemTemplate>
              <FooterTemplate></ol></FooterTemplate>
            </asp:Repeater>
            <% if (Lessons.Count == 0) { %><p class="ci-empty">Add an article, lab or quiz to build your course.</p><% } %>
          </section>
          <section class="ci-assessment"><span class="ci-assessment-icon" aria-hidden="true"><%= Icons.Check %></span><div><h2>Assessment</h2><p><%= QuizSummary %></p></div><a class="button" href="QuizEditor?id=<%= C?.Id %>">Edit quiz &rarr;</a></section>
          <details class="ci-history"><summary>Review history</summary><%= History %></details>
          <div class="ci-danger-actions">
            <div><strong>Course visibility</strong><p>Hide the course from learners or <%= Me.Role == "Instructor" ? "ask the Program Leader to remove it" : "remove it permanently" %>.</p></div>
            <skill:SubmitButton runat="server" Text="Hide course" CausesValidation="false" CommandArgument="hide" OnCommand="CourseAction_Command" data-confirm="Hide this course? Learners will no longer be able to open it." />
            <skill:SubmitButton ID="DeleteCourse" runat="server" CssClass="danger" Text="Delete course" CausesValidation="false" CommandArgument="delete" OnCommand="CourseAction_Command" data-confirm="Delete this course permanently?" />
            <skill:SubmitButton ID="AskDeletion" runat="server" CssClass="danger" Text="Request deletion" CausesValidation="false" OnClick="AskDeletion_Click" />
          </div>
          <asp:Panel ID="DeletionForm" runat="server" CssClass="ci-lesson-editor ci-delete-request" DefaultButton="SendDeletion" Visible="false">
            <h3>Request course deletion</h3>
            <p><%: C?.Title %></p>
            <label>Reason for deletion<asp:TextBox ID="DeleteReason" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="DeleteReason" ValidationGroup="deletion" ErrorMessage="Give a reason for the deletion." Display="Dynamic" CssClass="field-error" /></label>
            <p class="notice">Program Leader approval is required. Courses with learners can be hidden instead.</p>
            <div class="actions"><skill:SubmitButton ID="SendDeletion" runat="server" CssClass="danger" Text="Submit deletion request" ValidationGroup="deletion" OnClick="RequestDeletion_Click" /><a class="button ghost" href="<%= EditorUrl(null) %>">Cancel</a></div>
          </asp:Panel>
          </asp:PlaceHolder>
          </div>
        </asp:View>
        <asp:View runat="server">
          <div role="tabpanel" data-ci-panel="learners" aria-labelledby="<%= TabLearners.ClientID %>">
          <header class="ci-section-heading"><h2>Enrolled learners</h2><a class="button" href="Learners?course=<%= C?.Id %>">View student performance &rarr;</a></header>
          <skill:LearnerTable ID="LearnerTable" runat="server" />
          </div>
        </asp:View>
        <asp:View runat="server">
          <div role="tabpanel" data-ci-panel="resources" aria-labelledby="<%= TabResources.ClientID %>">
          <skill:CourseFiles ID="Files" runat="server" />
          </div>
        </asp:View>
      </asp:MultiView>
    </div>
  </div>
</section>
</asp:PlaceHolder>
</asp:Content>
