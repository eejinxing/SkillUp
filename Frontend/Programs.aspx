<%@ Page Title="Program Management" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Programs.aspx.cs" Inherits="SKILL_UP.Frontend.ProgramsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="program-management<%= P != null ? " pm-editor" : "" %>"><%= Rail %><div class="pm-main">

<asp:PlaceHolder ID="Denied" runat="server" Visible="false"><h1>Program access denied</h1><p>This program is not assigned to you or has been removed.</p><a href="Programs">&larr; Program Management</a></asp:PlaceHolder>

<%-- Forms that used to open in a dialog unfold at the top of the page (?edit=, ?menu=, ?delete=, ?help=1, ?add=1, ?move=). --%>
<asp:Panel ID="ProgramForm" runat="server" CssClass="panel pm-dialog" DefaultButton="SaveProgram" Visible="false">
  <h2><%= FormProgram == null ? "Create program" : "Edit program" %></h2>
  <p class="dlg-note">A program groups courses into a learning path that learners follow in order.</p>
  <label>Program name<asp:TextBox ID="ProgramName" runat="server" MaxLength="80" autocomplete="off" placeholder="e.g. Data & Development" /><asp:RequiredFieldValidator runat="server" ControlToValidate="ProgramName" ValidationGroup="program" ErrorMessage="Name the program." Display="Dynamic" CssClass="field-error" /></label>
  <label>Description<asp:TextBox ID="ProgramDescription" runat="server" TextMode="MultiLine" Rows="4" MaxLength="400" placeholder="What will learners be able to do after this program?" /><asp:RequiredFieldValidator runat="server" ControlToValidate="ProgramDescription" ValidationGroup="program" ErrorMessage="Describe the program." Display="Dynamic" CssClass="field-error" /></label>
  <label class="check dlg-switch"><asp:CheckBox ID="ProgramPublished" runat="server" /><span><b>Publish learning path</b><small>Published programs appear in learners' learning paths.</small></span></label>
  <div class="actions"><skill:SubmitButton ID="SaveProgram" runat="server" CssClass="primary" Text="Save program" ValidationGroup="program" OnClick="SaveProgram_Click" /><a class="button ghost" href="<%: HomeUrl %>">Cancel</a></div>
</asp:Panel>
<asp:Panel ID="MenuPanel" runat="server" CssClass="panel pm-dialog" Visible="false">
  <h2><%: MenuProgram?.Name %></h2>
  <p>Manage this program and its assigned courses.</p>
  <div class="actions"><a class="button" href="Programs?id=<%= MenuProgram?.Id %>">Manage courses</a><a class="button" href="Programs?edit=<%= MenuProgram?.Id %>">Edit details</a><a class="button ghost danger" href="Programs?delete=<%= MenuProgram?.Id %>">Delete program</a><a class="button ghost" href="Programs">Close</a></div>
</asp:Panel>
<asp:Panel ID="DeletePanel" runat="server" CssClass="panel pm-dialog" Visible="false">
  <h2>Delete program?</h2>
  <p>Only programs with no courses can be deleted.</p>
  <div class="actions"><skill:SubmitButton runat="server" CssClass="danger" Text="Delete program" CausesValidation="false" OnClick="DeleteProgram_Click" /><a class="button ghost" href="<%: HomeUrl %>">Cancel</a></div>
</asp:Panel>
<asp:Panel ID="HelpPanel" runat="server" CssClass="panel pm-dialog" Visible="false">
  <h2>Manage programs</h2>
  <p>Create a program, assign its courses, then choose the order of its learning path.</p>
  <p>Open a program to add or remove courses from its path. Save path applies the new sequence. Removing a course from the path keeps its program assignment.</p>
  <p>Use Add existing course or Move program to transfer a course between programs you manage. Materials, enrolments and student progress stay with the course.</p>
  <div class="actions"><a class="button ghost" href="Programs">Close</a></div>
