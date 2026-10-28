<%@ Page Title="Contact us" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="SKILL_UP.Frontend.ContactPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
    <div class="home-refined contact-page">
        <skill:EnquiryForm runat="server" />
    </div>
</asp:Content>
