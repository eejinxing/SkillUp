<%@ Page Title="User account" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="UserEditor.aspx.cs" Inherits="SKILL_UP.Frontend.UserEditorPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">

<asp:PlaceHolder ID="Missing" runat="server" Visible="false"><section class="user-editor-page ue-missing"><h1>User not found</h1><p>This account may have been removed.</p><a href="Users">&larr; User accounts</a></section></asp:PlaceHolder>

<%-- A new account: information, role, status and a temporary password. --%>
<asp:PlaceHolder ID="CreateForm" runat="server" Visible="false">
<section class="user-editor-page ue-account-form ue-create" data-account="new">
  <a class="ue-back" href="Users">&larr; User accounts</a>
  <header class="ue-header"><div><h1>Create user account</h1><p>Add their information, then choose a role and account status.</p></div></header>
  <div id="user-editor-form">
    <details class="ue-create-panel" open><summary><h2>Information</h2><span><%= Icon("chevron") %></span></summary>
      <div class="ue-create-rows">
        <label class="ue-create-row" data-ue-field="name"><span class="ue-create-label"><%= Icon("name") %><span>Full name<b aria-hidden="true"> *</b></span></span><span class="ue-create-value"><asp:TextBox ID="NewName" runat="server" placeholder="Enter full name" autocomplete="off" MaxLength="100" /><asp:RequiredFieldValidator runat="server" ControlToValidate="NewName" ValidationGroup="create" ErrorMessage="Enter the full name." Display="Dynamic" CssClass="field-error" /></span><span class="ue-create-pencil"><%= Icon("edit") %></span></label>
        <label class="ue-create-row" data-ue-field="username"><span class="ue-create-label"><%= Icon("username") %><span>Username<b aria-hidden="true"> *</b></span></span><span class="ue-create-value"><asp:TextBox ID="NewUsername" runat="server" placeholder="Choose a username" autocomplete="off" spellcheck="false" MaxLength="50" /><asp:RequiredFieldValidator runat="server" ControlToValidate="NewUsername" ValidationGroup="create" ErrorMessage="Choose a username." Display="Dynamic" CssClass="field-error" /></span><span class="ue-create-pencil"><%= Icon("edit") %></span></label>
        <label class="ue-create-row" data-ue-field="email"><span class="ue-create-label"><%= Icon("email") %><span>Email<b aria-hidden="true"> *</b></span></span><span class="ue-create-value"><asp:TextBox ID="NewEmail" runat="server" TextMode="Email" placeholder="name@example.com" autocomplete="off" spellcheck="false" MaxLength="150" /><asp:RequiredFieldValidator runat="server" ControlToValidate="NewEmail" ValidationGroup="create" ErrorMessage="Enter an email address." Display="Dynamic" CssClass="field-error" /></span><span class="ue-create-pencil"><%= Icon("edit") %></span></label>
        <label class="ue-create-row" data-ue-field="bio"><span class="ue-create-label"><%= Icon("bio") %><span>About the user</span></span><span class="ue-create-value"><asp:TextBox ID="NewBio" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" placeholder="Profile or application details (optional)" /></span><span class="ue-create-pencil"><%= Icon("edit") %></span></label>
      </div>
    </details>
    <details class="ue-create-panel" open><summary><h2>Account &amp; access</h2><span><%= Icon("chevron") %></span></summary>
      <div class="ue-create-rows">
        <label class="ue-create-row" data-ue-field="role"><span class="ue-create-label"><%= Icon("role") %><span>Role<b aria-hidden="true"> *</b></span></span><span class="ue-create-value"><asp:DropDownList ID="NewRole" runat="server"><asp:ListItem>Learner</asp:ListItem><asp:ListItem>Instructor</asp:ListItem><asp:ListItem>Program Leader</asp:ListItem><asp:ListItem>Admin</asp:ListItem></asp:DropDownList></span><span class="ue-create-pencil"><%= Icon("edit") %></span></label>
        <label class="ue-create-row" data-ue-field="status"><span class="ue-create-label"><%= Icon("status") %><span>Account status<b aria-hidden="true"> *</b></span></span><span class="ue-create-value"><asp:DropDownList ID="NewStatus" runat="server"><asp:ListItem>Active</asp:ListItem><asp:ListItem>Inactive</asp:ListItem><asp:ListItem>Pending</asp:ListItem></asp:DropDownList></span><span class="ue-create-pencil"><%= Icon("edit") %></span></label>
        <label class="ue-create-row" data-ue-field="password"><span class="ue-create-label"><%= Icon("password") %><span>Temporary password<b aria-hidden="true"> *</b></span></span><span class="ue-create-value"><asp:TextBox ID="NewPassword" runat="server" TextMode="Password" placeholder="8+ characters, a capital, a number and a symbol" autocomplete="new-password" aria-describedby="ue-password-help" /><asp:RequiredFieldValidator runat="server" ControlToValidate="NewPassword" ValidationGroup="create" ErrorMessage="Set a temporary password." Display="Dynamic" CssClass="field-error" /><asp:RegularExpressionValidator runat="server" ControlToValidate="NewPassword" ValidationGroup="create" ValidationExpression="^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9\s]).{8,128}$" ErrorMessage="Use 8 or more characters with a capital letter, a number and a special character." Display="Dynamic" CssClass="field-error" /></span><span class="ue-create-pencil"><%= Icon("edit") %></span></label>
        <div class="ue-create-row ue-create-readonly"><span class="ue-create-label"><%= Icon("id") %><span>User ID</span></span><span class="ue-create-value">Assigned when the account is created</span></div>
      </div>
      <p id="ue-password-help" class="ue-create-help">Share the temporary password with the user.</p>
    </details>
    <div class="ue-create-bottom"><div><p class="ue-draft-state" role="status">Required fields are marked with *.</p></div>
      <div class="ue-create-actions"><skill:SubmitButton runat="server" CssClass="ue-create-discard" Text="Discard changes" CausesValidation="false" OnClick="Discard_Click" /><skill:SubmitButton ID="CreateButton" runat="server" CssClass="ue-save" Text="Create account" ValidationGroup="create" OnClick="Create_Click" aria-label="Create account" /></div></div>
  </div>
