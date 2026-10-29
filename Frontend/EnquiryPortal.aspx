<%@ Page Title="Your enquiries." Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="EnquiryPortal.aspx.cs" Inherits="SKILL_UP.Frontend.EnquiryPortalPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="enquiry-portal" aria-labelledby="ep-title">
  <a class="ep-back" href="Home">&larr; Back to Skill UP</a>
  <header class="ep-head">
    <div>
      <h1 id="ep-title">Your enquiries.</h1>
      <p>A place for your questions. And your next steps.</p>
    </div>
    <a class="ep-primary" href="EnquiryPortal">+ New enquiry</a>
  </header>
  <%-- Sending, saving and deleting redraw only this part (no page reload). --%>
  <asp:UpdatePanel ID="PortalPanel" runat="server">
    <ContentTemplate>
      <div class="ep-layout">
        <aside class="ep-sidebar">
          <div class="ep-list-heading">
            <h2>My enquiries</h2>
            <span><%= Count %></span>
          </div>
          <p class="ep-local-note">Linked to this browser, or to your account when you&rsquo;re signed in.</p>
          <div class="ep-list" aria-label="Your saved enquiries">
            <asp:Repeater ID="EnquiryList" runat="server">
              <ItemTemplate>
                <a class="ep-entry" href="EnquiryPortal?id=<%# Eval("Id") %>" aria-current="<%# (bool)Eval("Selected") ? "true" : "false" %>"><span><%#: Eval("Subject") %></span><small><%#: Eval("Date") %><i><%#: Eval("Status") %></i></small></a>
              </ItemTemplate>
            </asp:Repeater>
            <asp:PlaceHolder ID="EmptyList" runat="server"><p class="ep-empty">No saved enquiries yet.<br>Start a new one with the form.</p></asp:PlaceHolder>
          </div>
          <ol class="enq-steps ep-steps" aria-label="What happens after you send">
            <li><b>01</b><span>Send your message</span></li>
            <li><b>02</b><span>See it listed under My enquiries</span></li>
            <li><b>03</b><span>View, edit or delete it in your portal</span></li>
          </ol>
        </aside>
        <div class="ep-detail">
          <header class="ep-detail-head">
            <div>
              <h2><%: Selected == null ? "New enquiry" : Selected.Subject %></h2>
              <p><%: Selected == null ? "Tell us what you need a hand with." : "Submitted " + Ui.Date(Selected.CreatedAt) %></p>
            </div>
            <asp:PlaceHolder ID="StatusBadge" runat="server"><span class="ep-status"><%: Selected?.Status %></span></asp:PlaceHolder>
          </header>
          <%-- Same fields and look as the contact form on the landing page (Controls/EnquiryForm.ascx). --%>
          <asp:PlaceHolder ID="SavedPanel" runat="server" Visible="false">
            <div class="home-enquiry-saved ep-saved" role="status">
              <h3>Enquiry saved</h3>
              <p>Thanks, we have your message. View, edit or delete it any time under My enquiries in this browser.</p>
              <div class="actions"><a class="home-enquiry-open" href="EnquiryPortal?id=<%= Selected?.Id %>">View or edit this enquiry &rarr;</a></div>
            </div>
          </asp:PlaceHolder>
          <asp:Panel ID="EditorPanel" runat="server" CssClass="home-enquiry-form ep-landing-form" DefaultButton="SaveButton">
            <asp:PlaceHolder ID="Topics" runat="server">
              <fieldset class="home-enquiry-topics">
                <legend>What can we help with?</legend>
                <div>
                  <label><asp:RadioButton ID="TopicCourse" runat="server" GroupName="topic" /><span><strong>Choosing a course</strong><small>Levels, paths and fit</small></span></label>
                  <label><asp:RadioButton ID="TopicActivity" runat="server" GroupName="topic" /><span><strong>A practical activity</strong><small>Labs, quizzes and hints</small></span></label>
                  <label><asp:RadioButton ID="TopicAccount" runat="server" GroupName="topic" /><span><strong>My account</strong><small>Sign-in, profile, progress</small></span></label>
                  <label><asp:RadioButton ID="TopicOther" runat="server" GroupName="topic" /><span><strong>Something else</strong><small>Anything we missed</small></span></label>
                </div>
              </fieldset>
            </asp:PlaceHolder>
            <div class="home-enquiry-pair">
              <label class="home-enquiry-field"><span>Your name</span>
                <asp:TextBox ID="NameBox" runat="server" MaxLength="100" autocomplete="name" placeholder="Alex Morgan" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="NameBox" ValidationGroup="editor" ErrorMessage="Enter your name." Display="Dynamic" CssClass="field-error" />
              </label>
              <label class="home-enquiry-field"><span>Email address</span>
                <asp:TextBox ID="EmailBox" runat="server" TextMode="Email" MaxLength="254" autocomplete="email" placeholder="you@example.com" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="editor" ErrorMessage="Enter your email." Display="Dynamic" CssClass="field-error" />
                <asp:RegularExpressionValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="editor" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Use an address like name@example.com." Display="Dynamic" CssClass="field-error" />
              </label>
            </div>
            <label class="home-enquiry-field"><span>Subject</span>
              <asp:TextBox ID="SubjectBox" runat="server" MaxLength="160" placeholder="Pick a topic above or write your own" />
            </label>
            <label class="home-enquiry-field"><span>Your message <small class="home-enquiry-count">Up to 3000 characters</small></span>
              <asp:TextBox ID="MessageBox" runat="server" TextMode="MultiLine" Rows="3" MaxLength="3000" placeholder="What did you try, and where did you get stuck?" />
              <asp:RequiredFieldValidator runat="server" ControlToValidate="MessageBox" ValidationGroup="editor" ErrorMessage="Write a few words about your question." Display="Dynamic" CssClass="field-error" />
            </label>
            <p class="form-error" role="alert"><asp:Literal ID="EditorError" runat="server" /></p>
            <p class="ep-notice" role="status"><asp:Literal ID="EditorNotice" runat="server" /></p>
            <div class="home-enquiry-actions">
              <div class="home-enquiry-action-copy">
                <p id="home-enquiry-note">Manage your message under My enquiries.</p>
                <small class="home-enquiry-ready">All fields are required.</small>
              </div>
              <skill:SubmitButton ID="SaveButton" runat="server" CssClass="home-enquiry-submit" ValidationGroup="editor" OnClick="Save_Click"><span><%= Selected == null ? "Send enquiry" : "Save changes" %></span><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg></skill:SubmitButton>
            </div>
          </asp:Panel>
          <div class="ep-delete-row" id="DeleteRow" runat="server"><skill:SubmitButton ID="DeleteButton" runat="server" CssClass="ep-delete" Text="Delete enquiry" CausesValidation="false" OnClick="Delete_Click" /></div>
          <asp:Panel ID="DeleteConfirm" runat="server" CssClass="ep-delete-confirm" Visible="false">
            <p>Delete this enquiry? This cannot be undone.</p>
            <skill:SubmitButton runat="server" CssClass="ep-danger" Text="Delete permanently" CausesValidation="false" OnClick="ConfirmDelete_Click" />
            <skill:SubmitButton runat="server" CssClass="ep-secondary" Text="Cancel" CausesValidation="false" OnClick="Cancel_Click" />
          </asp:Panel>
        </div>
      </div>
    </ContentTemplate>
  </asp:UpdatePanel>
</section>
</asp:Content>
