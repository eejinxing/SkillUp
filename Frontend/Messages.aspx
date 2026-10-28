<%@ Page Title="Communication workspace" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Messages.aspx.cs" Inherits="SKILL_UP.Frontend.MessagesPage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="skill-chat sc-v2 sc-workspace <%= Selected != null && QueryInt("id") != null ? "sc-conversation-open" : "" %>">
  <h1 class="sc-sr-only" id="page-title" tabindex="-1">Communication workspace</h1>
  <aside class="sc-contacts chat-contacts" aria-label="Workspace and contacts">
    <header class="sc-workspace-identity"><span class="sc-workspace-mark">S</span><div><strong>Skill workspace</strong><small><%: Me.Name %></small></div></header>
    <nav class="sc-channels" aria-label="Course announcement channels">
      <span class="sc-sidebar-label">COURSES</span>
      <a href="<%= Url(0, 0, SelectedId) %>" class="sc-channel <%= Channel == 0 ? "active" : "" %>"<%= Channel == 0 ? " aria-current=\"true\"" : "" %>><span class="sc-channel-dot all"></span><span>All announcements</span><small><%= AllAnnouncementCount %></small></a>
      <asp:Repeater ID="Channels" runat="server">
        <ItemTemplate><a href="<%# Eval("Url") %>" class="sc-channel <%# (bool)Eval("Active") ? "active" : "" %>"<%# (bool)Eval("Active") ? " aria-current=\"true\"" : "" %>><span class="sc-channel-dot" style="--channel-color:<%# Eval("Colour") %>"></span><span><%#: Eval("Title") %></span><small><%# Eval("Count") %></small></a></ItemTemplate>
      </asp:Repeater>
      <% if (Courses.Count == 0) { %><p class="sc-sidebar-empty">Your courses will appear here.</p><% } %>
    </nav>
    <header class="sc-contacts-head"><h2>DIRECT MESSAGES</h2><% if (TotalUnread > 0) { %><span class="sc-total" aria-label="<%= TotalUnread %> unread"><%= TotalUnread %></span><% } %></header>
    <asp:Panel runat="server" CssClass="sc-search" DefaultButton="SearchButton">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>
      <asp:TextBox ID="ContactSearch" runat="server" ClientIDMode="Static" TextMode="Search" aria-label="Search contacts" placeholder="Find a person…" autocomplete="off" />
      <skill:SubmitButton ID="SearchButton" runat="server" CssClass="sr-only" Text="Search" CausesValidation="false" OnClick="Search_Click" />
    </asp:Panel>
    <div class="sc-contact-scroll contact-list" aria-label="Chat contacts">
      <asp:Repeater ID="Contacts" runat="server">
        <ItemTemplate>
          <a href="<%# Eval("Url") %>" class="sc-contact <%# (bool)Eval("Active") ? "active" : "" %> <%# (int)Eval("Unread") > 0 ? "has-unread" : "" %>"<%# (bool)Eval("Active") ? " aria-current=\"true\"" : "" %>>
            <%# Eval("FaceHtml") %>
            <div class="sc-contact-copy">
              <div class="sc-contact-line"><strong><%#: Eval("Name") %></strong><%# Eval("TimeHtml") %></div>
              <div class="sc-contact-line"><p><%# Eval("PreviewHtml") %></p><%# (int)Eval("Unread") > 0 ? "<span class=\"sc-unread\" aria-label=\"" + Eval("Unread") + " unread messages\"><b>" + Eval("Unread") + "</b></span>" : "" %></div>
            </div>
          </a>
        </ItemTemplate>
      </asp:Repeater>
      <% if (ContactCount == 0) { %><p class="sc-contact-empty"><%= ContactSearch.Text.Length > 0 ? "No matching contacts." : "No contacts available yet." %></p><% } %>
    </div>
    <a class="sc-sidebar-help" href="Help"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 5h16v11H9l-5 4Z"/></svg> Help &amp; support</a>
  </aside>

  <section class="sc-announcements" aria-label="Course announcements">
    <header class="sc-announcements-heading">
      <span>Workspace <b>/</b> <strong>Announcements</strong></span>
      <asp:PlaceHolder ID="NewAnnouncementLink" runat="server"><a class="sc-new-announcement button" href="<%= Url(Channel, 0, SelectedId) %>&amp;compose=1#sc-compose">+ New announcement</a></asp:PlaceHolder>
    </header>
    <div class="sc-announcements-scroll">
      <asp:Panel ID="ComposePanel" runat="server" CssClass="sc-compose panel" ClientIDMode="Static" DefaultButton="PublishButton" Visible="false">
        <h3 id="sc-compose">New announcement</h3>
        <label>Course <asp:DropDownList ID="ComposeCourse" runat="server" DataTextField="Title" DataValueField="Id" /></label>
        <label>Title
          <asp:TextBox ID="ComposeTitle" runat="server" MaxLength="200" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="ComposeTitle" ValidationGroup="compose" ErrorMessage="Add a title." Display="Dynamic" CssClass="field-error" />
        </label>
        <label>Message
          <asp:TextBox ID="ComposeBody" runat="server" TextMode="MultiLine" Rows="5" MaxLength="10000" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="ComposeBody" ValidationGroup="compose" ErrorMessage="Write the announcement." Display="Dynamic" CssClass="field-error" />
        </label>
        <div class="actions"><skill:SubmitButton ID="PublishButton" runat="server" CssClass="primary" Text="Publish announcement" ValidationGroup="compose" OnClick="Publish_Click" /><a class="button ghost" href="<%= Url(Channel, AnnouncementId, SelectedId) %>">Cancel</a></div>
      </asp:Panel>
      <% if (Announcement != null) { %>
      <article class="sc-announcement" data-announcement-id="<%= Announcement.Id %>">
        <div class="sc-announcement-eyebrow"><span class="sc-announcement-symbol" aria-hidden="true">&#8599;</span><span>COURSE UPDATE</span><time><%= Ui.Date(Announcement.CreatedAt) %></time></div>
        <h2><%: Announcement.Title %></h2>
        <div class="sc-announcement-tags"><a href="Course?id=<%= ActiveCourse.Id %>"><%: ActiveCourse.Title %></a><span><i aria-hidden="true"></i>Announcement</span></div>
        <div class="sc-announcement-author"><%= Author == null ? "" : Face(Author, "") %><div><strong><%: Author?.Name ?? "Former instructor" %></strong><small><%: Author?.Role ?? "Course team" %> &middot; Posted <%= Ui.Date(Announcement.CreatedAt) %></small></div></div>
        <p class="sc-announcement-body"><%: Announcement.Message %></p>
        <a class="sc-open-course" href="Course?id=<%= ActiveCourse.Id %>">Open course <%= SendIcon %></a>
      </article>
      <section class="sc-comments sc-discussion" aria-labelledby="sc-discussion-title">
        <header><h3 id="sc-discussion-title">Comments</h3><span><%= CommentCount %></span><% if (CommentCount == 0) { %><small>Questions and tips for the class go here.</small><% } %></header>
        <% if (CommentCount > 0) { %><ol class="sc-comment-list"><% } %>
        <asp:Repeater ID="Comments" runat="server">
          <ItemTemplate>
            <li class="sc-comment">
              <header class="sc-announcement-author sc-comment-author"><%# Eval("FaceHtml") %><div><strong><%#: Eval("Name") %></strong><small><%#: Eval("Role") %> &middot; <time><%# Eval("When") %></time></small></div>
                <skill:SubmitButton runat="server" CssClass="sc-comment-delete" Text="Delete" CausesValidation="false" Visible='<%# Eval("Mine") %>' CommandArgument='<%# Eval("Id") %>' OnCommand="DeleteComment_Command" aria-label="Delete your comment" data-confirm="Delete your comment?" />
              </header>
              <p class="sc-announcement-body sc-comment-body"><%#: Eval("Text") %></p>
            </li>
          </ItemTemplate>
        </asp:Repeater>
        <% if (CommentCount > 0) { %></ol><% } %>
        <asp:Panel runat="server" CssClass="sc-comment-form" DefaultButton="PostCommentButton">
          <div class="sc-comment-box">
            <div class="sc-comment-input"><%= Face(Me, "") %><label class="sr-only" for="CommentBox">Write a comment</label><asp:TextBox ID="CommentBox" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="1" MaxLength="1000" /></div>
            <div class="sc-comment-bar"><span class="sc-comment-hint"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 5h16v11H9l-5 4Z"/></svg><span>Visible to everyone in <%: ActiveCourse.Title %></span></span><skill:SubmitButton ID="PostCommentButton" runat="server" CssClass="sc-comment-post" Text="Post" CausesValidation="false" OnClick="PostComment_Click" /></div>
          </div>
        </asp:Panel>
      </section>
      <% } else { %>
      <div class="sc-announcement-empty"><span aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 5h16v11H9l-5 4Z"/></svg></span><h2>No announcements yet</h2><p><%= Channel != 0 ? "Updates from this course will appear here." : "Course announcements will appear here when your teaching team shares an update." %></p></div>
      <% } %>
      <% if (ChannelAnnouncements.Count > 1) { %>
      <section class="sc-announcement-list" aria-label="All announcements in this channel">
        <header><h3>All updates</h3><span><%= ChannelAnnouncements.Count %></span></header>
        <asp:Repeater ID="AnnouncementList" runat="server">
          <ItemTemplate><a href="<%# Eval("Url") %>" class="<%# (bool)Eval("Active") ? "active" : "" %>"<%# (bool)Eval("Active") ? " aria-current=\"true\"" : "" %>><span><strong><%#: Eval("Title") %></strong><small><%#: Eval("Course") %> &middot; <%# Eval("Date") %></small></span><%= SendIcon %></a></ItemTemplate>
        </asp:Repeater>
      </section>
      <% } %>
      <% if (ActiveCourse != null) { %>
      <section class="sc-course-context">
        <header><h3>In this course</h3><a href="Course?id=<%= ActiveCourse.Id %>">View course</a></header>
        <div>
          <asp:Repeater ID="CourseActivities" runat="server">
            <ItemTemplate><a href="Course?id=<%# Eval("CourseId") %>"><span class="sc-activity-index"><%# (Container.ItemIndex + 1).ToString("00") %></span><span><strong><%#: Eval("Title") %></strong><small><%#: Eval("Type") %> &middot; <%# Eval("Minutes") %> min</small></span><%= SendIcon %></a></ItemTemplate>
          </asp:Repeater>
          <% if (ActivityCount == 0) { %><p>No activities published yet.</p><% } %>
        </div>
      </section>
      <% } %>
      <p class="sc-announcement-footnote">Course updates stay here. Private messages stay in your conversation.</p>
    </div>
  </section>

  <section class="sc-pane chat-pane" aria-label="Conversation">
    <% if (Selected != null) { %>
    <header class="sc-chat-heading">
      <a class="sc-back" href="Messages" aria-label="Back to chats"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M19 12H5m6-6-6 6 6 6"/></svg></a>
      <%= Face(Selected, "md") %>
      <div class="sc-heading-copy"><h2><%: Selected.Name %></h2><small><%: Selected.Role %></small></div>
      <skill:SubmitButton ID="MarkReadButton" runat="server" CssClass="sc-mark-read" Text="Mark as read" CausesValidation="false" OnClick="MarkRead_Click" />
      <% if (Unread == 0) { %><span class="sc-read-label"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 12.5 4.5 4.5L19 7.5"/></svg>Up to date</span><% } %>
      <div class="sc-call-actions">
        <button type="button" data-start-call="voice" data-call-name="<%: Selected.Name %>" data-call-role="<%: Selected.Role %>" aria-label="Start voice call preview"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 3 4 1 1 5-3 2c2 3 3 4 6 6l2-3 5 1 1 4c-1 4-6 3-11-1S0 5 5 3Z"/></svg></button>
        <button type="button" data-start-call="video" data-call-name="<%: Selected.Name %>" data-call-role="<%: Selected.Role %>" aria-label="Start video call preview"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="6" width="12" height="12" rx="3"/><path d="m15 10 6-4v12l-6-4"/></svg></button>
      </div>
    </header>
    <div class="sc-messages bubbles" id="conversation-messages" role="log" aria-label="Messages with <%: Selected.Name %>" aria-live="polite" tabindex="0">
      <div class="sc-thread"><%= Thread %></div>
    </div>
    <asp:Panel runat="server" CssClass="sc-composer" DefaultButton="SendButton">
      <div class="sc-composer-box">
        <label class="sc-sr-only" for="MessageBox">Message</label>
        <asp:TextBox ID="MessageBox" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="1" MaxLength="4000" aria-describedby="chat-send-help" />
        <skill:SubmitButton ID="SendButton" runat="server" CssClass="sc-send" CausesValidation="false" OnClick="Send_Click" aria-label="Send message" title="Send message"><%= SendIcon %></skill:SubmitButton>
      </div>
      <small id="chat-send-help">Enter to send &middot; Shift + Enter for a new line</small>
    </asp:Panel>
    <% } else { %>
    <div class="sc-empty"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 5h16v11H9l-5 4Z"/></svg><h2>No conversation yet</h2><p>Active contacts will appear on the left.</p></div>
    <% } %>
  </section>
</section>
</asp:Content>
