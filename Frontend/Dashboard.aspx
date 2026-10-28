<%@ Page Title="Overview" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="SKILL_UP.Frontend.DashboardPage" %>
<%-- Each role has its own overview, in Controls/<Role>Dashboard.ascx. --%>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
    <asp:PlaceHolder ID="RoleDashboard" runat="server" />
</asp:Content>
