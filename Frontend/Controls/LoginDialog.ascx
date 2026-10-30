<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LoginDialog.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.LoginDialog" %>
<%@ Register Src="AuthArt.ascx" TagPrefix="skill" TagName="AuthArt" %>
<%-- Opens over any page when the address ends in #login (a link to "#login"); shown open on the Login page and when a sign-in is refused. --%>
<div class="auth-popup<%= Shown ? " is-open" : "" %>" id="login">
<div class="auth-screen">
  <a class="auth-backdrop" href="<%= CloseUrl %>" tabindex="-1" aria-hidden="true"></a>
<div id="login-dialog" class="auth-dialog" data-auth="login" role="dialog" aria-modal="true" aria-labelledby="login-title">
  <a class="dialog-close" href="<%= CloseUrl %>" aria-label="Close">&times;</a>
  <div class="auth-v2 auth-modal auth-login">
    <skill:AuthArt runat="server" Kind="login" />
    <section class="auth-form">
      <div class="auth-card">
        <h2 id="login-title">Log in</h2>
        <p class="auth-modal-sub">Use your email or username to open your workspace.</p>
        <asp:Panel runat="server" CssClass="auth-fields" DefaultButton="LogInButton">
          <div class="auth-control">
            <div class="auth-label-row"><label for="LoginUser">Email or username</label></div>
            <span class="auth-field"><asp:TextBox ID="LoginUser" runat="server" ClientIDMode="Static" placeholder="you@example.com" autocomplete="username" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="LoginUser" ErrorMessage="Enter your email or username." Display="Dynamic" CssClass="field-error" ValidationGroup="login" />
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="LoginPassword">Password</label><a class="auth-inline-link" href="ForgotPassword">Forgot password?</a></div>
            <span class="auth-field"><asp:TextBox ID="LoginPassword" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="Your password" autocomplete="current-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="LoginPassword" ErrorMessage="Enter your password." Display="Dynamic" CssClass="field-error" ValidationGroup="login" />
          </div>
        <p class="form-error" role="alert"><asp:Literal ID="FormError" runat="server" /></p>
          <div class="actions"><skill:SubmitButton ID="LogInButton" runat="server" CssClass="primary" Text="Log in" OnClick="LogIn_Click" ValidationGroup="login" /></div>
        </asp:Panel>
        <p class="auth-switch">New to Skill UP? <a class="link" href="<%= RegisterUrl %>">Create an account</a></p>
        <asp:PlaceHolder ID="DemoPanel" runat="server">
        <section class="auth-demo" aria-labelledby="auth-demo-title">
          <h2 id="auth-demo-title">Explore with a demo account</h2>
          <div class="demo-grid">
            <skill:SubmitButton runat="server" CssClass="auth-social" Text="Learner" CommandArgument="Learner" OnCommand="Demo_Command" CausesValidation="false" />
            <skill:SubmitButton runat="server" CssClass="auth-social" Text="Instructor" CommandArgument="Instructor" OnCommand="Demo_Command" CausesValidation="false" />
          </div>
          <p class="auth-notice">Demo usernames: alex, john. Password: <span class="auth-code">Demo1234!</span></p>
        </section>
        </asp:PlaceHolder>
      </div>
    </section>
  </div>
</div>
</div>
</div>