</asp:Panel>
<asp:Panel ID="MovePanel" runat="server" CssClass="panel pm-dialog" DefaultButton="MoveCourse" Visible="false">
  <h2><%: MoveTitle %></h2>
  <asp:PlaceHolder ID="MoveBlocked" runat="server"><p><%: MoveMessage %></p><div class="actions"><%= MoveAction %><a class="button ghost" href="<%: HomeUrl %>">Cancel</a></div></asp:PlaceHolder>
  <asp:PlaceHolder ID="MoveFields" runat="server">
    <label><%= MoveLabel %><asp:DropDownList ID="MoveChoice" runat="server" DataTextField="Text" DataValueField="Value" /></label>
    <label class="check"><asp:CheckBox ID="IncludeInPath" runat="server" Checked="true" />Include in the target learning path</label>
    <p class="notice">This moves the course assignment to the selected program. Materials, publication status, enrolments and student progress are retained. Courses awaiting review must be withdrawn first.</p>
    <div class="actions"><skill:SubmitButton ID="MoveCourse" runat="server" CssClass="primary" Text="Move course" CausesValidation="false" OnClick="MoveCourse_Click" /><a class="button ghost" href="<%: HomeUrl %>">Cancel</a></div>
  </asp:PlaceHolder>
</asp:Panel>

<asp:PlaceHolder ID="Library" runat="server">
  <header class="pm-header"><div><h1>Program Management</h1><p><%= Ui.Plural(Rows.Count, "managed program") %></p></div>
    <asp:Panel runat="server" CssClass="pm-header-tools" DefaultButton="SearchButton">
      <label><span class="sr-only">Search programs</span><asp:TextBox ID="SearchBox" runat="server" ClientIDMode="Static" TextMode="Search" aria-label="Search programs" placeholder="Search programs…" /></label>
      <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Search_Click" />
      <skill:SubmitButton runat="server" CssClass="pm-create" Text="+ New program" CausesValidation="false" CommandArgument="Programs?edit=new" OnCommand="Open_Command" />
    </asp:Panel>
  </header>
  <div class="pm-grid" id="pm-program-grid">
    <%= Cards %>
    <article class="pm-new"><div class="pm-new-art" aria-hidden="true"><%= ProgramArt.Plane() %></div><h2>New Program</h2><p>Create a program and organise courses into a clear learning path.</p>
      <div><skill:SubmitButton runat="server" Text="＋ Add new program" data-action="editProgram" CausesValidation="false" CommandArgument="Programs?edit=new" OnCommand="Open_Command" /><skill:SubmitButton runat="server" Text="Learn more" CausesValidation="false" CommandArgument="Programs?help=1" OnCommand="Open_Command" /></div></article>
  </div>
  <% if (NoMatches) { %><p class="pm-no-results" id="pm-no-results">No matching programs. Try another name.</p><% } %>
</asp:PlaceHolder>

