<%@ Page Title="Certificates" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Certificates.aspx.cs" Inherits="SKILL_UP.Frontend.CertificatesPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<asp:MultiView ID="Views" runat="server" ActiveViewIndex="0">
  <asp:View runat="server">
    <div class="page-head"><div><h1 tabindex="-1" id="page-title">Certificates</h1><p>Your completed courses, recognised.</p></div><div class="actions tight"></div></div>
    <div class="grid">
      <asp:Repeater ID="Earned" runat="server">
        <ItemTemplate>
          <article class="panel"><span class="tag green">&#10003; Completed</span><h2 style="margin:20px 0"><%#: Eval("Title") %></h2><p><%# Eval("Lessons") %> activities completed</p><div class="actions"><a class="button primary" href="Certificates?id=<%# Eval("Id") %>">View certificate</a></div></article>
        </ItemTemplate>
      </asp:Repeater>
      <asp:PlaceHolder ID="NoneYet" runat="server"><div class="empty"><h3>Your first certificate is ahead</h3><p>Complete every activity and pass the quiz to unlock your certificate.</p></div></asp:PlaceHolder>
    </div>
  </asp:View>
  <asp:View runat="server">
    <div class="page-head"><div><h1 tabindex="-1" id="page-title">Your certificate</h1><p>Print or save as a PDF from your browser.</p></div><div class="actions tight"><button type="button" class="primary" onclick="window.print()">Print / Save PDF</button></div></div>
    <div class="certificate">
      <div class="brand" style="justify-content:center">&#8599; Skill UP</div>
      <p style="margin-top:35px">CERTIFICATE OF COMPLETION</p>
      <h1>A new skill. A new possibility.</h1>
      <p>This recognises that</p>
      <div class="name"><%: Me.Name %></div>
      <p>has completed all learning activities in</p>
      <h2 style="margin:20px"><%: Shown?.Title %></h2>
      <div class="divider"></div>
      <small>Certificate ID: <%= Me.Id %>-<%= Shown?.Id %><br>Skill UP &middot; Demo certificate</small>
    </div>
  </asp:View>
  <asp:View runat="server">
    <div class="empty"><h3>Certificate not earned</h3><p>Complete every activity in the course first.</p></div>
  </asp:View>
</asp:MultiView>
</asp:Content>
