<%@ Page Title="AI Assistant" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Assistant.aspx.cs" Inherits="SKILL_UP.Frontend.AssistantPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
  <div class="page-head"><div><h1 tabindex="-1" id="page-title">AI Assistant</h1><p>A little guidance for your next step.</p></div><div class="actions tight"></div></div>
  <div class="split assistant-split">
    <section class="panel">
      <div class="bubbles">
        <asp:Repeater ID="History" runat="server">
          <ItemTemplate><div class="bubble mine"><%#: Eval("Prompt") %></div><div class="bubble"><%#: Eval("Response") %><small><%= Companion.Configured ? "Skill UP companion" : "Sample AI response" %> &middot; <%# Eval("Date") %></small></div></ItemTemplate>
        </asp:Repeater>
        <asp:PlaceHolder ID="Welcome" runat="server"><div class="orb" aria-hidden="true"></div><h2 style="text-align:center">What would you like to understand?</h2><p style="text-align:center">Try &ldquo;Explain aggregation&rdquo; or &ldquo;How do I improve my progress?&rdquo;</p></asp:PlaceHolder>
      </div>
      <%-- Opens the conversation at its newest message; the page itself does not scroll (css/secondary-pages.css). --%>
      <script>(function (log) { var end = function () { log.scrollTop = log.scrollHeight; }; end(); addEventListener('load', end); })(document.currentScript.previousElementSibling);</script>
      <asp:Panel runat="server" DefaultButton="AskButton" CssClass="assistant-ask">
        <label>Ask a question
          <asp:TextBox ID="Question" runat="server" TextMode="MultiLine" Rows="2" MaxLength="4000" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="Question" ErrorMessage="Type a question first." Display="Dynamic" CssClass="field-error" />
        </label>
        <div class="actions"><skill:SubmitButton ID="AskButton" runat="server" CssClass="primary" Text="Ask companion" OnClick="Ask_Click" /></div>
      </asp:Panel>
    </section>
    <aside class="panel">
      <h2>Learn with intention</h2>
      <div class="list-item"><strong>One concept at a time</strong><p>Describe what you tried and where you got stuck.</p></div>
      <div class="list-item"><strong>Keep practising</strong><p>Apply the explanation to an activity in your course.</p></div>
      <p class="notice"><%= Companion.Configured ? "The companion is powered by AI and can make mistakes, so check anything important." : "Demo companion: responses come from simple templates on the server." %> Your questions are saved in your account&rsquo;s history.</p>
    </aside>
  </div>
</asp:Content>
