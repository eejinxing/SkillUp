<%@ Page Title="Learners" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Learners.aspx.cs" Inherits="SKILL_UP.Frontend.LearnersPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:MultiView ID="Screens" runat="server" ActiveViewIndex="0">

<%-- One learner in one course: completion, the activities done, the topics they get wrong and every attempt. --%>
<asp:View runat="server">
  <div class="page-head"><div><h1 tabindex="-1" id="page-title"><%: LearnerName %></h1><p><%: DetailCourse?.Title %> &middot; Enrolled <%= Ui.Date(DetailEnrolment?.EnrolledAt) %></p></div><div class="actions tight"><a class="button ghost" href="Learners">&larr; Learners</a></div></div>
  <div class="metrics">
    <asp:Repeater ID="Metrics" runat="server"><ItemTemplate><div class="metric"><small><%# Eval("Label") %></small><strong class="number"><%# Eval("Value") %></strong><small><%# Eval("Note") %></small><span class="spark" aria-hidden="true">▂▃▅▆</span></div></ItemTemplate></asp:Repeater>
  </div>
  <div class="grid two">
    <section class="panel"><h2>Completed activities</h2>
      <asp:Repeater ID="LessonStates" runat="server"><ItemTemplate><div class="list-item row between"><div><%#: Eval("Title") %><p><%#: Eval("Type") %></p></div><%# Ui.Tag((string)Eval("State")) %></div></ItemTemplate></asp:Repeater>
    </section>
    <section class="panel"><h2>Topics to practise</h2>
      <asp:Repeater ID="Topics" runat="server"><ItemTemplate><div class="list-item row between"><span><%#: Eval("Topic") %></span><%# Ui.Tag(Eval("Count") + " incorrect attempts") %></div></ItemTemplate></asp:Repeater>
      <asp:PlaceHolder ID="NoTopics" runat="server"><div class="empty"><h3>No mistakes recorded</h3><p>Incorrect answers will be grouped by topic here.</p></div></asp:PlaceHolder>
      <div class="divider"></div><h3>Completion types</h3>
      <asp:Repeater ID="TypeCounts" runat="server"><ItemTemplate><div class="list-item row between"><span><%# Eval("Type") %></span><strong><%# Eval("Done") %> / <%# Eval("Total") %></strong></div></ItemTemplate></asp:Repeater>
    </section>
    <section class="panel"><h2>Attempt history</h2>
      <asp:Repeater ID="AttemptRows" runat="server">
        <HeaderTemplate><div class="table-wrap"><table><thead><tr><th scope="col">Type</th><th scope="col">Score</th><th scope="col">Date</th></tr></thead><tbody></HeaderTemplate>
        <ItemTemplate><tr><td><%# Eval("Type") %></td><td><%# Eval("Score") %>%</td><td><%# Eval("Date") %></td></tr></ItemTemplate>
        <FooterTemplate></tbody></table></div></FooterTemplate>
      </asp:Repeater>
      <asp:PlaceHolder ID="NoAttempts" runat="server"><div class="empty"><h3>No matching records</h3><p>Try another search or create a new record.</p></div></asp:PlaceHolder>
    </section>
  </div>
</asp:View>

