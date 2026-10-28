<%@ Page Title="Account settings" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="SKILL_UP.Frontend.SettingsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
  <div class="page-head"><div><h1 tabindex="-1" id="page-title">Account settings</h1><p>Make this workspace yours.</p></div><div class="actions tight"></div></div>
  <div class="settings-layout">
    <nav class="panel settings-menu" aria-label="Account settings">
      <a class="button" href="Profile">Edit profile</a>
      <a class="<%= Tab == "security" ? "active" : "" %>" href="Settings?tab=security">Account security</a>
      <a class="<%= Tab == "address" ? "active" : "" %>" href="Settings?tab=address">Saved address</a>
      <a class="<%= Tab == "billing" ? "active" : "" %>" href="Settings?tab=billing">Learning access</a>
      <skill:SubmitButton runat="server" CssClass="ghost" Text="Sign out" CausesValidation="false" OnClick="SignOut_Click" />
    </nav>
    <section class="panel">
      <asp:MultiView ID="Tabs" runat="server">
        <asp:View runat="server">
          <h2>Account security</h2>
          <div class="list-item">
            <strong>Password</strong><p>Use a unique password for your account.</p>
            <details class="inline-form"<%= PasswordOpen ? " open" : "" %>>
              <summary class="button ghost">Change password</summary>
              <asp:Panel runat="server" DefaultButton="PasswordButton">
                <label>Current password <asp:TextBox ID="CurrentPassword" runat="server" TextMode="Password" autocomplete="current-password" />
                  <asp:RequiredFieldValidator runat="server" ControlToValidate="CurrentPassword" ValidationGroup="password" ErrorMessage="Enter your current password." Display="Dynamic" CssClass="field-error" /></label>
                <label>New password <asp:TextBox ID="NewPassword" runat="server" TextMode="Password" autocomplete="new-password" />
                  <asp:RegularExpressionValidator runat="server" ControlToValidate="NewPassword" ValidationGroup="password" ValidationExpression="^[\s\S]{8,}$" ErrorMessage="Use at least 8 characters." Display="Dynamic" CssClass="field-error" />
                  <asp:RequiredFieldValidator runat="server" ControlToValidate="NewPassword" ValidationGroup="password" ErrorMessage="Choose a new password." Display="Dynamic" CssClass="field-error" /></label>
                <label>Confirm new password <asp:TextBox ID="ConfirmPassword" runat="server" TextMode="Password" autocomplete="new-password" />
                  <asp:CompareValidator runat="server" ControlToValidate="ConfirmPassword" ControlToCompare="NewPassword" ValidationGroup="password" ErrorMessage="Passwords must match." Display="Dynamic" CssClass="field-error" /></label>
                <p class="form-error" role="alert"><asp:Literal ID="PasswordError" runat="server" /></p>
                <div class="actions"><skill:SubmitButton ID="PasswordButton" runat="server" CssClass="primary" Text="Update password" ValidationGroup="password" OnClick="Password_Click" /></div>
              </asp:Panel>
            </details>
          </div>
          <div class="list-item"><strong>Current account</strong><p><%: Me.Email %> &middot; <%: Me.Role %></p><%= Ui.Tag(Me.Status) %></div>
        </asp:View>
        <asp:View runat="server">
          <h2>Saved address</h2>
          <asp:Panel runat="server" DefaultButton="AddressButton">
            <label>Address <asp:TextBox ID="Address" runat="server" MaxLength="300" /></label>
            <div class="form-grid">
              <label>City <asp:TextBox ID="City" runat="server" MaxLength="300" /></label>
              <label>Country <asp:TextBox ID="Country" runat="server" MaxLength="300" /></label>
              <label>Postcode <asp:TextBox ID="Postcode" runat="server" MaxLength="300" /></label>
            </div>
            <div class="actions"><skill:SubmitButton ID="AddressButton" runat="server" CssClass="primary" Text="Save address" CausesValidation="false" OnClick="Address_Click" /></div>
          </asp:Panel>
        </asp:View>
        <asp:View runat="server">
          <h2>Learning access</h2>
          <p>Your courses in this prototype are free. No payment method is required.</p>
          <div class="metrics">
            <div class="metric"><small>Enrolled courses</small><strong class="number"><%= MyEnrollments.Count %></strong><small>Current enrolments</small><span class="spark" aria-hidden="true">&#9602;&#9603;&#9605;&#9606;</span></div>
            <div class="metric"><small>Amount due</small><strong class="number">$0.00</strong><small>Free learning access</small><span class="spark" aria-hidden="true">&#9602;&#9603;&#9605;&#9606;</span></div>
          </div>
          <a class="button primary" href="Catalog">Explore courses</a>
        </asp:View>
      </asp:MultiView>
    </section>
  </div>
</asp:Content>
