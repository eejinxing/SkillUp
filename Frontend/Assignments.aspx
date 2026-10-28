<%@ Page Title="Instructor assignments" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Assignments.aspx.cs" Inherits="SKILL_UP.Frontend.AssignmentsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="asg-page" aria-labelledby="page-title">
  <header class="asg-head">
    <div><h1 id="page-title" tabindex="-1">Instructor assignments</h1><p>Keep each course connected to the right instructor.</p></div>
    <asp:Panel runat="server" CssClass="asg-search" DefaultButton="SearchButton">
      <label class="sr-only" for="<%= SearchBox.ClientID %>">Search assignments</label>
      <asp:TextBox ID="SearchBox" runat="server" TextMode="Search" placeholder="Search courses or instructors…" autocomplete="off" />
      <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Search_Click" />
    </asp:Panel>
  </header>
  <div class="asg-summary"><%= Summary %></div>
  <asp:Panel ID="AssignForm" runat="server" CssClass="panel asg-assign-form" DefaultButton="AssignSave" Visible="false">
    <h2 id="asg-assign">Assign instructor</h2>
    <p><%: AssignCourse?.Title %></p>
    <label>Instructor<asp:DropDownList ID="AssignInstructor" runat="server" DataTextField="Name" DataValueField="Id" /></label>
    <div class="actions"><skill:SubmitButton ID="AssignSave" runat="server" CssClass="primary" Text="Save assignment" CausesValidation="false" OnClick="Assign_Click" /><a class="button ghost" href="<%: ListUrl %>">Cancel</a></div>
  </asp:Panel>
  <%= Cards %>
</section>
</asp:Content>