<asp:PlaceHolder ID="Editor" runat="server" Visible="false">
  <a class="pm-back" href="Programs">&larr; Program Management</a>
  <header class="pm-header"><div><h1><%: P?.Name %></h1><p>Manage assigned courses and the order of this learning path.</p></div>
    <div><skill:SubmitButton runat="server" Text="Edit program details" CausesValidation="false" CommandArgument="edit=1" OnCommand="EditorOpen_Command" /><skill:SubmitButton runat="server" CssClass="pm-save" Text="Save path" CausesValidation="false" OnClick="SavePath_Click" aria-label="Save learning path" /></div></header>
  <div class="pm-detail-layout">
    <aside class="pm-detail-summary">
      <div class="pm-cover"><span class="pm-native" aria-hidden="true"><img src="assets/program-management/<%= Cover %>" alt=""></span></div>
      <h2><%: P?.Name %></h2><p><%: P?.Description %></p>
      <dl><div><dt>Program status</dt><dd><%= P?.Published == true ? "Published" : "Draft" %></dd></div><div><dt>Assigned courses</dt><dd><%= Assigned.Count %></dd></div><div><dt>Courses in saved path</dt><dd><%= SavedPath.Count %></dd></div><div><dt>Enrolments</dt><dd><%= Enrolments %></dd></div></dl>
      <a href="CourseEditor?program=<%= P?.Id %>">＋ Create a course in this program</a>
      <skill:SubmitButton runat="server" Text="Add existing course" CausesValidation="false" CommandArgument="add=1" OnCommand="EditorOpen_Command" />
      <skill:SubmitButton runat="server" CssClass="pm-delete" Text="Delete program" CausesValidation="false" CommandArgument="delete=1" OnCommand="EditorOpen_Command" />
    </aside>
    <div class="pm-detail-content">
      <section class="pm-path-section"><h2>Learning path</h2>
        <p class="pm-help">Choose and order the courses learners see in this path. Removing a course here keeps it assigned to this program. Only published courses are visible to learners.</p>
        <div id="pm-path-form">
          <div id="pm-path-rows">
            <asp:Repeater ID="PathRows" runat="server">
              <ItemTemplate>
                <div class="pm-course-row" data-course="<%# Eval("Id") %>"><span class="pm-order"><%# (Container.ItemIndex + 1).ToString("00") %></span><div><h3><%#: Eval("Title") %></h3><p><%#: Eval("Status") %> &middot; <%#: Eval("Instructor") %></p></div>
                  <div class="pm-row-actions"><skill:SubmitButton runat="server" Text="↑" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Up_Command" Enabled='<%# Container.ItemIndex > 0 %>' aria-label='<%# "Move " + Eval("Title") + " up" %>' /><skill:SubmitButton runat="server" Text="↓" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Down_Command" Enabled='<%# Container.ItemIndex < Path.Count - 1 %>' aria-label='<%# "Move " + Eval("Title") + " down" %>' /><skill:SubmitButton runat="server" Text="Remove" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Remove_Command" aria-label='<%# "Remove " + Eval("Title") + " from learning path" %>' /><a href="CourseEditor?id=<%# Eval("Id") %>" aria-label="Edit <%#: Eval("Title") %>">↗</a></div></div>
              </ItemTemplate>
            </asp:Repeater>
            <% if (Path.Count == 0) { %><p class="pm-empty">No courses selected. Add an assigned course below to start this path.</p><% } %>
          </div>
          <p class="pm-draft-state" role="status"><%= DraftState %></p>
          <skill:SubmitButton ID="Discard" runat="server" CssClass="pm-discard" Text="Discard path changes" CausesValidation="false" OnClick="Discard_Click" />
        </div>
      </section>
      <section class="pm-available"><h2>Other courses in this program</h2><p class="pm-help">Add these to the learning path, or move them to another program you manage.</p>
        <div id="pm-available-rows">
          <asp:Repeater ID="AvailableRows" runat="server">
            <ItemTemplate><div class="pm-available-course"><div><strong><%#: Eval("Title") %></strong><small><%#: Eval("Status") %></small></div><skill:SubmitButton runat="server" Text="＋ Add to path" CausesValidation="false" CommandArgument='<%# Eval("Id") %>' OnCommand="Add_Command" aria-label='<%# "Add " + Eval("Title") + " to learning path" %>' /><skill:SubmitButton runat="server" Text="Move program" CausesValidation="false" CommandArgument='<%# "move=" + Eval("Id") %>' OnCommand="EditorOpen_Command" aria-label='<%# "Move " + Eval("Title") + " to another program" %>' /></div></ItemTemplate>
          </asp:Repeater>
          <% if (Available.Count == 0) { %><p class="pm-empty">Every assigned course is included in this path.</p><% } %>
        </div>
      </section>
      <section class="pm-assigned"><header><h2>All assigned courses</h2><span><%= Assigned.Count %></span></header>
        <asp:Repeater ID="AssignedRows" runat="server">
          <ItemTemplate><div><a href="CourseEditor?id=<%# Eval("Id") %>"><%#: Eval("Title") %></a><span><%#: Eval("Status") %></span><skill:SubmitButton runat="server" Text="Move program" CausesValidation="false" CommandArgument='<%# "move=" + Eval("Id") %>' OnCommand="EditorOpen_Command" aria-label='<%# "Move " + Eval("Title") + " to another program" %>' /></div></ItemTemplate>
        </asp:Repeater>
        <% if (Assigned.Count == 0) { %><p class="pm-empty">No assigned courses yet. Create one or add an existing course.</p><% } %>
      </section>
    </div>
  </div>
</asp:PlaceHolder>
</div></section>
</asp:Content>
