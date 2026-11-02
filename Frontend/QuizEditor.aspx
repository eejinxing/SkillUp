<%@ Page Title="Question editor" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="QuizEditor.aspx.cs" Inherits="SKILL_UP.Frontend.QuizEditorPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:PlaceHolder ID="Denied" runat="server" Visible="false"><div class="empty"><h3>Course access denied</h3><p></p></div></asp:PlaceHolder>
<asp:PlaceHolder ID="Editor" runat="server">
<section class="quiz-editor-page" data-locked="<%= Locked ? "true" : "false" %>">
  <header class="answer-breadcrumb"><a href="CourseEditor?id=<%= C.Id %>">&larr; Course editor</a><h1><%: C.Title %> <span>/ Question editor</span></h1><span class="qe-course-status"><%: C.Status %></span></header>
  <% if (Deleting) { %><p class="qe-review-lock" role="status">Deletion pending review. Withdraw it from My requests before changing questions.</p><% } else if (Locked) { %><p class="qe-review-lock" role="status">This course is pending review. Withdraw the review in the course editor before changing questions.</p><% } %>
  <div class="answer-layout">
    <aside class="answer-ai" aria-label="AI teaching companion">
      <div class="answer-orb"><img src="assets/quiz-editor/imgRectangle4.png" alt="Blue and violet AI companion orb"></div>
      <h2>Lets Go Through It</h2>
      <div class="answer-ai-conversation" id="qe-ai-history" aria-live="polite">
        <asp:Repeater ID="Conversation" runat="server"><ItemTemplate><div class="answer-ai-prompt"><span class="ai-avatar"><%= Ui.Avatar(Me) %></span><small>You &middot; <%# ChatTime(Eval("PromptAt")) %></small><p><%#: Eval("Prompt") %></p></div><div class="answer-ai-reply"><span class="ai-avatar bot" aria-hidden="true"></span><small>Teaching companion &middot; sample reply &middot; <%# ChatTime(Eval("ResponseAt")) %></small><p><%#: Eval("Response") %></p></div></ItemTemplate></asp:Repeater>
      </div>
      <asp:Panel runat="server" CssClass="answer-ai-composer" DefaultButton="AskButton">
        <img class="answer-composer-surface" src="assets/quiz-editor/imgFrame177.svg" alt="">
        <label class="sr-only" for="AskBox">Ask the teaching companion</label>
        <asp:TextBox ID="AskBox" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="2" MaxLength="1000" placeholder="Ask Anything" />
        <skill:SubmitButton runat="server" CssClass="answer-ai-add" CausesValidation="false" OnClick="UseContext_Click" aria-label="Use question context" title="Use question context"><img src="assets/quiz-editor/imgAddLine.svg" alt=""></skill:SubmitButton>
        <skill:SubmitButton ID="AskButton" runat="server" CssClass="answer-ai-send" CausesValidation="false" OnClick="Ask_Click" aria-label="Send companion question"><img src="assets/quiz-editor/imgVector2.svg" alt=""></skill:SubmitButton>
        <p class="form-error" role="alert"><asp:Literal ID="AskError" runat="server" /></p>
      </asp:Panel>
    </aside>
    <div class="answer-workspace">
      <div class="answer-banner" role="img" aria-label="MARATHON"><div class="answer-banner-crop"><img src="assets/quiz-editor/imgRectangle3.png" alt=""></div></div>
      <asp:Panel runat="server" CssClass="qe-form" DefaultButton="SaveButton">
        <fieldset class="qe-editable"<%= Locked ? " disabled" : "" %>>
          <legend class="sr-only">Quiz settings and questions</legend>
          <div class="qe-settings">
            <label>Time limit <span>(minutes)</span><asp:TextBox ID="TimeLimit" runat="server" TextMode="Number" min="1" max="180" step="1" /></label>
            <label class="qe-publish"><asp:CheckBox ID="Published" runat="server" />Enable quiz for learners</label>
            <skill:SubmitButton ID="SaveButton" runat="server" CssClass="qe-save" Text="Save quiz" CausesValidation="false" OnClick="Save_Click" />
            <p>Saving returns the course to Draft. Program Leader approval is required before learners see changes.</p>
            <span data-quiz-save-status role="status"><%= Changed ? "Unsaved changes kept for this session &middot; Save quiz to apply" : "All questions saved" %></span>
          </div>
          <div id="quiz-questions">
            <asp:Repeater ID="Questions" runat="server">
              <ItemTemplate>
                <fieldset class="qe-question answer-task quiz-question" data-question="<%# Eval("Index") %>">
                  <legend class="sr-only">Question <%# Eval("Number") %></legend>
                  <asp:HiddenField ID="QuestionId" runat="server" Value='<%# Eval("Id") %>' />
                  <header class="answer-task-heading"><span class="answer-task-label">Task <%# Eval("Number") %></span><skill:SubmitButton runat="server" CssClass="qe-remove" Text="Remove" CausesValidation="false" CommandArgument='<%# Eval("Index") %>' OnCommand="RemoveQuestion_Command" aria-label='<%# "Remove question " + Eval("Number") %>' /></header>
                  <label class="sr-only" for="<%# Container.FindControl("Prompt").ClientID %>">Question <%# Eval("Number") %></label>
                  <asp:TextBox ID="Prompt" runat="server" CssClass="qe-prompt" TextMode="MultiLine" Rows="2" MaxLength="2000" placeholder="Write your question here" Text='<%# Eval("Text") %>' />
                  <div class="qe-options" role="group" aria-label="Answer options for question <%# Eval("Number") %>">
                    <asp:Repeater ID="Options" runat="server" DataSource='<%# Eval("Options") %>'>
                      <ItemTemplate>
                        <div class="qe-option"><img class="qe-option-outline" src="assets/quiz-editor/<%# Eval("Outline") %>" alt="">
                          <input class="qe-correct" type="radio" name="answer<%# Eval("Question") %>" value="<%# Eval("Index") %>"<%# (bool)Eval("Correct") ? " checked" : "" %> aria-label="Question <%# Eval("QuestionNumber") %>: option <%# Eval("Number") %> is correct">
                          <label class="sr-only" for="<%# Container.FindControl("OptionText").ClientID %>">Question <%# Eval("QuestionNumber") %> option <%# Eval("Number") %></label>
                          <asp:TextBox ID="OptionText" runat="server" data-option-text="" Text='<%# Eval("Text") %>' MaxLength="500" placeholder='<%# "Write option " + Eval("Number") %>' />
                          <skill:SubmitButton runat="server" CssClass="qe-remove-option" Text="×" CausesValidation="false" CommandArgument='<%# Eval("Question") + ":" + Eval("Index") %>' OnCommand="RemoveOption_Command" Enabled='<%# Eval("Removable") %>' aria-label='<%# "Remove question " + Eval("QuestionNumber") + " option " + Eval("Number") %>' />
                        </div>
                      </ItemTemplate>
                    </asp:Repeater>
                  </div>
                  <div class="qe-option-tools"><small>Select the circle beside the correct answer.</small><skill:SubmitButton runat="server" Text="＋ Add option" CausesValidation="false" CommandArgument='<%# Eval("Index") %>' OnCommand="AddOption_Command" Enabled='<%# Eval("CanAddOption") %>' /></div>
                  <div class="qe-question-bottom">
                    <label class="qe-topic">Topic / skill<asp:TextBox ID="Topic" runat="server" Text='<%# Eval("Topic") %>' MaxLength="100" placeholder="e.g. Query methods" /></label>
                    <div class="answer-task-actions">
                      <skill:SubmitButton runat="server" CssClass="answer-glass" CausesValidation="false" OnClick="Save_Click" aria-label='<%# "Save quiz from question " + Eval("Number") %>' title="Save all quiz questions"><img src="assets/quiz-editor/imgRectangle1.png" alt="Submit"></skill:SubmitButton>
                      <skill:SubmitButton runat="server" CssClass="answer-glass" CausesValidation="false" CommandArgument='<%# Eval("Index") %>' OnCommand="Hint_Command" aria-label='<%# "Question " + Eval("Number") + " authoring hint" %>' title="Question authoring hint"><img src="assets/quiz-editor/imgRectangle.png" alt="Hint"></skill:SubmitButton>
                    </div>
                  </div>
                  <asp:PlaceHolder runat="server" Visible='<%# Eval("HintOpen") %>'><div class="notice qe-hint" role="note"><p>Test one clear concept per question. Write plausible, distinct options and select one correct answer using the circle beside it.</p><p>The Submit key saves the entire quiz. Add a topic to group learner mistakes in performance reports.</p></div></asp:PlaceHolder>
                </fieldset>
              </ItemTemplate>
            </asp:Repeater>
            <asp:PlaceHolder ID="NoQuestions" runat="server"><div class="answer-task qe-empty"><h2>No questions yet</h2><p>Add a question or generate a sample draft, then review it before saving.</p></div></asp:PlaceHolder>
          </div>
          <footer class="qe-editor-actions">
            <skill:SubmitButton runat="server" Text="＋ Add question" CausesValidation="false" OnClick="AddQuestion_Click" />
            <skill:SubmitButton runat="server" Text="Generate sample AI draft" CausesValidation="false" OnClick="SampleDraft_Click" />
            <skill:SubmitButton runat="server" Text="Discard changes" CausesValidation="false" OnClick="Discard_Click" />
            <skill:SubmitButton runat="server" CssClass="qe-delete" Text="Delete quiz" CausesValidation="false" OnClick="DeleteQuiz_Click" data-confirm="Delete this quiz? Questions will be removed. Add a new quiz before publishing any quiz activity." />
            <small>Sample drafts are local templates. Check every answer before publishing.</small>
          </footer>
        </fieldset>
      </asp:Panel>
    </div>
  </div>
</section>
</asp:PlaceHolder>
</asp:Content>
