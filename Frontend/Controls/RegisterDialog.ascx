<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RegisterDialog.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.RegisterDialog" %>
<%@ Register Src="AuthArt.ascx" TagPrefix="skill" TagName="AuthArt" %>
<%-- Opens over any page when the address ends in #register (a link to "#register"); shown open on the Register page, when a sign-up is refused and after the footer sign-up box. --%>
<div class="auth-popup<%= Shown ? " is-open" : "" %>" id="register">
<div class="auth-screen">
  <a class="auth-backdrop" href="<%= CloseUrl %>" tabindex="-1" aria-hidden="true"></a>
<div id="register-dialog" class="auth-dialog" data-auth="register" role="dialog" aria-modal="true" aria-labelledby="register-title">
  <a class="dialog-close" href="<%= CloseUrl %>" aria-label="Close">&times;</a>
  <div class="auth-v2 auth-modal auth-register">
    <skill:AuthArt runat="server" Kind="register" />
    <section class="auth-form">
      <div class="auth-card">
        <h2 id="register-title">Create your account</h2>
        <p class="auth-modal-sub">Instructor accounts are reviewed before they can publish courses.</p>
        <asp:Panel runat="server" CssClass="auth-fields" DefaultButton="RegisterButton">
          <div class="form-grid">
          <div class="auth-control">
            <div class="auth-label-row"><label for="SignUpName">Full name <b class="req" title="Required" aria-hidden="true">*</b></label></div>
            <span class="auth-field"><asp:TextBox ID="SignUpName" runat="server" ClientIDMode="Static" placeholder="Alex Morgan" autocomplete="name" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="SignUpName" ErrorMessage="Enter your full name." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="SignUpUsername">Username <b class="req" title="Required" aria-hidden="true">*</b></label></div>
            <span class="auth-field"><asp:TextBox ID="SignUpUsername" runat="server" ClientIDMode="Static" placeholder="alexmorgan" autocomplete="username" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="SignUpUsername" ErrorMessage="Choose a username." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
          </div>
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="SignUpEmailBox">Email <b class="req" title="Required" aria-hidden="true">*</b></label></div>
            <span class="auth-field"><asp:TextBox ID="SignUpEmailBox" runat="server" ClientIDMode="Static" TextMode="Email" placeholder="you@example.com" autocomplete="email" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="SignUpEmailBox" ErrorMessage="Enter your email." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
            <asp:RegularExpressionValidator runat="server" ControlToValidate="SignUpEmailBox" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Enter a valid email address." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
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
            <div class="auth-label-row"><label for="SignUpPassword">Password <b class="req" title="Required" aria-hidden="true">*</b></label></div>
            <span class="auth-field"><asp:TextBox ID="SignUpPassword" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="8+ characters" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="SignUpPassword" ErrorMessage="Choose a password." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
            <asp:CustomValidator runat="server" ControlToValidate="SignUpPassword" ClientValidationFunction="skillUpPasswordCheck" OnServerValidate="Password_ServerValidate" ErrorMessage="Use 8 to 128 characters." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="SignUpConfirm">Confirm password <b class="req" title="Required" aria-hidden="true">*</b></label></div>
            <span class="auth-field"><asp:TextBox ID="SignUpConfirm" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="Type it again" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="SignUpConfirm" ErrorMessage="Type the password again." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
            <asp:CompareValidator runat="server" ControlToValidate="SignUpConfirm" ControlToCompare="SignUpPassword" ErrorMessage="Passwords must match." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
          </div>
          </div>
          <div class="pw-help" data-pw-for="SignUpPassword">
            <div class="pw-meter" role="progressbar" aria-label="Password strength" aria-valuemin="0" aria-valuemax="4" aria-valuenow="0"><i></i><i></i><i></i><i></i></div>
            <p class="pw-label" aria-live="polite">Password strength: <b>not entered yet</b></p>
            <ul class="pw-rules">
              <li data-rule="length">8 to 128 characters</li>
              <li data-rule="capital">A capital letter (A&ndash;Z)</li>
              <li data-rule="number">A number (0&ndash;9)</li>
              <li data-rule="special">A special character (! @ # $ %)</li>
            </ul>
          </div>
          <div class="auth-control auth-terms">
            <label class="auth-check"><asp:CheckBox ID="AgreeTerms" runat="server" ClientIDMode="Static" /><span>I agree to the <a href="Terms" target="_blank" rel="noopener">Terms of use</a> and the <a href="Privacy" target="_blank" rel="noopener">Privacy information</a> <b class="req" title="Required" aria-hidden="true">*</b></span></label>
            <asp:CustomValidator runat="server" ClientValidationFunction="skillUpAgreeCheck" OnServerValidate="Agree_ServerValidate" ErrorMessage="Tick the box to accept the terms and the privacy information." Display="Dynamic" CssClass="field-error" ValidationGroup="register" />
          </div>
        <p class="form-error" role="alert"><asp:Literal ID="FormError" runat="server" /></p>
          <div class="actions"><skill:SubmitButton ID="RegisterButton" runat="server" CssClass="primary" Text="Create account" OnClick="Register_Click" ValidationGroup="register" /></div>
        </asp:Panel>
        <p class="auth-switch">Already have an account? <a class="link" href="<%= LoginUrl %>">Log in</a></p>
      </div>
    </section>
  </div>
</div>
</div>
</div>
