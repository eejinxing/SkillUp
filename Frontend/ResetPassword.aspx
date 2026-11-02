<%@ Page Title="Choose a new password" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="ResetPassword.aspx.cs" Inherits="SKILL_UP.Frontend.ResetPasswordPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<div class="auth-screen">
<div id="dialog" class="auth-dialog" data-auth="reset" role="dialog" aria-modal="true" aria-labelledby="dialog-title">
  <a class="dialog-close" href="Home" aria-label="Close">&times;</a>
  <div class="auth-v2 auth-modal auth-reset">
    <skill:AuthArt runat="server" Kind="reset" />
    <section class="auth-form">
      <div class="auth-card">
        <h2 id="dialog-title">Choose a new password</h2>
        <p class="auth-modal-sub">Use 8 or more characters with a capital letter, a number and a special character. You&rsquo;ll log in with it next time.</p>
        <asp:Panel runat="server" DefaultButton="SaveButton">
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthPassword">New password <b class="req" title="Required" aria-hidden="true">*</b></label></div>
            <span class="auth-field"><asp:TextBox ID="AuthPassword" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="8+ characters" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthPassword" ErrorMessage="Choose a password." Display="Dynamic" CssClass="field-error" />
            <asp:CustomValidator runat="server" ControlToValidate="AuthPassword" ClientValidationFunction="skillUpPasswordCheck" OnServerValidate="Password_ServerValidate" ErrorMessage="Use 8 to 128 characters." Display="Dynamic" CssClass="field-error" />
          </div>
          <div class="pw-help" data-pw-for="AuthPassword">
            <div class="pw-meter" role="progressbar" aria-label="Password strength" aria-valuemin="0" aria-valuemax="4" aria-valuenow="0"><i></i><i></i><i></i><i></i></div>
            <p class="pw-label" aria-live="polite">Password strength: <b>not entered yet</b></p>
            <ul class="pw-rules">
              <li data-rule="length">8 to 128 characters</li>
              <li data-rule="capital">A capital letter (A&ndash;Z)</li>
              <li data-rule="number">A number (0&ndash;9)</li>
              <li data-rule="special">A special character (! @ # $ %)</li>
              <li data-rule="different">Different from your current password (checked when you save)</li>
            </ul>
            <p class="pw-tip">Tip: a short phrase such as <em>blue-tiger-42</em> is easy to remember. Longer passwords (12+ and 16+ characters) rate stronger.</p>
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthConfirm">Confirm new password <b class="req" title="Required" aria-hidden="true">*</b></label></div>
            <span class="auth-field"><asp:TextBox ID="AuthConfirm" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="Type it again" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthConfirm" ErrorMessage="Type the password again." Display="Dynamic" CssClass="field-error" />
            <asp:CompareValidator runat="server" ControlToValidate="AuthConfirm" ControlToCompare="AuthPassword" ErrorMessage="Passwords must match." Display="Dynamic" CssClass="field-error" />
          </div>
        <p class="form-error" role="alert"><asp:Literal ID="FormError" runat="server" /></p>
          <div class="actions"><skill:SubmitButton ID="SaveButton" runat="server" CssClass="primary" Text="Save new password" OnClick="Save_Click" /></div>
        </asp:Panel>
        <p class="auth-switch">Remembered your password? <a class="link" href="#login">Log in</a></p>
      </div>
    </section>
  </div>
</div>
</div>
</asp:Content>
