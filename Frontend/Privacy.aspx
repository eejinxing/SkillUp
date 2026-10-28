<%@ Page Title="Privacy information" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Privacy.aspx.cs" Inherits="SKILL_UP.Frontend.PrivacyPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
    <div class="page-head">
        <div>
            <h1 tabindex="-1" id="page-title">Privacy information</h1>
            <p>Information for this learning platform prototype.</p>
        </div>
        <div class="actions tight"></div>
    </div>
    <section class="panel">
        <h2>Your demo data</h2>
        <p>This is a learning platform prototype. Sample accounts and your changes are saved in this site's database. Use fictional information while testing. Production policies, retention rules and service terms will be provided by the platform operator before launch.</p>
        <a class="button ghost" href="<%= HelpUrl %>">Get help</a>
    </section>
</asp:Content>
