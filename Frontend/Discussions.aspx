<%@ Page Title="Course discussions" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Discussions.aspx.cs" Inherits="SKILL_UP.Frontend.DiscussionsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
  <div class="page-head">
    <div><h1 tabindex="-1" id="page-title">Course discussions</h1><p>Ask a question, share an idea and learn together.</p></div>
    <div class="actions tight"><a class="button primary" href="#new-discussion">+ New discussion</a></div>
  </div>
  <details class="panel inline-form" id="new-discussion"<%= NewOpen ? " open" : "" %>>
    <summary>Start a discussion</summary>
    <asp:Panel runat="server" DefaultButton="PostButton">
      <label>Course <asp:DropDownList ID="PostCourse" runat="server" DataTextField="Title" DataValueField="Id" /></label>
      <label>Your question or idea
        <asp:TextBox ID="PostText" runat="server" TextMode="MultiLine" Rows="4" MaxLength="4000" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="PostText" ValidationGroup="post" ErrorMessage="Write your question or idea." Display="Dynamic" CssClass="field-error" />
      </label>
      <div class="actions"><skill:SubmitButton ID="PostButton" runat="server" CssClass="primary" Text="Post discussion" ValidationGroup="post" OnClick="Post_Click" /></div>
    </asp:Panel>
  </details>
  <div class="stack">
    <asp:Repeater ID="Posts" runat="server">
      <ItemTemplate>
        <article class="panel">
          <div class="row between"><div class="row"><%# Eval("AvatarHtml") %><div><strong><%#: Eval("Author") %></strong><p><%#: Eval("Course") %> &middot; <%# Eval("Date") %></p></div></div><%# Eval("StatusTag") %></div>
          <p style="margin:18px 0;white-space:pre-line"><%#: Eval("Text") %></p>
          <asp:Repeater runat="server" DataSource='<%# Eval("Replies") %>'>
            <ItemTemplate><div class="reply"><strong><%#: Eval("Author") %></strong> <%# Eval("RoleTag") %><p><%#: Eval("Text") %></p><small><%# Eval("Date") %></small></div></ItemTemplate>
          </asp:Repeater>
          <div class="actions">
            <details class="inline-form">
              <summary class="button ghost">Reply</summary>
              <asp:TextBox ID="ReplyText" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" placeholder="Your reply" aria-label="Your reply" />
              <skill:SubmitButton runat="server" CssClass="primary" Text="Post reply" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Reply_Command" />
            </details>
            <asp:PlaceHolder runat="server" Visible="<%# CanModerate %>">
            <details class="inline-form">
              <summary class="button ghost">Moderate</summary>
              <label>Visibility
                <asp:DropDownList ID="ModerateStatus" runat="server" SelectedValue='<%# Eval("Status") %>'><asp:ListItem>Visible</asp:ListItem><asp:ListItem>Flagged</asp:ListItem><asp:ListItem>Hidden</asp:ListItem><asp:ListItem>Removed</asp:ListItem></asp:DropDownList>
              </label>
              <label>Reason <asp:TextBox ID="ModerateReason" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500" Text='<%# Eval("Reason") %>' /></label>
              <skill:SubmitButton runat="server" CssClass="primary" Text="Save decision" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Moderate_Command" />
            </details>
            </asp:PlaceHolder>
          </div>
          <%# string.IsNullOrEmpty((string)Eval("Reason")) ? "" : "<small>Moderation reason: " + Ui.H(Eval("Reason")) + "</small>" %>
        </article>
      </ItemTemplate>
    </asp:Repeater>
    <asp:PlaceHolder ID="NoPosts" runat="server"><div class="empty"><h3>Start the conversation</h3><p>Choose a course and ask your first question.</p></div></asp:PlaceHolder>
  </div>
</asp:Content>
