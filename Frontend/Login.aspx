<%@ Page Title="Log in" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SKILL_UP.Frontend.LoginPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<div class="auth-screen">
<div id="dialog" class="auth-dialog" data-auth="login" role="dialog" aria-modal="true" aria-labelledby="dialog-title">
  <a class="dialog-close" href="Home" aria-label="Close">&times;</a>
  <div class="auth-v2 auth-modal auth-login">
    <skill:AuthArt runat="server" Kind="login" />
    <section class="auth-form">
      <div class="auth-card">
        <h2 id="dialog-title">Log in</h2>
        <p class="auth-modal-sub">Use your email or username to open your workspace.</p>
        <asp:Panel runat="server" DefaultButton="LogInButton">
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthEmail">Email or username</label></div>
            <span class="auth-field"><asp:TextBox ID="AuthEmail" runat="server" ClientIDMode="Static" placeholder="you@example.com" autocomplete="username" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthEmail" ErrorMessage="Enter your email or username." Display="Dynamic" CssClass="field-error" />
          </div>
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthPassword">Password</label><a class="auth-inline-link" href="ForgotPassword">Forgot password?</a></div>
            <span class="auth-field"><asp:TextBox ID="AuthPassword" runat="server" ClientIDMode="Static" TextMode="Password" placeholder="Your password" autocomplete="current-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthPassword" ErrorMessage="Enter your password." Display="Dynamic" CssClass="field-error" />
          </div>
        <p class="form-error" role="alert"><asp:Literal ID="FormError" runat="server" /></p>
          <div class="actions"><skill:SubmitButton ID="LogInButton" runat="server" CssClass="primary" Text="Log in" OnClick="LogIn_Click" /></div>
        </asp:Panel>
        <p class="auth-switch">New to Skill UP? <a class="link" href="Register">Create an account</a></p>
        <asp:PlaceHolder ID="DemoPanel" runat="server">
        <section class="auth-demo" aria-labelledby="auth-demo-title">
          <h2 id="auth-demo-title">Explore with a demo account</h2>
          <div class="demo-grid">
            <skill:SubmitButton runat="server" CssClass="auth-social" Text="Learner" CommandArgument="Learner" OnCommand="Demo_Command" CausesValidation="false" />
            <skill:SubmitButton runat="server" CssClass="auth-social" Text="Instructor" CommandArgument="Instructor" OnCommand="Demo_Command" CausesValidation="false" />
            <skill:SubmitButton runat="server" CssClass="auth-social" Text="Program Leader" CommandArgument="Program Leader" OnCommand="Demo_Command" CausesValidation="false" />
            <skill:SubmitButton runat="server" CssClass="auth-social" Text="Admin" CommandArgument="Admin" OnCommand="Demo_Command" CausesValidation="false" />
          </div>
          <p class="auth-notice">Demo usernames: alex, john, sarah, admin. Password: <span class="auth-code">Demo1234!</span></p>
        </section>
        </asp:PlaceHolder>
      </div>
    </section>
  </div>
</div>
</div>
</asp:Content>
