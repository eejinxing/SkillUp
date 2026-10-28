<%@ Page Title="Create your account" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="SKILL_UP.Frontend.RegisterPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<div class="auth-screen">
<div id="dialog" class="auth-dialog" data-auth="register" role="dialog" aria-modal="true" aria-labelledby="dialog-title">
  <a class="dialog-close" href="Home" aria-label="Close">&times;</a>
  <div class="auth-v2 auth-modal auth-register">
    <skill:AuthArt runat="server" Kind="register" />
    <section class="auth-form">
      <div class="auth-card">
        <h2 id="dialog-title">Create your account</h2>
        <p class="auth-modal-sub">Instructor accounts are reviewed before they can publish courses.</p>
        <asp:Panel runat="server" DefaultButton="RegisterButton">
          <div class="form-grid">
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthName">Full name</label></div>
            <span class="auth-field"><asp:TextBox ID="AuthName" runat="server" ClientIDMode="Static" placeholder="Alex Morgan" autocomplete="name" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthName" ErrorMessage="Enter your full name." Display="Dynamic" CssClass="field-error" />
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthUsername">Username</label></div>
            <span class="auth-field"><asp:TextBox ID="AuthUsername" runat="server" ClientIDMode="Static" placeholder="alexmorgan" autocomplete="username" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthUsername" ErrorMessage="Choose a username." Display="Dynamic" CssClass="field-error" />
          </div>
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthEmail">Email</label></div>
            <span class="auth-field"><asp:TextBox ID="AuthEmail" runat="server" ClientIDMode="Static" TextMode="Email" placeholder="you@example.com" autocomplete="email" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthEmail" ErrorMessage="Enter your email." Display="Dynamic" CssClass="field-error" />
            <asp:RegularExpressionValidator runat="server" ControlToValidate="AuthEmail" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Enter a valid email address." Display="Dynamic" CssClass="field-error" />
          </div>
          <fieldset class="auth-role">
            <legend>I&rsquo;m joining to</legend>
            <div class="auth-role-options">
              <label><asp:RadioButton ID="RoleLearner" runat="server" GroupName="role" Checked="true" /><span><strong>Learn</strong><small>Take courses and track progress</small></span></label>
              <label><asp:RadioButton ID="RoleInstructor" runat="server" GroupName="role" /><span><strong>Teach</strong><small>Publish courses after approval</small></span></label>
            </div>
          </fieldset>
          <div class="form-grid">
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthPassword">Password</label></div>
            <span class="auth-field"><asp:TextBox ID="AuthPassword" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="8+ characters" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthPassword" ErrorMessage="Choose a password." Display="Dynamic" CssClass="field-error" />
            <asp:RegularExpressionValidator runat="server" ControlToValidate="AuthPassword" ValidationExpression="^[\s\S]{8,}$" ErrorMessage="Use at least 8 characters." Display="Dynamic" CssClass="field-error" />
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthConfirm">Confirm password</label></div>
            <span class="auth-field"><asp:TextBox ID="AuthConfirm" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="Type it again" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthConfirm" ErrorMessage="Type the password again." Display="Dynamic" CssClass="field-error" />
            <asp:CompareValidator runat="server" ControlToValidate="AuthConfirm" ControlToCompare="AuthPassword" ErrorMessage="Passwords must match." Display="Dynamic" CssClass="field-error" />
          </div>
          </div>
        <p class="form-error" role="alert"><asp:Literal ID="FormError" runat="server" /></p>
          <div class="actions"><skill:SubmitButton ID="RegisterButton" runat="server" CssClass="primary" Text="Create account" OnClick="Register_Click" /></div>
        </asp:Panel>
        <p class="auth-switch">Already have an account? <a class="link" href="Login">Log in</a></p>
      </div>
    </section>
  </div>
</div>
</div>
</asp:Content>
