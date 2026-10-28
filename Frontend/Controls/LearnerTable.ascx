<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LearnerTable.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.LearnerTable" %>
<asp:Repeater ID="Rows" runat="server">
  <HeaderTemplate><div class="table-wrap"><table><thead><tr><th scope="col">Learner</th><th scope="col">Course</th><th scope="col">Progress</th><th scope="col">Status</th><th scope="col">Last active</th><th scope="col"></th></tr></thead><tbody></HeaderTemplate>
  <ItemTemplate>
    <tr>
      <td><div class="row lr-person"><%# Eval("Avatar") %><div><strong><%#: Eval("Name") %></strong><p><%#: Eval("Email") %></p></div></div></td>
      <td><span class="lr-course"><%#: Eval("Course") %></span></td>
      <td><span class="lr-meter" role="progressbar" aria-label="<%#: Eval("Course") %> completion" aria-valuemin="0" aria-valuemax="100" aria-valuenow="<%# Eval("Progress") %>"><span><i style="--w:<%# Eval("Progress") %>%"></i></span><b><%# Eval("Progress") %>%</b></span></td>
      <td><span class="lr-status" data-status="<%# Eval("StatusKey") %>"><%# Eval("Status") %></span></td>
      <td><span class="lr-date"><%# Eval("LastActive") %></span></td>
      <td><div class="actions"><a class="link lr-view" href="Learners?learner=<%# Eval("UserId") %>&amp;course=<%# Eval("CourseId") %>">View</a><skill:SubmitButton runat="server" CssClass="ghost danger" Text="Remove" CausesValidation="false" Visible='<%# CanRemove %>' CommandArgument='<%# Eval("UserId") + ":" + Eval("CourseId") %>' OnCommand="Remove_Command" data-confirm="Remove this enrolment? The learner's progress for this course will be removed." /></div></td>
    </tr>
  </ItemTemplate>
  <FooterTemplate></tbody></table></div></FooterTemplate>
</asp:Repeater>
<asp:PlaceHolder ID="Empty" runat="server" Visible="false"><div class="empty"><h3>No matching records</h3><p>Try another search or create a new record.</p></div></asp:PlaceHolder>
