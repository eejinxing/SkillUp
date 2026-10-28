<%@ Page Title="User accounts" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="SKILL_UP.Frontend.UsersPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="admin-users-page au-market" data-view="<%= View %>" aria-labelledby="au-title">
  <%= Network %>
  <header class="au-header"><div><a class="au-back" href="Dashboard"><%= Svg("back") %>Overview</a><h1 id="au-title" tabindex="-1">User accounts</h1><p><%= Data.Users.Count %> users in your workspace</p></div><a class="au-add" href="UserEditor?id=new"><span class="au-add-icon"><img src="assets/admin-users/imgFamiconsAddCircleOutline.svg" alt=""></span>Add User</a></header>
  <asp:Panel ID="ActionsPanel" runat="server" CssClass="panel au-actions-dialog" Visible="false">
    <h2><%: Target?.Name %></h2>
    <p><%: Target?.Email %><br><%: Target?.Role %> &middot; <%: Target?.Status %></p>
    <div class="actions"><a class="button primary" href="UserEditor?id=<%= Target?.Id %>">View account</a><a class="button ghost" href="UserEditor?id=<%= Target?.Id %>">Edit account</a><skill:SubmitButton ID="ApproveButton" runat="server" CssClass="ghost" Text="Approve" CausesValidation="false" OnClick="Approve_Click" /><skill:SubmitButton ID="DeleteButton" runat="server" CssClass="danger ghost" Text="Delete account" CausesValidation="false" OnClick="Delete_Click" data-confirm="Delete this user account? This removes the account and its enrolments. Assigned staff must be reassigned first." /><a class="button ghost" href="<%: ListUrl() %>">Close</a></div>
  </asp:Panel>
  <asp:Panel runat="server" CssClass="au-toolbar" DefaultButton="SearchButton">
    <label class="au-search-wrap"><%= Svg("search") %><span class="sr-only">Search users</span><asp:TextBox ID="SearchBox" runat="server" TextMode="Search" aria-label="Search users" placeholder="Search by name, email or username" /></label>
    <label class="au-sort"><%= Svg("sort") %><span class="sr-only">Sort users</span><asp:DropDownList ID="SortList" runat="server" aria-label="Sort users" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed"><asp:ListItem Value="name">Name A–Z</asp:ListItem><asp:ListItem Value="name-desc">Name Z–A</asp:ListItem><asp:ListItem Value="role">Role</asp:ListItem><asp:ListItem Value="status">Status</asp:ListItem></asp:DropDownList></label>
    <div class="au-view-toggle" role="group" aria-label="Layout"><skill:SubmitButton ID="GridButton" runat="server" CausesValidation="false" CommandArgument="grid" OnCommand="View_Command" aria-label="Grid view"><%= Svg("grid") %></skill:SubmitButton><skill:SubmitButton ID="ListButton" runat="server" CausesValidation="false" CommandArgument="list" OnCommand="View_Command" aria-label="List view"><%= Svg("list") %></skill:SubmitButton></div>
    <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Filter_Changed" />
  </asp:Panel>
  <div class="au-body">
    <aside class="au-sidebar" aria-label="Filters">
      <details class="au-facet" open><summary>Role<%= Svg("chevron") %></summary>
        <label class="au-select"><span class="sr-only">Filter role</span><asp:DropDownList ID="RoleList" runat="server" aria-label="Filter role" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed"><asp:ListItem Value="">All roles</asp:ListItem><asp:ListItem>Learner</asp:ListItem><asp:ListItem>Instructor</asp:ListItem><asp:ListItem>Program Leader</asp:ListItem><asp:ListItem>Admin</asp:ListItem></asp:DropDownList><%= Svg("chevron") %></label>
        <div class="au-facet-list">
          <asp:Repeater ID="RoleFacets" runat="server"><ItemTemplate><skill:SubmitButton runat="server" CssClass="au-facet-row" CausesValidation="false" CommandArgument='<%# Eval("Role") %>' OnCommand="RoleFacet_Command" aria-pressed='<%# Eval("Pressed") %>' data-role='<%# Eval("Key") %>'><span class="au-facet-icon"><%# Eval("Icon") %></span><span><%# Eval("Role") %></span><em><%# Eval("Count") %></em><i><%# Svg("check") %></i></skill:SubmitButton></ItemTemplate></asp:Repeater>
        </div>
      </details>
      <details class="au-facet" open><summary>Account status<%= Svg("chevron") %></summary>
        <label class="au-select"><span class="sr-only">Filter account status</span><asp:DropDownList ID="StatusList" runat="server" aria-label="Filter account status" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed"><asp:ListItem Value="">All statuses</asp:ListItem><asp:ListItem>Active</asp:ListItem><asp:ListItem>Pending</asp:ListItem><asp:ListItem>Inactive</asp:ListItem></asp:DropDownList><%= Svg("chevron") %></label>
        <div class="au-chip-row">
          <asp:Repeater ID="StatusFacets" runat="server"><ItemTemplate><skill:SubmitButton runat="server" CssClass="au-chip" CausesValidation="false" CommandArgument='<%# Eval("Status") %>' OnCommand="StatusFacet_Command" aria-pressed='<%# Eval("Pressed") %>'><span class="au-dot au-dot-<%# Eval("Key") %>" aria-hidden="true"></span><%# Eval("Status") %><em><%# Eval("Count") %></em></skill:SubmitButton></ItemTemplate></asp:Repeater>
        </div>
      </details>
      <details class="au-facet" open><summary>Workspace mix<%= Svg("chevron") %></summary><%= Mix %></details>
      <skill:SubmitButton ID="ClearButton" runat="server" CssClass="au-clear" Text="Clear filters" CausesValidation="false" OnClick="Clear_Click" />
    </aside>
    <div class="au-results">
      <div class="au-directory" id="au-directory"><%= Cards %></div>
      <asp:PlaceHolder ID="Empty" runat="server"><div class="au-empty" id="au-empty"><h2>No matching users</h2><p>Try a different name, email, username or filter.</p><skill:SubmitButton runat="server" Text="Clear filters" CausesValidation="false" OnClick="Clear_Click" /></div></asp:PlaceHolder>
      <footer class="au-footer"><p id="au-count" role="status">Showing <%= Shown %> of <%= Total %> users</p><skill:SubmitButton ID="MoreButton" runat="server" CssClass="au-more" Text="Load More" CausesValidation="false" OnClick="More_Click" /></footer>
    </div>
  </div>
</section>
</asp:Content>
