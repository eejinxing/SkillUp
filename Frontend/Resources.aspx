<%@ Page Title="Learning resources" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Resources.aspx.cs" Inherits="SKILL_UP.Frontend.ResourcesPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
  <div class="page-head"><div><h1 tabindex="-1" id="page-title">Learning resources</h1><p>Course content, organised by the courses you teach.</p></div><div class="actions tight"></div></div>
  <div class="grid">
    <asp:Repeater ID="Lessons" runat="server">
      <ItemTemplate>
        <article class="panel">
          <div class="course-art <%# Eval("Colour") %>" style="height:100px;border-radius:10px;margin-bottom:16px"><b style="font-size:28px"><%# Eval("Kind") %></b></div>
          <h3><%#: Eval("Title") %></h3>
          <p><%#: Eval("Course") %></p>
          <small><%# Eval("Minutes") %> min &middot; <%# Eval("Audience") %></small>
          <div class="actions"><a class="button" href="CourseEditor?id=<%# Eval("CourseId") %>&amp;lesson=<%# Eval("Id") %>#lesson-editor">Edit resource</a></div>
        </article>
      </ItemTemplate>
    </asp:Repeater>
    <asp:PlaceHolder ID="NoLessons" runat="server"><div class="empty"><h3>No resources yet</h3><p></p></div></asp:PlaceHolder>
  </div>
  <skill:CourseFiles runat="server" />
</asp:Content>
