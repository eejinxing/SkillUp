<%@ Page Title="Course approvals" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Approvals.aspx.cs" Inherits="SKILL_UP.Frontend.ApprovalsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:MultiView ID="Screens" runat="server" ActiveViewIndex="0">

<%-- The request board: every Create / Delete request in the leader's programs. --%>
<asp:View runat="server">
<section class="leader-applications"><div class="la-window"><%= Rail %><div class="la-workspace">
  <header class="la-heading"><div><h1>Requests</h1><p>Review course creation and deletion applications</p></div>
    <div class="la-view"><skill:SubmitButton ID="BoardView" runat="server" Text="Board" CausesValidation="false" CommandArgument="board" OnCommand="View_Command" /><skill:SubmitButton ID="TableView" runat="server" Text="Table" CausesValidation="false" CommandArgument="table" OnCommand="View_Command" /></div>
  </header>
  <div class="la-date-heading"><span id="la-month"><%= Month %></span><skill:SubmitButton ID="AllDates" runat="server" Text="All dates" CausesValidation="false" OnClick="AllDates_Click" /><span id="la-date-status"><%= Filters.Date.Length > 0 ? "Submitted on " + Filters.Date : "All submission dates" %></span></div>
  <div class="la-date-strip">
    <skill:SubmitButton runat="server" Text="‹" CausesValidation="false" CommandArgument="-12" OnCommand="Shift_Command" aria-label="Previous 12 days" />
    <div id="la-days">
      <asp:Repeater ID="Days" runat="server"><ItemTemplate><skill:SubmitButton runat="server" CausesValidation="false" CommandArgument='<%# Eval("Iso") %>' OnCommand="Day_Command" aria-pressed='<%# Eval("Pressed") %>' aria-label='<%# "Submitted on " + Eval("Iso") %>'><small><%# Eval("Weekday") %></small><span><%# Eval("Day") %></span></skill:SubmitButton></ItemTemplate></asp:Repeater>
    </div>
    <skill:SubmitButton runat="server" Text="›" CausesValidation="false" CommandArgument="12" OnCommand="Shift_Command" aria-label="Next 12 days" />
  </div>
  <asp:Panel runat="server" CssClass="la-filters" DefaultButton="SearchButton">
    <label><span class="sr-only">Search course approvals</span><asp:TextBox ID="SearchBox" runat="server" ClientIDMode="Static" TextMode="Search" aria-label="Search course approvals" placeholder="Search course, instructor or request…" /></label>
    <label><span class="sr-only">Application type</span><asp:DropDownList ID="KindFilter" runat="server" aria-label="Application type" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed"><asp:ListItem Value="">All types</asp:ListItem><asp:ListItem Value="Create">Create / update</asp:ListItem><asp:ListItem Value="Delete">Delete course</asp:ListItem></asp:DropDownList></label>
    <label><span class="sr-only">Application program</span><asp:DropDownList ID="ProgramFilter" runat="server" aria-label="Application program" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed" /></label>
    <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Filter_Changed" />
  </asp:Panel>
  <p id="la-count" class="la-count" role="status"><%= Ui.Plural(Shown.Count, "application") %></p>
  <div id="approval-results"><%= Results %></div>
</div></div></section>
</asp:View>

<%-- One application: the request, the course as it stands, the decision form and the request's history. --%>
<asp:View runat="server">
<section class="leader-applications la-detail" data-application="<%= Current?.Id %>"><div class="la-window"><%= Rail %><div class="la-workspace">
  <a class="la-back" href="Approvals">&larr; All requests</a>
  <header class="la-heading"><div><h1><%= Current?.Kind == "Delete" ? "Delete course application" : "Course application" %></h1><p><%: Current?.CourseTitle %></p></div><span class="la-detail-status"><%: Current?.Status %></span></header>
  <div class="la-detail-grid">
    <div class="la-content"><%= Content %></div>
    <aside class="la-decision-side">
      <section class="la-detail-panel"><h2>Your decision</h2>
        <asp:PlaceHolder ID="DecisionForm" runat="server">
          <skill:SubmitButton ID="StartReview" runat="server" CssClass="la-start" Text="Start review" CausesValidation="false" OnClick="StartReview_Click" />
          <p class="la-help"><%= Current?.Kind == "Delete" ? "Approve to remove this course, or reject to keep it." : "Approve to publish this course to learners, reject it, or request changes." %> Feedback is required and shared with the instructor.</p>
          <asp:Panel runat="server" CssClass="la-decision-form" DefaultButton="SubmitDecision">
            <label>Decision<asp:DropDownList ID="Decision" runat="server" /></label>
            <label>Feedback to instructor<asp:TextBox ID="Feedback" runat="server" TextMode="MultiLine" Rows="5" MaxLength="2000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="Feedback" ValidationGroup="decision" ErrorMessage="Write feedback for the instructor." Display="Dynamic" CssClass="field-error" /></label>
            <div class="actions"><skill:SubmitButton ID="SubmitDecision" runat="server" CssClass="primary" Text="Submit decision" ValidationGroup="decision" OnClick="SubmitDecision_Click" /></div>
          </asp:Panel>
        </asp:PlaceHolder>
        <asp:PlaceHolder ID="ReadOnly" runat="server"><p class="la-copy"><%: Current?.Status %>. This application is read only.</p></asp:PlaceHolder>
      </section>
      <section class="la-detail-panel"><h2>Application progress</h2><ol class="la-timeline"><%= Timeline %></ol></section>
    </aside>
  </div>
</div></div></section>
</asp:View>

<asp:View runat="server">
<section class="leader-applications la-detail"><div class="la-window"><%= Rail %><div class="la-workspace"><h1>Request access denied</h1><p>This application is not assigned to your program or has been removed.</p><a href="Approvals">&larr; All requests</a></div></div></section>
</asp:View>
</asp:MultiView>
</asp:Content>
