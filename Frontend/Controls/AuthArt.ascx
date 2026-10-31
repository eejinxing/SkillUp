<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AuthArt.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.AuthArt" %>
<section class="auth-art" aria-hidden="true">
  <div class="auth-grain"></div>
  <a class="brand auth-brand has-skill-logo" href="Home" tabindex="-1"><img class="skill-logo" src="assets/skill-up-logo.png" alt="Skill UP" width="930" height="420"></a>
  <div class="auth-orbit">
    <span class="auth-orb"></span>
    <span class="auth-chip" style="--d:0"><i>01</i> Guided labs</span>
    <span class="auth-chip" style="--d:1"><i>02</i> Quizzes with instant feedback</span>
    <span class="auth-chip" style="--d:2"><i>03</i> Progress saves as you go</span>
    <span class="auth-chip" style="--d:3"><i>04</i> A learning companion</span>
  </div>
  <div class="auth-art-copy">
    <h2><%: Heading %></h2>
    <p><%: Lead %></p>
    <ol class="auth-steps">
      <asp:Repeater ID="StepList" runat="server"><ItemTemplate><li<%# Container.ItemIndex == CurrentStep ? " aria-current=\"step\"" : "" %>><span><%# (Container.ItemIndex + 1).ToString("00") %></span><strong><%#: Eval("Title") %></strong><small><%#: Eval("Detail") %></small></li></ItemTemplate></asp:Repeater>
    </ol>
  </div>
</section>