<%-- Instructor: one card per enrolment in the courses they teach. --%>
<asp:View runat="server">
<section class="student-performance">
  <nav class="sp-rail" aria-label="Instructor quick navigation"><%= Rail %></nav>
  <div class="sp-main">
    <header class="sp-heading"><div><h1>Student performance</h1><p><%: RosterNote %></p></div><skill:SubmitButton ID="EnrolToggle" runat="server" CssClass="sp-enrol" Text="+ Enrol learner" CausesValidation="false" OnClick="EnrolToggle_Click" /></header>
    <asp:Panel ID="EnrolForm" runat="server" CssClass="panel sp-enrol-form" DefaultButton="EnrolButton" Visible="false">
      <h2>Enrol a learner</h2>
      <div class="form-grid">
        <label>Course<asp:DropDownList ID="EnrolCourse" runat="server" DataTextField="Title" DataValueField="Id" /></label>
        <label>Learner<asp:DropDownList ID="EnrolLearner" runat="server" DataTextField="Name" DataValueField="Id" /></label>
      </div>
      <div class="actions"><skill:SubmitButton ID="EnrolButton" runat="server" CssClass="primary" Text="Enrol learner" CausesValidation="false" OnClick="Enrol_Click" /><a class="button ghost" href="<%: Request.RawUrl %>">Cancel</a></div>
    </asp:Panel>
    <asp:Panel runat="server" CssClass="sp-filters" DefaultButton="RosterFilter">
      <label class="sp-search"><span class="sr-only">Search learners</span><asp:TextBox ID="RosterSearch" runat="server" TextMode="Search" placeholder="Search learners or courses…" aria-label="Search learners" /></label>
      <label><span class="sr-only">Filter by course</span><asp:DropDownList ID="RosterCourse" runat="server" aria-label="Filter by course" /></label>
      <label><span class="sr-only">Filter by progress</span><asp:DropDownList ID="RosterState" runat="server" aria-label="Filter by progress"><asp:ListItem Value="">All progress</asp:ListItem><asp:ListItem Value="completed">Completed</asp:ListItem><asp:ListItem Value="progress">In progress</asp:ListItem><asp:ListItem Value="not-started">Not started</asp:ListItem><asp:ListItem Value="needs-support">Quiz below 70%</asp:ListItem></asp:DropDownList></label>
      <label><span class="sr-only">Sort learners</span><asp:DropDownList ID="RosterSort" runat="server" aria-label="Sort learners"><asp:ListItem Value="name">Name A–Z</asp:ListItem><asp:ListItem Value="completion">Completion: highest first</asp:ListItem><asp:ListItem Value="recent">Recently active</asp:ListItem></asp:DropDownList></label>
      <skill:SubmitButton ID="RosterFilter" runat="server" CssClass="sp-apply" Text="Apply" CausesValidation="false" OnClick="RosterFilter_Click" />
    </asp:Panel>
    <p class="sp-count" aria-live="polite"><%= RosterCount %></p>
    <div class="sp-grid">
      <asp:Repeater ID="Cards" runat="server">
        <ItemTemplate>
          <article class="sp-card<%# (bool)Eval("Collapsed") ? " is-collapsed" : "" %>" id="sp-card-<%# Eval("EnrolmentId") %>"<%# (bool)Eval("Complete") ? " data-complete" : "" %>>
            <header class="sp-block-head"><asp:Literal runat="server" EnableViewState="false" Text='<%# Eval("Head") %>' /><skill:SubmitButton runat="server" CssClass="sp-toggle" CausesValidation="false" CommandArgument='<%# Eval("EnrolmentId") %>' OnCommand="Toggle_Command" aria-expanded='<%# (bool)Eval("Collapsed") ? "false" : "true" %>' aria-controls='<%# "sp-body-" + Eval("EnrolmentId") %>' aria-label='<%# Eval("ToggleLabel") %>'><%# Chevron %></skill:SubmitButton></header>
            <div class="sp-collapse" id="sp-body-<%# Eval("EnrolmentId") %>"<%# (bool)Eval("Collapsed") ? " inert" : "" %>><div class="sp-collapse-inner"><asp:Literal runat="server" EnableViewState="false" Text='<%# Eval("Body") %>' />
              <footer class="sp-block-foot"><span>Follow up?</span><div><a class="sp-open sp-pill sp-pill-primary" href="Learners?learner=<%# Eval("UserId") %>&amp;course=<%# Eval("CourseId") %>" aria-label="View <%#: Eval("Name") %> performance in <%#: Eval("CourseTitle") %>">View</a><a class="sp-pill" href="Messages?id=<%# Eval("UserId") %>">Message</a><skill:SubmitButton runat="server" CssClass="sp-remove sp-pill" Text="Remove enrolment" CausesValidation="false" CommandArgument='<%# Eval("UserId") + ":" + Eval("CourseId") %>' OnCommand="Remove_Command" data-confirm="Remove this enrolment? The learner's progress for this course will be removed." /></div></footer>
            </div></div>
          </article>
        </ItemTemplate>
      </asp:Repeater>
      <asp:PlaceHolder ID="NoCards" runat="server"><div class="sp-empty"><h2>No matching learners</h2><p>Try another search or filter. Enrol a learner to start tracking their progress.</p></div></asp:PlaceHolder>
    </div>
  </div>
