<%@ Page Title="Reset your password" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="SKILL_UP.Frontend.ForgotPasswordPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<div class="auth-screen">
<div id="dialog" class="auth-dialog" data-auth="forgot" role="dialog" aria-modal="true" aria-labelledby="dialog-title">
  <a class="dialog-close" href="Home" aria-label="Close">&times;</a>
  <div class="auth-v2 auth-modal auth-forgot">
    <skill:AuthArt runat="server" Kind="forgot" />
    <section class="auth-form">
      <div class="auth-card">
        <h2 id="dialog-title">Reset your password</h2>
        <asp:MultiView ID="Steps" runat="server" ActiveViewIndex="0">
          <asp:View runat="server">
            <p class="auth-modal-sub">Enter the email you registered with. In this demo, the reset link appears on screen.</p>
            <asp:Panel runat="server" DefaultButton="SendButton">
          <div class="auth-control">
            <div class="auth-label-row"><label for="AuthEmail">Email</label></div>
            <span class="auth-field"><asp:TextBox ID="AuthEmail" runat="server" ClientIDMode="Static" TextMode="Email" placeholder="The email you registered with" autocomplete="email" /></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="AuthEmail" ErrorMessage="Enter your email." Display="Dynamic" CssClass="field-error" />
            <asp:RegularExpressionValidator runat="server" ControlToValidate="AuthEmail" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Enter a valid email address." Display="Dynamic" CssClass="field-error" />
          </div>
        <p class="form-error" role="alert"><asp:Literal ID="FormError" runat="server" /></p>
              <div class="actions"><skill:SubmitButton ID="SendButton" runat="server" CssClass="primary" Text="Send reset link" OnClick="Send_Click" /></div>
            </asp:Panel>
          </asp:View>
          <asp:View runat="server">
            <p class="auth-modal-sub">This prototype does not send email. Use the link below within 15 minutes.</p>
            <div class="actions"><asp:HyperLink ID="ResetLink" runat="server" CssClass="button primary" Text="Choose a new password" /></div>
          </asp:View>
          <asp:View runat="server">
            <p class="auth-modal-sub">If that email belongs to an account, a reset link is on its way. It stays valid for 15 minutes.</p>
          </asp:View>
        </asp:MultiView>
        <p class="auth-switch">Remembered your password? <a class="link" href="#login">Log in</a></p>
      </div>
    </section>
  </div>
</div>
</div>
</asp:Content>
