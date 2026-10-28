<%@ Page Title="Review submission" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="CourseReview.aspx.cs" Inherits="SKILL_UP.Frontend.CourseReviewPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:PlaceHolder ID="Denied" runat="server" Visible="false"><div class="empty"><h3>Course access denied</h3><p>New items will appear here when they are available.</p></div></asp:PlaceHolder>
<asp:PlaceHolder ID="Review" runat="server">
  <div class="page-head"><div><h1 tabindex="-1" id="page-title">Review submission</h1><p><%: C?.Title %></p></div><div class="actions tight"><a class="button ghost" href="Approvals">&larr; Approvals</a></div></div>
  <div class="split">
    <section class="panel stack"><%= Submission %></section>
    <aside class="stack">
      <section class="panel"><h2>Your decision</h2>
        <skill:SubmitButton ID="StartReview" runat="server" CssClass="ghost" Text="Start review" CausesValidation="false" OnClick="StartReview_Click" />
        <asp:Panel ID="DecisionForm" runat="server" CssClass="stack" DefaultButton="SubmitDecision">
          <label>Decision<asp:DropDownList ID="Decision" runat="server"><asp:ListItem>Approved</asp:ListItem><asp:ListItem>Changes requested</asp:ListItem><asp:ListItem>Rejected</asp:ListItem></asp:DropDownList></label>
          <label>Feedback to instructor<asp:TextBox ID="Feedback" runat="server" TextMode="MultiLine" Rows="5" MaxLength="2000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="Feedback" ValidationGroup="decision" ErrorMessage="Write feedback for the instructor." Display="Dynamic" CssClass="field-error" /></label>
          <div class="actions"><skill:SubmitButton ID="SubmitDecision" runat="server" CssClass="primary" Text="Submit decision" ValidationGroup="decision" OnClick="SubmitDecision_Click" /></div>
        </asp:Panel>
        <asp:PlaceHolder ID="NotWaiting" runat="server"><p class="notice">This course is not awaiting a decision.</p></asp:PlaceHolder>
      </section>
      <section class="panel"><h2>Review history</h2><%= History %></section>
    </aside>
  </div>
  <skill:CourseFiles ID="Files" runat="server" />
</asp:PlaceHolder>
</asp:Content>
