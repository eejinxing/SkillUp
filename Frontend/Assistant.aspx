<%@ Page Title="AI Assistant" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Assistant.aspx.cs" Inherits="SKILL_UP.Frontend.AssistantPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
  <div class="page-head"><div><h1 tabindex="-1" id="page-title">AI Assistant</h1><p>A little guidance for your next step.</p></div><div class="actions tight"></div></div>
  <div class="split assistant-split">
    <%-- Marks a short window (zoom-aware, so it matches what the person really sees) before the page is first drawn; js/site.js keeps it right on resize. --%>
    <script>(function (split) { var zoom = parseFloat(getComputedStyle(document.documentElement).zoom) || 1, h = innerHeight / zoom; split.classList.toggle('is-short', h < 800); split.classList.toggle('is-tiny', h < 660); })(document.currentScript.parentElement);</script>
    <%-- The same companion as on an activity page (css/activity.css, the script in js/site.js), widened to fill the page. --%>
    <div class="answer-page assistant-stage">
      <aside class="answer-ai answer-ai-reference assistant-companion <%= HasChat ? "has-conversation" : "" %>" aria-label="AI Assistant" data-api="<%= ResolveUrl("~/api/commands/") %>" data-lesson="<%= SelectedLesson %>">
        <header class="answer-ai-top">
          <a href="Assistant" class="answer-ai-brand" aria-label="Skill UP companion">SKILL<span>companion</span></a>
          <span class="answer-ai-where"><%: WhereText %></span>
          <details class="answer-ai-options"><summary aria-label="Companion options"><span aria-hidden="true">&bull;&bull;&bull;</span></summary><div><% if (ChatLessonUrl != null) { %><a href="<%= ChatLessonUrl %>">Open activity &#8599;</a><% } else { %><a href="MyCourses">My courses &#8599;</a><% } %></div></details>
        </header>
        <div class="answer-ai-intro">
          <div class="answer-orb"><img src="assets/answer/imgRectangle4.png" alt="Blue and violet AI companion orb"></div>
          <h2><span>Your next <em>big</em></span> <span><em>move</em> starts here.</span></h2>
        </div>
        <div class="answer-ai-conversation" aria-live="polite">
          <asp:Repeater ID="History" runat="server"><ItemTemplate><div class="answer-ai-prompt"><span class="ai-avatar"><%= Ui.Avatar(Me) %></span><small>You &middot; <%# ChatTime(Eval("PromptAt")) %></small><p><%#: Eval("Prompt") %></p></div><div class="answer-ai-reply"><span class="ai-avatar bot" aria-hidden="true"></span><small>Skill UP companion &middot; <%# ChatTime(Eval("ResponseAt")) %></small><p><%#: Eval("Response") %></p></div></ItemTemplate></asp:Repeater>
        </div>
        <div class="answer-ai-bottom">
          <div class="answer-ai-books" aria-hidden="true">
            <div class="answer-ai-book"><small>SKILL UP</small><b>Your next<br>chapter.</b><i></i></div>
            <div class="answer-ai-book"><small>PRACTICE</small><b>Learn.<br>Try. Repeat.</b><i></i></div>
            <div class="answer-ai-book"><small>FIELD NOTES</small><b>Make it<br>click.</b><i></i></div>
          </div>
          <div class="answer-ai-suggest" role="group" aria-label="Suggested questions">
            <asp:Repeater ID="Suggestions" runat="server"><ItemTemplate><skill:SubmitButton runat="server" CausesValidation="false" CommandArgument='<%# Eval("Question") %>' data-question='<%# Eval("Question") %>' OnCommand="Ask_Command"><span><%#: Eval("Title") %></span><small><%#: Eval("Detail") %></small></skill:SubmitButton></ItemTemplate></asp:Repeater>
          </div>
          <asp:Panel runat="server" CssClass="answer-ai-composer" DefaultButton="AskButton">
            <div class="answer-ai-input-wrap">
              <skill:SubmitButton ID="ContextButton" runat="server" CssClass="answer-ai-add" CausesValidation="false" OnClick="Context_Click" aria-label="Ask about this activity" title="Ask about this activity"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" aria-hidden="true"><path d="M12 4v16M4 12h16"/></svg></skill:SubmitButton>
              <label class="sr-only" for="AskBox">Ask your learning companion</label>
              <asp:TextBox ID="AskBox" runat="server" ClientIDMode="Static" MaxLength="2000" autocomplete="off" placeholder="Curious? Let’s explore together" />
            </div>
            <skill:SubmitButton ID="AskButton" runat="server" CssClass="answer-ai-send" CausesValidation="false" OnClick="Ask_Click" aria-label="Send question"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 19 19 5M5 5h14v14"/></svg></skill:SubmitButton>
            <p class="form-error" role="alert"><asp:Literal ID="AskError" runat="server" /></p>
          </asp:Panel>
        </div>
      </aside>
    </div>
    <aside class="assistant-convos" aria-label="Conversations">
      <h2 class="assistant-label">Conversations</h2>
      <label class="assistant-search"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg><span class="sr-only">Find a conversation</span><input type="search" placeholder="Find a conversation…" autocomplete="off" aria-label="Find a conversation" data-conversation-search></label>
      <nav class="assistant-convo-list" aria-label="Your conversations"><%= ConversationsHtml %></nav>
      <p class="assistant-nomatch" hidden>No conversations match.</p>
      <p class="assistant-note"><%= Companion.Configured ? "The companion is powered by AI and can make mistakes, so check anything important." : "Demo companion: responses come from simple templates on the server." %> Your questions are saved in your account&rsquo;s history.</p>
    </aside>
  </div>
</asp:Content>
