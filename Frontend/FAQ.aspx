<%@ Page Title="Frequently asked questions" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="FAQ.aspx.cs" Inherits="SKILL_UP.Frontend.FAQPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
    <div class="page-head">
        <div>
            <h1 tabindex="-1" id="page-title">Frequently asked questions</h1>
            <p>Answers for your next learning step.</p>
        </div>
        <div class="actions tight"></div>
    </div>
    <section class="panel">
        <asp:Repeater ID="Questions" runat="server">
            <ItemTemplate>
                <details class="list-item">
                    <summary><%#: Eval("Question") %></summary>
                    <p><%#: Eval("Answer") %></p>
                </details>
            </ItemTemplate>
        </asp:Repeater>
    </section>
</asp:Content>
