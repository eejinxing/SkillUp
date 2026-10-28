<%@ Page Title="Live sessions" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="LiveSessions.aspx.cs" Inherits="SKILL_UP.Frontend.LiveSessionsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
  <div class="page-head">
    <div><h1 tabindex="-1" id="page-title">Live sessions</h1><p>Office hours and group discussions.</p></div>
    <div class="actions tight"><% if (Me.Role == "Instructor") { %><a class="button primary" href="#schedule">+ Schedule session</a><% } %></div>
  </div>
  <asp:PlaceHolder ID="SchedulePanel" runat="server">
  <details class="panel inline-form" id="schedule">
    <summary>Schedule a live session</summary>
    <asp:Panel runat="server" DefaultButton="ScheduleButton">
      <label>Course <asp:DropDownList ID="SessionCourse" runat="server" DataTextField="Title" DataValueField="Id" /></label>
      <label>Session title
        <asp:TextBox ID="SessionTitle" runat="server" MaxLength="100" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="SessionTitle" ValidationGroup="session" ErrorMessage="Give the session a title." Display="Dynamic" CssClass="field-error" />
      </label>
      <label>Start date and time
        <asp:TextBox ID="SessionStart" runat="server" TextMode="DateTimeLocal" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="SessionStart" ValidationGroup="session" ErrorMessage="Choose a session time." Display="Dynamic" CssClass="field-error" />
      </label>
      <div class="actions"><skill:SubmitButton ID="ScheduleButton" runat="server" CssClass="primary" Text="Schedule session" ValidationGroup="session" OnClick="Schedule_Click" /></div>
    </asp:Panel>
  </details>
  </asp:PlaceHolder>
  <div class="grid two">
    <asp:Repeater ID="Sessions" runat="server">
      <ItemTemplate>
        <section class="panel">
          <%# Eval("CourseTag") %>
          <h2 style="margin:14px 0"><%#: Eval("Title") %></h2>
          <p><%# Eval("When") %> &middot; <%#: Eval("Host") %></p>
          <small><%# Eval("Attendees") %> participants joined</small>
          <div class="actions">
            <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("Joined") %>'><skill:SubmitButton runat="server" CssClass="primary" Text="Join session" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Join_Command" /></asp:PlaceHolder>
            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("Joined") %>'>
              <button type="button" class="primary" data-start-call="video" data-call-name="<%#: Eval("Host") %>" data-call-role="Live session · local preview" data-call-title="<%#: Eval("Title") %>">Open session</button>
              <skill:SubmitButton runat="server" CssClass="ghost" Text="Leave" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Leave_Command" />
            </asp:PlaceHolder>
          </div>
        </section>
      </ItemTemplate>
    </asp:Repeater>
    <asp:PlaceHolder ID="NoSessions" runat="server"><div class="empty"><h3>No sessions scheduled</h3><p></p></div></asp:PlaceHolder>
  </div>
  <p class="notice">Joining is saved to your account. The call itself is a local preview: your camera is used only after you allow it, and audio and video are not transmitted.</p>
</asp:Content>
