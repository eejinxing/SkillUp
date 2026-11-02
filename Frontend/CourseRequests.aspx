<%@ Page Title="My requests" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="CourseRequests.aspx.cs" Inherits="SKILL_UP.Frontend.CourseRequestsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="course-requests-page rq-data-view rq-grid-view">
  <div class="rq-main">
    <div class="rq-breadcrumb crumb-trail"><a href="Dashboard"><svg viewBox="0 0 16 16" aria-hidden="true"><path d="M10 3.5 5.5 8l4.5 4.5" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg>Workspace</a><i aria-hidden="true">/</i><span class="is-current">Course requests</span></div>
    <header class="rq-title"><div><h1>My requests</h1><p>Track course creation and deletion submissions</p></div>
      <div class="rq-tool-group rq-table-actions">
        <skill:SubmitButton ID="ExportButton" runat="server" ClientIDMode="Static" CssClass="rq-tool" Text="Export CSV" CausesValidation="false" OnClick="Export_Click" />
        <skill:SubmitButton ID="NewToggle" runat="server" CssClass="rq-new" Text="＋ New request" CausesValidation="false" OnClick="NewToggle_Click" />
      </div>
    </header>
    <asp:Panel ID="NewForm" runat="server" CssClass="panel rq-new-form" DefaultButton="SendRequest" Visible="false">
      <h2>Submit course request</h2>
      <div class="form-grid">
        <label>Request type<asp:DropDownList ID="NewKind" runat="server"><asp:ListItem Value="Create">Create / update course</asp:ListItem><asp:ListItem Value="Delete">Delete course</asp:ListItem></asp:DropDownList></label>
        <label>Course<asp:DropDownList ID="NewCourse" runat="server" DataTextField="Title" DataValueField="Id" /></label>
        <label class="full">Reason for request<asp:TextBox ID="NewReason" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="NewReason" ValidationGroup="request" ErrorMessage="Give a reason for the request." Display="Dynamic" CssClass="field-error" /></label>
      </div>
      <p class="notice">Create requests need saved activities. Courses with learners can be hidden instead of deleted.</p>
      <div class="actions"><skill:SubmitButton ID="SendRequest" runat="server" CssClass="primary" Text="Submit request" ValidationGroup="request" OnClick="SendRequest_Click" /><a class="button ghost" href="<%: Request.RawUrl %>">Cancel</a></div>
    </asp:Panel>
    <div class="rq-summary"><%= Summary %></div>
    <asp:Panel runat="server" CssClass="rq-grid-toolbar" DefaultButton="ApplyFilters">
      <div class="rq-tool-group">
        <span class="rq-tool rq-selected-chip"><%= Draw("<rect x=\"4\" y=\"4\" width=\"16\" height=\"16\" rx=\"4\"/><path d=\"m8.5 12 2.5 2.5 4.5-5\"/>") %><span id="rq-selection-count" role="status">0 selected</span></span>
        <button type="button" class="rq-tool" id="rq-clear-selection" hidden>Clear</button>
        <label class="rq-tool rq-table-search"><%= Draw("<circle cx=\"11\" cy=\"11\" r=\"6\"/><path d=\"m20 20-4.2-4.2\"/>") %><span class="sr-only">Search requests</span><asp:TextBox ID="SearchBox" runat="server" TextMode="Search" aria-label="Search requests" placeholder="Search requests" /></label>
        <label class="rq-tool rq-select-tool"><%= Draw("<path d=\"M4 6h16M7 12h10M10 18h4\"/>") %><span class="sr-only">Request type</span><asp:DropDownList ID="KindFilter" runat="server" aria-label="Request type"><asp:ListItem Value="">All types</asp:ListItem><asp:ListItem Value="Create">Creating</asp:ListItem><asp:ListItem Value="Delete">Deleting</asp:ListItem></asp:DropDownList></label>
        <label class="rq-tool rq-select-tool"><span class="rq-tool-dot" aria-hidden="true"></span><span class="sr-only">Request status</span><asp:DropDownList ID="StatusFilter" runat="server" aria-label="Request status"><asp:ListItem Value="">All statuses</asp:ListItem><asp:ListItem>Pending review</asp:ListItem><asp:ListItem>Approved</asp:ListItem><asp:ListItem>Changes requested</asp:ListItem><asp:ListItem>Rejected</asp:ListItem><asp:ListItem>Withdrawn</asp:ListItem></asp:DropDownList></label>
        <label class="rq-tool rq-select-tool"><%= Draw("<path d=\"M7 4v16M4 17l3 3 3-3M17 20V4M14 7l3-3 3 3\"/>") %><span class="sr-only">Sort requests</span><asp:DropDownList ID="SortFilter" runat="server" aria-label="Sort requests"><asp:ListItem Value="newest">Newest first</asp:ListItem><asp:ListItem Value="oldest">Oldest first</asp:ListItem><asp:ListItem Value="name">Course A–Z</asp:ListItem><asp:ListItem Value="name-rev">Course Z–A</asp:ListItem><asp:ListItem Value="priority">Priority: high to low</asp:ListItem><asp:ListItem Value="priority-rev">Priority: low to high</asp:ListItem><asp:ListItem Value="program">Program A–Z</asp:ListItem><asp:ListItem Value="program-rev">Program Z–A</asp:ListItem><asp:ListItem Value="status">Status A–Z</asp:ListItem><asp:ListItem Value="status-rev">Status Z–A</asp:ListItem></asp:DropDownList></label>
        <skill:SubmitButton ID="ApplyFilters" runat="server" CssClass="rq-tool" Text="Apply" CausesValidation="false" OnClick="Apply_Click" />
        <span id="rq-result-count" class="rq-count" role="status"><%= Ui.Plural(Filtered.Count, "request") %></span>
      </div>
    </asp:Panel>
    <div class="rq-table-wrap" tabindex="0" aria-label="Course requests table">
      <table class="rq-data-table">
        <caption class="sr-only">Your submitted course requests with priority, program, type, size, reviewer, status, submission date and next step</caption>
        <thead><tr><th class="rq-check-cell" scope="col"><input id="rq-select-all" type="checkbox" aria-label="Select all visible requests"></th><%= Headers %></tr></thead>
        <tbody id="rq-request-list"><%= Rows %></tbody>
      </table>
    </div>
    <nav class="rq-pager" id="rq-pager" aria-label="Request pages">
      <div class="rq-pager-info"><label>Rows per page <asp:DropDownList ID="PageSize" runat="server" aria-label="Rows per page" AutoPostBack="true" OnSelectedIndexChanged="Apply_Click"><asp:ListItem>10</asp:ListItem><asp:ListItem>20</asp:ListItem><asp:ListItem>50</asp:ListItem></asp:DropDownList></label><span>Showing <b><%= From %>–<%= To %></b> of <b><%= Filtered.Count %></b></span></div>
      <div class="rq-pager-pages"><%= PageLinks %></div>
    </nav>
  </div>
  <aside class="rq-side">
    <header class="rq-overview"><h2>Request activity</h2><div class="rq-panel-nav" id="rq-panel-nav"><%= Nav %></div></header>
    <div id="rq-request-detail" class="rq-request-detail">
      <asp:PlaceHolder ID="NoDetail" runat="server"><div class="rq-timeline-empty"><span><%= Draw("<path d=\"M6 4h12v16H6Z\"/><path d=\"M9 8h6M9 12h6M9 16h3\"/>") %></span><h2>No request selected</h2><p>Select a request to see its activity and review history.</p></div></asp:PlaceHolder>
      <asp:PlaceHolder ID="Detail" runat="server">
        <%= DetailHead %>
        <div class="rq-tabs" role="tablist" aria-label="Request details"><span class="rq-tab-glider" aria-hidden="true"></span><a class="rq-tab" role="tab" href="<%: Url(id: Active?.Row.Id, page: PageNumber, tab: "overview") %>" aria-selected="<%= Tab == "overview" ? "true" : "false" %>">Overview</a><a class="rq-tab" role="tab" href="<%: Url(id: Active?.Row.Id, page: PageNumber, tab: "activity") %>" aria-selected="<%= Tab == "activity" ? "true" : "false" %>">Activity</a></div>
        <div class="rq-tab-panel" data-rq-panel="overview"<%= Tab == "overview" ? "" : " hidden" %>><%= Overview %></div>
        <div class="rq-tab-panel" data-rq-panel="activity"<%= Tab == "activity" ? "" : " hidden" %>><%= Activity %></div>
        <div class="rq-detail-actions"><skill:SubmitButton ID="WithdrawButton" runat="server" CssClass="rq-withdraw" Text="Withdraw request" CausesValidation="false" OnCommand="Withdraw_Command" data-confirm="Withdraw this request? It will remain in your history with a Withdrawn status." /><small><%= DetailNote %></small></div>
      </asp:PlaceHolder>
    </div>
  </aside>
</section>
</asp:Content>