</section>
</asp:PlaceHolder>

<%-- An existing account: information (?tab=), activity logs (?tab=audit) and learning activity (?tab=learning). --%>
<asp:PlaceHolder ID="Manage" runat="server" Visible="false">
<section class="user-editor-page ue-course-editor ue-edit" data-account="<%= U?.Id %>">
  <a class="ue-back" href="Users">&larr; User accounts</a>
  <header class="ue-header"><span class="uem-title-icon"><%= Icon("id") %></span><h1><%: U?.Name %></h1><skill:SubmitButton ID="HeaderSave" runat="server" CssClass="ue-save" ValidationGroup="user" OnClick="Save_Click" aria-label="Save account"><span aria-hidden="true">+</span> Save</skill:SubmitButton></header>
  <div class="uem-layout<%= Tab == "audit" ? " uem-layout-audit" : "" %>">
    <% if (Tab != "audit") { %><aside class="uem-summary" aria-label="Account summary"><%= Summary %></aside><% } %>
    <div class="uem-content">
      <nav class="ue-tabs uem-tabs" aria-label="User detail tabs"><%= Tabs %></nav>
      <asp:PlaceHolder ID="InformationForm" runat="server"><div id="user-editor-form">
        <fieldset class="uem-fields"><legend class="sr-only">Account information</legend>
          <div class="uem-field-grid">
            <label class="uem-field"><span>Full name<b aria-hidden="true"> *</b></span><asp:TextBox ID="EditName" runat="server" autocomplete="off" MaxLength="100" /><asp:RequiredFieldValidator runat="server" ControlToValidate="EditName" ValidationGroup="user" ErrorMessage="Enter the full name." Display="Dynamic" CssClass="field-error" /></label>
            <label class="uem-field"><span>Role<b aria-hidden="true"> *</b></span><asp:DropDownList ID="EditRole" runat="server"><asp:ListItem>Learner</asp:ListItem><asp:ListItem>Instructor</asp:ListItem><asp:ListItem>Program Leader</asp:ListItem><asp:ListItem>Admin</asp:ListItem></asp:DropDownList></label>
            <label class="uem-field"><span>Account status<b aria-hidden="true"> *</b></span><asp:DropDownList ID="EditStatus" runat="server"><asp:ListItem>Active</asp:ListItem><asp:ListItem>Inactive</asp:ListItem><asp:ListItem>Pending</asp:ListItem></asp:DropDownList></label>
            <label class="uem-field"><span>Email address<b aria-hidden="true"> *</b></span><asp:TextBox ID="EditEmail" runat="server" TextMode="Email" autocomplete="off" spellcheck="false" MaxLength="150" /><asp:RequiredFieldValidator runat="server" ControlToValidate="EditEmail" ValidationGroup="user" ErrorMessage="Enter an email address." Display="Dynamic" CssClass="field-error" /></label>
            <label class="uem-field"><span>Username<b aria-hidden="true"> *</b></span><asp:TextBox ID="EditUsername" runat="server" autocomplete="off" spellcheck="false" MaxLength="50" /><asp:RequiredFieldValidator runat="server" ControlToValidate="EditUsername" ValidationGroup="user" ErrorMessage="Choose a username." Display="Dynamic" CssClass="field-error" /></label>
            <label class="uem-field uem-readonly"><span>User ID</span><input aria-label="User ID" value="<%= U?.Id %>" readonly aria-readonly="true"></label>
          </div>
          <label class="uem-field uem-bio"><span>About the user</span><asp:TextBox ID="EditBio" runat="server" TextMode="MultiLine" Rows="4" MaxLength="1000" placeholder="Profile or application details (optional)" /></label>
          <div class="uem-field-grid uem-dates"><label class="uem-field uem-readonly"><span>Joined</span><input aria-label="Joined" value="<%= Joined %>" readonly aria-readonly="true"></label><label class="uem-field uem-readonly"><span>Last active</span><input aria-label="Last active" value="<%= LastActive %>" readonly aria-readonly="true"></label></div>
        </fieldset>
        <p class="ue-draft-state" role="status">Save to apply account information and access changes.</p>
        <div class="uem-form-actions"><skill:SubmitButton ID="SaveButton" runat="server" CssClass="uem-save-changes" Text="Save changes" ValidationGroup="user" OnClick="Save_Click" /><skill:SubmitButton runat="server" Text="Discard changes" CausesValidation="false" OnClick="Discard_Click" /></div>
      </div></asp:PlaceHolder>
      <asp:PlaceHolder ID="InformationExtras" runat="server">
        <%-- Courses and programs this account owns. They move to another active account of the same role before the role changes,
             the account is deactivated or it is deleted (Backend: staffTransfer). --%>
        <asp:PlaceHolder ID="Responsibilities" runat="server" Visible="false">
        <section class="uem-transfer" aria-labelledby="uem-transfer-title">
          <header><h2 id="uem-transfer-title">Responsibilities</h2><p>Only the assigned instructor or Program Leader can manage these. Transfer them before changing this account&rsquo;s role, deactivating it or deleting it.</p></header>
          <asp:PlaceHolder ID="CourseTransfer" runat="server" Visible="false">
          <div class="uem-transfer-row">
            <div class="uem-transfer-what"><strong>Teaches <%= Ui.Plural(TeachCount, "course") %></strong><p><%= TeachList %></p></div>
            <div class="uem-transfer-action">
              <asp:DropDownList ID="CourseTarget" runat="server" DataTextField="Name" DataValueField="Id" aria-label="Transfer courses to" />
              <skill:SubmitButton ID="CourseTransferButton" runat="server" Text="Transfer courses" CausesValidation="false" OnClick="TransferCourses_Click" data-confirm="Make the selected instructor the instructor of all these courses?" />
              <%= CourseNoTarget %>
            </div>
          </div>
          </asp:PlaceHolder>
          <asp:PlaceHolder ID="ProgramTransfer" runat="server" Visible="false">
          <div class="uem-transfer-row">
            <div class="uem-transfer-what"><strong>Leads <%= Ui.Plural(LeadCount, "program") %></strong><p><%= LeadList %></p></div>
            <div class="uem-transfer-action">
              <asp:DropDownList ID="ProgramTarget" runat="server" DataTextField="Name" DataValueField="Id" aria-label="Transfer programs to" />
              <skill:SubmitButton ID="ProgramTransferButton" runat="server" Text="Transfer programs" CausesValidation="false" OnClick="TransferPrograms_Click" data-confirm="Make the selected Program Leader the leader of all these programs? They will also review the programs' course requests." />
              <%= ProgramNoTarget %>
            </div>
          </div>
          </asp:PlaceHolder>
        </section>
        </asp:PlaceHolder>
        <section class="uem-access-panel" aria-label="Account access"><div><small>ACCOUNT ACCESS</small><p><%= AccessText %></p></div><skill:SubmitButton ID="StatusButton" runat="server" CausesValidation="false" OnClick="Status_Click" /></section>
        <section class="uem-recent"><header><h2>Recent account activity</h2><a href="UserEditor?id=<%= U?.Id %>&amp;tab=audit">View all &#8599;</a></header><%= Recent %></section>
        <section class="uem-danger"><div><h2>Account actions</h2><p><%= Self ? "You cannot deactivate or delete your own administrator account." : "Permanently remove this user account from the workspace." %></p></div><skill:SubmitButton ID="DeleteButton" runat="server" CssClass="ue-delete" Text="Delete account" CausesValidation="false" OnClick="Delete_Click" data-confirm="Delete this user account? This removes the account and its enrolments. Assigned staff must be reassigned first." /></section>
      </asp:PlaceHolder>
      <%= TabPanel %>
    </div>
  </div>
</section>
</asp:PlaceHolder>
</asp:Content>
