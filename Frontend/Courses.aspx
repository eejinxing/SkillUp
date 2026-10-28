<%@ Page Title="My courses" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Courses.aspx.cs" Inherits="SKILL_UP.Frontend.CoursesPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
  <div class="page-head">
    <div><h1 tabindex="-1" id="page-title">My courses</h1><p>Build, refine and submit your next learning experience.</p></div>
    <div class="actions tight"><a class="button primary" href="CourseEditor">+ Create course</a></div>
  </div>
  <asp:Panel runat="server" CssClass="toolbar" DefaultButton="FilterButton">
    <asp:TextBox ID="SearchBox" runat="server" aria-label="Search managed courses" placeholder="Search courses…" />
    <asp:DropDownList ID="StatusFilter" runat="server" aria-label="Filter course status">
      <asp:ListItem>All statuses</asp:ListItem><asp:ListItem>Draft</asp:ListItem><asp:ListItem>Pending review</asp:ListItem><asp:ListItem>Published</asp:ListItem><asp:ListItem>Changes requested</asp:ListItem><asp:ListItem>Rejected</asp:ListItem><asp:ListItem>Hidden</asp:ListItem>
    </asp:DropDownList>
    <skill:SubmitButton ID="FilterButton" runat="server" CssClass="ghost" Text="Filter" CausesValidation="false" OnClick="Filter_Click" />
  </asp:Panel>
  <div class="grid" id="results">
    <asp:Repeater ID="Results" runat="server"><ItemTemplate><skill:CourseCard runat="server" Course='<%# Container.DataItem %>' Manage="true" /></ItemTemplate></asp:Repeater>
    <asp:PlaceHolder ID="NoCourses" runat="server"><div class="empty"><h3><%= Mine.Count == 0 ? "Create your first course" : "No matching courses" %></h3><p><%= Mine.Count == 0 ? "" : "Try another search or status." %></p></div></asp:PlaceHolder>
  </div>
</asp:Content>
