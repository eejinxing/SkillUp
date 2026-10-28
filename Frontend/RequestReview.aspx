<%@ Page Title="Review deletion request" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="RequestReview.aspx.cs" Inherits="SKILL_UP.Frontend.RequestReviewPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:PlaceHolder ID="Denied" runat="server" Visible="false"><div class="empty"><h1>Request access denied</h1><p>This request is not assigned to your program.</p></div></asp:PlaceHolder>
<asp:PlaceHolder ID="Review" runat="server">
  <div class="page-head"><div><h1 tabindex="-1" id="page-title">Review deletion request</h1><p><%: R?.CourseTitle %></p></div><div class="actions tight"><a class="button ghost" href="Approvals">&larr; Course approvals</a></div></div>
  <div class="split">
    <section class="panel stack">
      <h2><%: R?.CourseTitle %></h2>
      <p><%: R?.Reason %></p>
      <div><strong>Submitted by</strong><p><%: Requester %> &middot; <%= R == null ? "" : Ui.Stamp(R.CreatedAt) %></p></div>
      <p>Current course: <%: CourseTitle %> &middot; <%= Learners %> enrolled learners</p>
      <p>Approving removes an unenrolled course and its materials from the mock workspace. The request timeline is retained.</p>
      <skill:SubmitButton ID="StartReview" runat="server" Text="Start review" CausesValidation="false" OnClick="StartReview_Click" />
      <div class="rq-review-events"><%= History %></div>
    </section>
    <aside class="panel"><h2>Your decision</h2>
      <asp:Panel ID="DecisionForm" runat="server" CssClass="stack" DefaultButton="SubmitDecision">
        <label>Decision<asp:DropDownList ID="Decision" runat="server"><asp:ListItem>Approved</asp:ListItem><asp:ListItem Selected="True">Rejected</asp:ListItem></asp:DropDownList></label>
        <label>Feedback to instructor<asp:TextBox ID="Feedback" runat="server" TextMode="MultiLine" Rows="5" MaxLength="2000" /><asp:RequiredFieldValidator runat="server" ControlToValidate="Feedback" ValidationGroup="decision" ErrorMessage="Write feedback for the instructor." Display="Dynamic" CssClass="field-error" /></label>
        <div class="actions"><skill:SubmitButton ID="SubmitDecision" runat="server" CssClass="primary" Text="Submit deletion decision" ValidationGroup="decision" OnClick="SubmitDecision_Click" /></div>
      </asp:Panel>
      <asp:PlaceHolder ID="Decided" runat="server"><p><%: R?.Status %></p></asp:PlaceHolder>
    </aside>
  </div>
</asp:PlaceHolder>
</asp:Content>
