<%@ Page Title="Profile" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Profile.aspx.cs" Inherits="SKILL_UP.Frontend.ProfilePage" %>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="pf-page" aria-labelledby="pf-title">
  <aside class="pf-side"><div class="pf-side-card">
    <div class="pf-avatar"><%= Ui.Avatar(Me, "profile-avatar") %><span class="pf-status" title="<%: Me.Status %>"></span></div>
    <h1 id="pf-title"><%: Me.Name %></h1>
    <p class="pf-handle">@<%: Me.Username %><span class="pf-role"><%: Me.Role %></span></p>
    <nav class="pf-nav" aria-label="Profile sections">
      <a href="#pf-overview" class="is-active"><%= Icon("overview") %>Overview</a>
      <a href="#pf-activity"><%= Icon("activity") %>Activity<em><%= HeatTotal %></em></a>
      <a href="#pf-info"><%= Icon("info") %>Information</a>
      <a href="#pf-account"><%= Icon("account") %>Account</a>
    </nav>
    <ul class="pf-contact">
      <li><%= Icon("mail") %><%: Me.Email %></li>
      <% if (!string.IsNullOrEmpty(Me.Location)) { %><li><%= Icon("pin") %><%: Me.Location %></li><% } %>
      <li><%= Icon("badge") %><%: Me.Role %> &middot; <%: Me.Status %></li>
    </ul>
    <p class="pf-counts"><%= Icon("book") %><b><%= CourseCount %></b> courses &middot; <b><%= Certificates %></b> certificates</p>
    <div class="pf-side-actions">
      <details class="pf-image-form">
        <summary class="button ghost pf-change-image">Change profile image</summary>
        <asp:FileUpload ID="AvatarFile" runat="server" accept="image/png,image/jpeg,image/webp,image/gif" />
        <skill:SubmitButton runat="server" CssClass="primary" Text="Upload image" CausesValidation="false" OnClick="Avatar_Click" />
        <% if (!string.IsNullOrEmpty(Me.Avatar)) { %><skill:SubmitButton runat="server" CssClass="ghost" Text="Remove image" CausesValidation="false" OnClick="RemoveAvatar_Click" /><% } %>
      </details>
      <a class="button pf-prefs" href="#pf-account">Account security</a>
      <asp:PlaceHolder ID="DemoSwitch" runat="server">
        <details class="pf-demo-switch">
          <summary class="button pf-prefs">Switch demo role</summary>
          <p>Switch accounts to see how a change travels between roles.</p>
          <div class="demo-grid">
            <skill:SubmitButton runat="server" Text="Learner" CommandArgument="Learner" OnCommand="Demo_Command" CausesValidation="false" />
            <skill:SubmitButton runat="server" Text="Instructor" CommandArgument="Instructor" OnCommand="Demo_Command" CausesValidation="false" />
          </div>
        </details>
      </asp:PlaceHolder>
      <skill:SubmitButton runat="server" CssClass="button pf-prefs pf-signout" Text="Sign out" CausesValidation="false" OnClick="SignOut_Click" />
    </div>
  </div></aside>
  <div class="pf-main">
    <div class="pf-hero" id="pf-overview">
      <figure class="pf-cover" aria-hidden="true"><span class="pf-cover-glow"></span><span class="pf-cover-initials"><%: Ui.Initials(Me) %></span></figure>
      <article class="pf-readme">
        <small><%: Me.Username %> / README.md</small>
        <h2>Hi, I'm <%: Me.Name.Split(' ')[0] %>!</h2>
        <p><%: string.IsNullOrWhiteSpace(Me.Bio) ? "Tell other learners a little about yourself in the Information section below." : Me.Bio %></p>
        <p><%: RoleLine %> You can reach me at <a href="mailto:<%: Me.Email %>"><%: Me.Email %></a><%: string.IsNullOrEmpty(Me.Location) ? "" : " from " + Me.Location %>.</p>
      </article>
    </div>
    <div class="pf-grid">
      <section class="pf-card pf-achievements" aria-labelledby="pf-ach-title">
        <header><h2 id="pf-ach-title">Achievements</h2><span class="pf-pill"><%= Earned %>/<%= BadgeCount %></span></header>
        <div class="pf-badges">
          <asp:Repeater ID="Badges" runat="server"><ItemTemplate><span class="pf-badge <%# (bool)Eval("Earned") ? "is-earned" : "" %>" style="--b:<%# Eval("Colour") %>;--n:<%# Container.ItemIndex %>" title="<%#: Eval("Title") %> · <%#: Eval("Detail") %><%# (bool)Eval("Earned") ? "" : " (locked)" %>"><img src="assets/badges/<%# Eval("Sticker") %>.png" alt="" loading="lazy"><span class="sr-only"><%#: Eval("Title") %><%# (bool)Eval("Earned") ? "" : " (locked)" %></span></span></ItemTemplate></asp:Repeater>
        </div>
        <p class="pf-card-foot"><span class="pf-pill is-soft">Beta</span>Earn badges as you learn and teach</p>
      </section>
      <section class="pf-card pf-contrib" id="pf-activity" aria-labelledby="pf-act-title">
        <header><h2 id="pf-act-title"><%= HeatTotal.ToString("N0") %> <%= Me.Role == "Learner" ? "learning sessions" : "activities" %> in the last year</h2><span class="pf-pill"><%= DateTime.Now.Year %></span></header>
        <div class="pf-heat-wrap"><div class="pf-months" style="--w:<%= HeatWeeks %>"><%= HeatMonths %></div><div class="pf-heat" style="--w:<%= HeatWeeks %>" role="img" aria-label="<%= HeatTotal %> activities over the last year"><%= HeatCells %></div></div>
        <p class="pf-legend">Less<i data-l="0"></i><i data-l="1"></i><i data-l="2"></i><i data-l="3"></i><i data-l="4"></i>More</p>
      </section>
    </div>
    <section class="pf-card pf-courses" aria-labelledby="pf-courses-title">
      <header><h2 id="pf-courses-title"><%= Me.Role == "Learner" ? "My courses" : Me.Role == "Admin" ? "Popular courses" : "Courses" %></h2><a class="pf-link" href="<%= Me.Role == "Learner" ? "MyCourses" : Me.Role == "Admin" ? "Catalog" : "Courses" %>">View all</a></header>
      <div class="pf-course-grid">
        <asp:Repeater ID="CourseCards" runat="server">
          <ItemTemplate><a class="pf-course" href="Course?id=<%# Eval("Id") %>"><header><%= Icon("book") %><strong><%#: Eval("Title") %></strong><span class="pf-tag" data-tag="<%# Eval("TagKey") %>"><%#: Eval("Tag") %></span></header><p><%#: Eval("Description") %></p><footer><span><%= Icon("star") %><%# Eval("Rating") %></span><span><%= Icon("users") %><%# Eval("Learners") %></span><span><%# Eval("Activities") %> activities</span></footer></a></ItemTemplate>
        </asp:Repeater>
        <asp:PlaceHolder ID="NoCourses" runat="server"><p class="pf-empty">No courses yet.</p></asp:PlaceHolder>
      </div>
    </section>
    <details class="pf-card pf-section" id="pf-info" open>
      <summary><h2>Information</h2><%= Icon("chevron", "pf-chev") %></summary>
      <asp:Panel runat="server" DefaultButton="SaveProfileButton">
        <div class="pf-rows">
          <div class="pf-row"><span class="pf-row-label"><%= Icon("user") %>Full name</span><div class="pf-row-value"><asp:TextBox ID="NameBox" runat="server" MaxLength="100" aria-label="Full name" /><asp:RequiredFieldValidator runat="server" ControlToValidate="NameBox" ValidationGroup="profile" ErrorMessage="Enter your name." Display="Dynamic" CssClass="field-error" /></div><label class="pf-edit" for="<%= NameBox.ClientID %>" aria-label="Edit Full name"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("at") %>Username</span><div class="pf-row-value"><asp:TextBox ID="UsernameBox" runat="server" MaxLength="50" aria-label="Username" /><asp:RequiredFieldValidator runat="server" ControlToValidate="UsernameBox" ValidationGroup="profile" ErrorMessage="Choose a username." Display="Dynamic" CssClass="field-error" /></div><label class="pf-edit" for="<%= UsernameBox.ClientID %>" aria-label="Edit Username"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("mail") %>Email</span><div class="pf-row-value"><asp:TextBox ID="EmailBox" runat="server" TextMode="Email" MaxLength="255" aria-label="Email" /><asp:RegularExpressionValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="profile" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Enter a valid email address." Display="Dynamic" CssClass="field-error" /></div><label class="pf-edit" for="<%= EmailBox.ClientID %>" aria-label="Edit Email"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("pin") %>Location</span><div class="pf-row-value"><asp:TextBox ID="LocationBox" runat="server" MaxLength="100" aria-label="Location" /></div><label class="pf-edit" for="<%= LocationBox.ClientID %>" aria-label="Edit Location"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("text") %>About you</span><div class="pf-row-value"><asp:TextBox ID="BioBox" runat="server" TextMode="MultiLine" Rows="3" MaxLength="1000" aria-label="About you" /></div><label class="pf-edit" for="<%= BioBox.ClientID %>" aria-label="Edit About you"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("moon") %>Appearance</span><div class="pf-row-value"><asp:DropDownList ID="ThemeList" runat="server" aria-label="Appearance"><asp:ListItem Value="dark">Dark</asp:ListItem><asp:ListItem Value="light">Light</asp:ListItem></asp:DropDownList></div><label class="pf-edit" for="<%= ThemeList.ClientID %>" aria-label="Edit Appearance"><%= Icon("pencil") %></label></div>
        </div>
        <div class="actions"><skill:SubmitButton ID="SaveProfileButton" runat="server" CssClass="primary" Text="Save profile" ValidationGroup="profile" OnClick="SaveProfile_Click" /></div>
      </asp:Panel>
    </details>
    <details class="pf-card pf-section" id="pf-account" open>
      <summary><h2>Account</h2><%= Icon("chevron", "pf-chev") %></summary>
      <div class="pf-rows">
        <div class="pf-row pf-row-action"><span class="pf-row-label"><%= Icon("chat") %>Feedback</span><div class="pf-row-value"><span>Tell us about your learning experience</span></div><a class="button ghost pf-row-btn" href="#pf-feedback">Give feedback</a></div>
      </div>
      <details class="inline-form pf-feedback" id="pf-feedback">
        <summary>Share your feedback</summary>
        <asp:Panel runat="server" DefaultButton="FeedbackButton">
          <label>Rating <asp:DropDownList ID="FeedbackRating" runat="server"><asp:ListItem>5</asp:ListItem><asp:ListItem>4</asp:ListItem><asp:ListItem>3</asp:ListItem><asp:ListItem>2</asp:ListItem><asp:ListItem>1</asp:ListItem></asp:DropDownList></label>
          <label>What should we know?
            <asp:TextBox ID="FeedbackText" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="FeedbackText" ValidationGroup="feedback" ErrorMessage="Write a few words." Display="Dynamic" CssClass="field-error" />
          </label>
          <div class="actions"><skill:SubmitButton ID="FeedbackButton" runat="server" CssClass="primary" Text="Submit feedback" ValidationGroup="feedback" OnClick="Feedback_Click" /></div>
        </asp:Panel>
      </details>
    </details>
  </div>
</section>
</asp:Content>
