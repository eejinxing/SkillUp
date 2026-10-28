<%@ Page Title="Activity" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Activity.aspx.cs" Inherits="SKILL_UP.Frontend.ActivityPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:MultiView ID="Screens" runat="server" ActiveViewIndex="0">
  <%-- 0: the activity --%>
  <asp:View runat="server">
<div class="answer-page" data-answer-course="<%= C.Id %>" data-answer-lesson="<%= L.Id %>">
  <header class="answer-breadcrumb">
    <a href="Course?id=<%= C.Id %>">&larr; Course overview</a>
    <h1><%: C.Title %> <span>/ <%: L.Title %></span></h1>
    <% if (Guest) { %><span>Preview</span><% } else if (QuizReady) { %><span class="timer" id="quiz-timer" aria-label="Quiz time remaining" data-seconds-left="<%= SecondsLeft %>"><%= SecondsLeft / 60 %>:<%= (SecondsLeft % 60).ToString("00") %> remaining</span><% } else { %><span><%= Percent %>% complete</span><% } %>
  </header>
  <div class="answer-layout">
    <aside class="answer-ai answer-ai-reference <%= History.Count > 0 ? "has-conversation" : "" %>" aria-label="AI learning companion">
      <header class="answer-ai-top">
        <a href="Assistant" class="answer-ai-brand" aria-label="Open Skill UP companion">SKILL<span>companion</span></a>
        <details class="answer-ai-options"><summary aria-label="Companion options"><span aria-hidden="true">&bull;&bull;&bull;</span></summary><div><a href="Assistant">Open companion &#8599;</a></div></details>
      </header>
      <div class="answer-ai-intro">
        <div class="answer-orb"><img src="assets/answer/imgRectangle4.png" alt="Blue and violet AI companion orb"></div>
        <h2><span>Your next <em>big</em></span><span><em>move</em> starts here.</span></h2>
      </div>
      <div class="answer-ai-conversation" aria-live="polite">
        <asp:Repeater ID="Conversation" runat="server"><ItemTemplate><div class="answer-ai-prompt"><small>You</small><p><%#: Eval("Prompt") %></p></div><div class="answer-ai-reply"><small>Skill UP companion</small><p><%#: Eval("Response") %></p></div></ItemTemplate></asp:Repeater>
      </div>
      <div class="answer-ai-bottom">
        <div class="answer-ai-books" aria-hidden="true">
          <div class="answer-ai-book"><small>SKILL UP</small><b>Your next<br>chapter.</b><i></i></div>
          <div class="answer-ai-book"><small>PRACTICE</small><b>Learn.<br>Try. Repeat.</b><i></i></div>
          <div class="answer-ai-book"><small>FIELD NOTES</small><b>Make it<br>click.</b><i></i></div>
        </div>
        <div class="answer-ai-suggest" role="group" aria-label="Suggested questions">
          <asp:Repeater ID="Suggestions" runat="server"><ItemTemplate><skill:SubmitButton runat="server" CausesValidation="false" Enabled="<%# !Guest %>" CommandArgument='<%# Eval("Question") %>' OnCommand="Ask_Command"><span><%#: Eval("Title") %></span><small><%#: Eval("Detail") %></small></skill:SubmitButton></ItemTemplate></asp:Repeater>
        </div>
        <asp:Panel runat="server" CssClass="answer-ai-composer" DefaultButton="AskButton">
          <div class="answer-ai-input-wrap">
            <skill:SubmitButton ID="ContextButton" runat="server" CssClass="answer-ai-add" CausesValidation="false" OnClick="Context_Click" aria-label="Ask about this activity" title="Ask about this activity"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" aria-hidden="true"><path d="M12 4v16M4 12h16"/></svg></skill:SubmitButton>
            <label class="sr-only" for="AskBox">Ask your learning companion</label>
            <asp:TextBox ID="AskBox" runat="server" ClientIDMode="Static" MaxLength="1000" autocomplete="off" />
          </div>
          <skill:SubmitButton ID="AskButton" runat="server" CssClass="answer-ai-send" CausesValidation="false" OnClick="Ask_Click" aria-label="Send question"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 19 19 5M5 5h14v14"/></svg></skill:SubmitButton>
          <p class="form-error" role="alert"><asp:Literal ID="AskError" runat="server" /></p>
        </asp:Panel>
      </div>
    </aside>
    <div class="answer-workspace">
      <%= Banner %>
      <% if (IsCompleted) { %><p class="answer-saved">&#10003; This activity is completed. You can review it again.</p><% } %>
      <% if (Guest) { %>
      <section class="answer-guest-note" role="note">
        <div><strong>You&rsquo;re previewing this activity</strong><p>Log in to answer, use hints, ask the companion and save your progress.<%= HiddenQuestions > 0 ? " " + HiddenQuestions + " more question" + (HiddenQuestions == 1 ? " is" : "s are") + " hidden in this preview." : "" %></p></div>
        <div><a class="answer-guest-login" href="Login?return=<%= Server.UrlEncode(Request.RawUrl) %>">Log in</a><a class="answer-guest-register" href="Register">Create account</a></div>
      </section>
      <% } %>
      <skill:TerminalLab ID="LabBefore" runat="server" />
      <%-- The task: reading, lab answer or quiz --%>
      <asp:PlaceHolder ID="TaskArea" runat="server">
        <asp:MultiView ID="Task" runat="server">
          <%-- article --%>
          <asp:View runat="server">
            <section class="answer-task">
              <div class="answer-task-heading"><span class="answer-task-label">Task <%= TaskNo %></span><small>Reading &middot; <%= L.Minutes %> min</small></div>
              <div class="answer-prompt answer-reading"><h2><%: L.Title %></h2><p><%: L.Body %></p></div>
              <div class="answer-task-actions">
                <% if (!Guest && IsCompleted) { %><span class="answer-completed">&#10003; Completed</span><% } %>
                <skill:SubmitButton ID="CompleteButton" runat="server" CssClass="answer-glass answer-submit" CausesValidation="false" OnClick="Complete_Click" aria-label="Mark as complete" title="Mark as complete"><img src="assets/answer/imgRectangle1.png" alt="" draggable="false"></skill:SubmitButton>
                <skill:SubmitButton ID="HintArticle" runat="server" CssClass="answer-glass answer-hint" CausesValidation="false" OnClick="Hint_Click" aria-label="Hint" title="Hint"><img src="assets/answer/imgRectangle.png" alt="" draggable="false"></skill:SubmitButton>
              </div>
            </section>
          </asp:View>
          <%-- lab --%>
          <asp:View runat="server">
            <asp:Panel runat="server" CssClass="answer-task" DefaultButton="LabSubmit">
              <div class="answer-task-heading"><span class="answer-task-label">Task <%= TaskNo %></span><small>Question &middot; <%= L.Minutes %> min</small></div>
              <div class="answer-prompt"><h2><%: L.Title %></h2><p><%: L.Body %></p></div>
              <div class="answer-answer-row">
                <label class="answer-input-label" for="LabAnswer">Your answer
                  <asp:TextBox ID="LabAnswer" runat="server" ClientIDMode="Static" autocomplete="off" spellcheck="false" />
                </label>
                <div class="answer-task-actions">
                  <skill:SubmitButton ID="LabSubmit" runat="server" CssClass="answer-glass answer-submit" CausesValidation="false" OnClick="LabSubmit_Click" aria-label="Submit answer" title="Submit answer"><img src="assets/answer/imgRectangle1.png" alt="" draggable="false"></skill:SubmitButton>
                  <skill:SubmitButton ID="HintLab" runat="server" CssClass="answer-glass answer-hint" CausesValidation="false" OnClick="Hint_Click" aria-label="Hint" title="Hint"><img src="assets/answer/imgRectangle.png" alt="" draggable="false"></skill:SubmitButton>
                </div>
              </div>
              <p class="form-error" role="alert"><asp:Literal ID="LabError" runat="server" /></p>
            </asp:Panel>
          </asp:View>
          <%-- quiz --%>
          <asp:View runat="server">
            <% if (!QuizReady) { %>
            <section class="answer-task"><span class="answer-task-label">Task <%= TaskNo %></span><h2>Quiz not available</h2><p>Your instructor is preparing this assessment.</p></section>
            <% } %>
            <asp:PlaceHolder ID="QuizForm" runat="server">
            <div class="answer-task answer-quiz<%= Guest ? " answer-guest-task" : "" %>">
              <div class="answer-task-heading"><span class="answer-task-label">Task <%= TaskNo %></span><small><%= Guest ? "Preview &middot; " + Questions.Count + " of " + Quiz.Questions.Count : Quiz.Questions.Count.ToString() %> questions &middot; Pass mark 70%</small></div>
              <asp:Repeater ID="QuizQuestions" runat="server">
                <ItemTemplate>
                  <fieldset class="answer-question">
                    <legend><span><%# Eval("Number") %><%#: Eval("Text") %></span></legend>
                    <div class="answer-choices">
                      <asp:Repeater runat="server" DataSource='<%# Eval("Options") %>'>
                        <ItemTemplate><label class="answer-choice"><img class="answer-choice-outline" src="assets/answer/imgFrame167.svg" alt=""><input type="radio" name="<%# Eval("Name") %>" value="<%# Eval("Index") %>"<%# (bool)Eval("Checked") ? " checked" : "" %><%# Guest ? " disabled aria-disabled=\"true\"" : "" %>><span class="answer-radio" aria-hidden="true"><img class="answer-radio-empty" src="assets/answer/imgVector.svg" alt=""><img class="answer-radio-selected" src="assets/answer/imgVector1.svg" alt=""></span><span class="answer-choice-text"><%#: Eval("Text") %></span></label></ItemTemplate>
                      </asp:Repeater>
                    </div>
                  </fieldset>
                </ItemTemplate>
              </asp:Repeater>
              <p class="form-error" role="alert"><asp:Literal ID="QuizError" runat="server" /></p>
              <div class="answer-task-actions">
                <skill:SubmitButton ID="QuizSubmit" runat="server" CssClass="answer-glass answer-submit" CausesValidation="false" OnClick="QuizSubmit_Click" aria-label="Submit answers" title="Submit answers"><img src="assets/answer/imgRectangle1.png" alt="" draggable="false"></skill:SubmitButton>
                <skill:SubmitButton ID="HintQuiz" runat="server" CssClass="answer-glass answer-hint" CausesValidation="false" OnClick="Hint_Click" aria-label="Hint" title="Hint"><img src="assets/answer/imgRectangle.png" alt="" draggable="false"></skill:SubmitButton>
                <skill:SubmitButton ID="RestartButton" runat="server" CssClass="ghost" Text="Restart quiz" CausesValidation="false" Visible="false" OnClick="Restart_Click" />
              </div>
            </div>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="QuizResult" runat="server" Visible="false">
            <section class="answer-task answer-quiz-result" role="status">
              <span class="answer-task-label"><%= ResultPassed ? "Quiz passed" : "Keep practising" %></span>
              <h1><%= ResultScore %>%</h1>
              <p><%= ResultPassed ? "This activity is now complete." : "You need 70% to pass. Review the explanations and try again." %></p>
              <asp:Repeater ID="ResultFeedback" runat="server"><ItemTemplate><div class="list-item"><strong class="result <%# (bool)Eval("Correct") ? "correct" : "incorrect" %>"><%# (bool)Eval("Correct") ? "&#10003;" : "&times;" %> <%#: Eval("Text") %></strong><p>Your answer: <%#: Eval("Selected") %><br>Correct answer: <%#: Eval("Answer") %></p></div></ItemTemplate></asp:Repeater>
              <div class="actions"><a class="button primary" href="Course?id=<%= C.Id %>">Back to course</a><skill:SubmitButton ID="TryAgainButton" runat="server" CssClass="ghost" Text="Try again" CausesValidation="false" OnClick="Restart_Click" /></div>
            </section>
            </asp:PlaceHolder>
          </asp:View>
        </asp:MultiView>
        <asp:PlaceHolder ID="HintPanel" runat="server" Visible="false"><div class="notice answer-hint-text" role="status"><strong>A small hint</strong><p><%: HintText %></p></div></asp:PlaceHolder>
      </asp:PlaceHolder>
      <skill:TerminalLab ID="LabAfter" runat="server" />
      <nav class="answer-activity-nav" aria-label="Activity navigation"><%= NavHtml %></nav>
    </div>
  </div>
</div>
  </asp:View>
  <%-- 1: unavailable --%>
  <asp:View runat="server">
    <div class="empty"><h3>Activity unavailable</h3><p><%: UnavailableText %></p></div>
    <% if (Me == null) { %><a class="button" href="Catalog">Explore courses</a><% } %>
  </asp:View>
  <%-- 2: locked for visitors --%>
  <asp:View runat="server">
    <div class="page-head"><div><h1 tabindex="-1" id="page-title">This activity is locked</h1><p></p></div><div class="actions tight"></div></div>
    <div class="empty"><h3>Log in to keep going</h3><p>Visitors can preview the first <%= CoursePage.GuestPreviewCount %> activities. Log in and enrol to unlock the rest of <%: C?.Title %>.</p></div>
    <div class="actions"><a class="button primary" href="Login?return=<%= Server.UrlEncode(Request.RawUrl) %>">Log in</a><a class="button" href="Course?id=<%= C?.Id %>">Back to course</a></div>
  </asp:View>
  <%-- 3: previous activity not done --%>
  <asp:View runat="server">
    <div class="page-head"><div><h1 tabindex="-1" id="page-title">One step at a time</h1><p></p></div><div class="actions tight"></div></div>
    <div class="empty"><h3>This activity is locked</h3><p>Complete the previous activity first.</p></div>
    <a class="button" href="Course?id=<%= C?.Id %>">Back to course</a>
  </asp:View>
</asp:MultiView>
</asp:Content>
<asp:Content ContentPlaceHolderID="PageScripts" runat="server">
  <script src="js/activity.js" defer></script>
</asp:Content>