</section>
</asp:View>

<%-- Program leader: every enrolment in the leader's programs, filtered by status (?status=) and course (?course=). --%>
<asp:View runat="server">
<div class="la-page la-kit lr-page" data-filter="<%= Status %>" data-expanded="<%= Expanded ? "true" : "false" %>">
  <nav class="kx-crumbs" aria-label="Breadcrumb"><a href="Dashboard"><%= Icons.KitHome %>Main</a><span aria-hidden="true">&rsaquo;</span><% if (ScopedTitle != null) { %><a href="Learners"><%= Icons.KitPeople %>Learners</a><span aria-hidden="true">&rsaquo;</span><span><%: ScopedTitle %></span><% } else { %><span><%= Icons.KitPeople %>Learners</span><% } %><div class="kx-crumb-actions"><a href="Analytics"><%= Icons.KitChart %>Analytics</a><button type="button" data-action="print"><%= Icons.KitPrint %>Print</button></div></nav>
  <header class="kx-title"><span class="kx-title-icon"><%= Icons.KitPeople %></span><h1 tabindex="-1" id="page-title">Learners</h1>
    <div class="kx-seg" role="group" aria-label="Filter by status">
      <skill:SubmitButton ID="StatusAll" runat="server" Text="All" CausesValidation="false" CommandArgument="all" OnCommand="Status_Command" />
      <skill:SubmitButton ID="StatusProgress" runat="server" Text="In progress" CausesValidation="false" CommandArgument="progress" OnCommand="Status_Command" />
      <skill:SubmitButton ID="StatusDone" runat="server" Text="Completed" CausesValidation="false" CommandArgument="done" OnCommand="Status_Command" />
      <skill:SubmitButton ID="StatusIdle" runat="server" Text="Not started" CausesValidation="false" CommandArgument="idle" OnCommand="Status_Command" />
    </div>
    <div class="kx-title-actions"></div>
  </header>
  <div class="kx-stats"><%= Stats %></div>
  <asp:Panel runat="server" CssClass="lr-toolbar kx-toolbar" DefaultButton="TableFilter">
    <label class="lr-search"><%= Icons.Search %><asp:TextBox ID="search" runat="server" ClientIDMode="Static" TextMode="Search" aria-label="Search learners" placeholder="Search learners or courses…" /></label>
    <asp:DropDownList ID="filter" runat="server" ClientIDMode="Static" aria-label="Filter by course" />
    <skill:SubmitButton ID="TableFilter" runat="server" CssClass="kx-btn" Text="Search" CausesValidation="false" OnClick="TableFilter_Click" />
  </asp:Panel>
  <section class="kx-panel lr-panel" id="results"><skill:LearnerTable ID="LeaderTable" runat="server" /></section>
  <skill:SubmitButton runat="server" CssClass="kx-more" CausesValidation="false" OnClick="More_Click">Load More <span aria-hidden="true">&rsaquo;</span></skill:SubmitButton>
  <p class="kx-foot"><%= ScopedTitle == null ? "" : Ui.H("Course: " + ScopedTitle) + " &middot; " %>Inspect individual progress, quiz results and common mistakes. Select View for a learner&rsquo;s attempts and topics to practise.</p>
</div>
</asp:View>

<asp:View runat="server"><div class="empty"><h3>Learner not found</h3><p>New items will appear here when they are available.</p></div></asp:View>
</asp:MultiView>
</asp:Content>
