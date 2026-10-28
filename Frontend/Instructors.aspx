<%@ Page Title="Meet our instructors" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Instructors.aspx.cs" Inherits="SKILL_UP.Frontend.InstructorsPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
    <div class="page-head">
        <div>
            <h1 tabindex="-1" id="page-title">Meet our instructors</h1>
            <p>Practical guidance, one concept at a time.</p>
        </div>
        <div class="actions tight"></div>
    </div>
    <div class="grid">
        <asp:Repeater ID="Team" runat="server">
            <ItemTemplate>
                <article class="panel instructor-card">
                    <%# Ui.Avatar((UserRow)Eval("User")) %>
                    <h3><%#: Eval("User.Name") %></h3>
                    <p><%#: Eval("Bio") %></p>
                    <small><%# Eval("Published") %> published courses</small>
                    <a class="button ghost" href="Catalog">Explore courses</a>
                </article>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</asp:Content>
