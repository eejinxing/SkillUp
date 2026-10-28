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
  <div class="ep-layout">
    <aside class="ep-sidebar">
      <div class="ep-list-heading">
        <h2>My enquiries</h2>
        <span><%= Count %></span>
      </div>
      <p class="ep-local-note">Linked to this browser, or to your account when you&rsquo;re signed in. Use your ID and access code to open an enquiry on another device.</p>
      <div class="ep-list" aria-label="Your saved enquiries">
        <asp:Repeater ID="EnquiryList" runat="server">
          <ItemTemplate>
            <a class="ep-entry" href="EnquiryPortal?id=<%# Eval("Id") %>" aria-current="<%# (bool)Eval("Selected") ? "true" : "false" %>"><span><%#: Eval("Subject") %></span><small><%#: Eval("Date") %><i><%#: Eval("Status") %></i></small></a>
          </ItemTemplate>
        </asp:Repeater>
        <asp:PlaceHolder ID="EmptyList" runat="server"><p class="ep-empty">No saved enquiries yet.<br>Start a new one, or find an existing enquiry below.</p></asp:PlaceHolder>
      </div>
      <details class="ep-lookup"<%= LookupOpen ? " open" : "" %>>
        <summary>Find an existing enquiry <span aria-hidden="true">&#8599;</span></summary>
        <asp:Panel runat="server" DefaultButton="FindButton">
          <label>Enquiry ID
            <asp:TextBox ID="LookupId" runat="server" autocomplete="off" placeholder="Paste your enquiry ID" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="LookupId" ValidationGroup="lookup" ErrorMessage="Enter the enquiry ID." Display="Dynamic" CssClass="field-error" />
          </label>
          <label>Access code
            <asp:TextBox ID="LookupToken" runat="server" TextMode="Password" autocomplete="off" placeholder="Your access code" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="LookupToken" ValidationGroup="lookup" ErrorMessage="Enter the access code." Display="Dynamic" CssClass="field-error" />
          </label>
          <p class="ep-lookup-error" role="alert"><asp:Literal ID="LookupError" runat="server" /></p>
          <skill:SubmitButton ID="FindButton" runat="server" CssClass="ep-secondary" Text="Find enquiry" ValidationGroup="lookup" OnClick="Find_Click" />
        </asp:Panel>
      </details>
      <ol class="enq-steps ep-steps" aria-label="What happens after you send">
        <li><b>01</b><span>Send your message</span></li>
        <li><b>02</b><span>Get an enquiry ID and access code</span></li>
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
      <asp:Panel runat="server" CssClass="ep-editor" DefaultButton="SaveButton">
        <div class="ep-two">
          <label>Your name
            <asp:TextBox ID="NameBox" runat="server" MaxLength="100" autocomplete="name" placeholder="Your name" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="NameBox" ValidationGroup="editor" ErrorMessage="Enter your name." Display="Dynamic" CssClass="field-error" />
          </label>
          <label>Email address
            <asp:TextBox ID="EmailBox" runat="server" TextMode="Email" MaxLength="254" autocomplete="email" placeholder="you@example.com" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="editor" ErrorMessage="Enter your email." Display="Dynamic" CssClass="field-error" />
            <asp:RegularExpressionValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="editor" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Use an address like name@example.com." Display="Dynamic" CssClass="field-error" />
          </label>
        </div>
        <label>Subject
          <asp:TextBox ID="SubjectBox" runat="server" MaxLength="160" placeholder="What&rsquo;s your question about?" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="SubjectBox" ValidationGroup="editor" ErrorMessage="Add a short subject." Display="Dynamic" CssClass="field-error" />
        </label>
        <label>
          <span class="ep-label-row">Your message <small class="ep-count">Up to 3000 characters</small></span>
          <asp:TextBox ID="MessageBox" runat="server" TextMode="MultiLine" Rows="6" MaxLength="3000" placeholder="What did you try, and where did you get stuck?" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="MessageBox" ValidationGroup="editor" ErrorMessage="Write a few words about your question." Display="Dynamic" CssClass="field-error" />
        </label>
        <p class="ep-form-error" role="alert"><asp:Literal ID="EditorError" runat="server" /></p>
        <div class="ep-editor-actions">
          <skill:SubmitButton ID="SaveButton" runat="server" CssClass="ep-primary" ValidationGroup="editor" OnClick="Save_Click"><%= Selected == null ? "Send enquiry" : "Save changes" %> <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></skill:SubmitButton>
          <skill:SubmitButton ID="DeleteButton" runat="server" CssClass="ep-delete" Text="Delete enquiry" CausesValidation="false" OnClick="Delete_Click" />
        </div>
      </asp:Panel>
      <asp:Panel ID="DeleteConfirm" runat="server" CssClass="ep-delete-confirm" Visible="false">
        <p>Delete this enquiry? This cannot be undone.</p>
        <skill:SubmitButton runat="server" CssClass="ep-danger" Text="Delete permanently" CausesValidation="false" OnClick="ConfirmDelete_Click" />
        <a class="ep-secondary" href="EnquiryPortal?id=<%= Selected?.Id %>">Cancel</a>
      </asp:Panel>
      <asp:PlaceHolder ID="AccessPanel" runat="server">
        <details class="ep-access"<%= Token != null ? " open" : "" %>>
          <summary>Enquiry ID &amp; access code</summary>
          <p>Keep these to open this enquiry on another device.</p>
          <dl>
            <dt>Enquiry ID</dt>
            <dd><%= Selected?.Id %></dd>
            <dt>Access code</dt>
            <dd><%: Token ?? "Shown once, when you sent or found this enquiry." %></dd>
          </dl>
        </details>
      </asp:PlaceHolder>
    </div>
  </div>
</section>
</asp:Content>
