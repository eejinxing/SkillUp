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
      <% if (Me.Role == "Learner") { %><a href="#pf-certificates"><%= Icon("badge") %>My Certificates<em><%= Certificates %></em></a><% } %>
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
    <asp:PlaceHolder ID="CertSection" runat="server">
    <section class="pf-card pf-certs" id="pf-certificates" aria-labelledby="pf-certs-title">
      <header><h2 id="pf-certs-title">My Certificates</h2><a class="pf-link" href="Certificates">View all</a></header>
      <div class="pf-course-grid">
        <asp:Repeater ID="CertCards" runat="server">
          <ItemTemplate><a class="pf-course pf-cert" href="Certificates?id=<%# Eval("Id") %>"><header><%= Icon("badge") %><strong><%#: Eval("Title") %></strong></header><footer><span>Completed <time data-local-date="<%# Eval("Iso") %>"><%#: Eval("Date") %></time></span><span><%#: Eval("Hours") %></span><span>View certificate &rarr;</span></footer></a></ItemTemplate>
        </asp:Repeater>
        <asp:PlaceHolder ID="NoCerts" runat="server"><p class="pf-empty">No certificates yet. Finish every activity in a course to earn one.</p></asp:PlaceHolder>
      </div>
    </section>
    </asp:PlaceHolder>
    <details class="pf-card pf-section" id="pf-info" open>
      <summary><h2>Information</h2><%= Icon("chevron", "pf-chev") %></summary>
      <asp:Panel runat="server" DefaultButton="SaveProfileButton">
        <div class="pf-rows">
          <div class="pf-row"><span class="pf-row-label"><%= Icon("user") %>Full name</span><div class="pf-row-value"><asp:TextBox ID="NameBox" runat="server" MaxLength="100" aria-label="Full name" /><asp:RequiredFieldValidator runat="server" ControlToValidate="NameBox" ValidationGroup="profile" ErrorMessage="Enter your name." Display="Dynamic" CssClass="field-error" /></div><label class="pf-edit" for="<%= NameBox.ClientID %>" aria-label="Edit Full name"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("at") %>Username</span><div class="pf-row-value"><asp:TextBox ID="UsernameBox" runat="server" MaxLength="50" aria-label="Username" /><asp:RequiredFieldValidator runat="server" ControlToValidate="UsernameBox" ValidationGroup="profile" ErrorMessage="Choose a username." Display="Dynamic" CssClass="field-error" /></div><label class="pf-edit" for="<%= UsernameBox.ClientID %>" aria-label="Edit Username"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("mail") %>Email</span><div class="pf-row-value"><asp:TextBox ID="EmailBox" runat="server" TextMode="Email" MaxLength="255" aria-label="Email" /><asp:RegularExpressionValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="profile" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Enter a valid email address." Display="Dynamic" CssClass="field-error" /></div><label class="pf-edit" for="<%= EmailBox.ClientID %>" aria-label="Edit Email"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("pin") %>Location</span><div class="pf-row-value"><asp:TextBox ID="LocationBox" runat="server" MaxLength="100" aria-label="Location" /></div><label class="pf-edit" for="<%= LocationBox.ClientID %>" aria-label="Edit Location"><%= Icon("pencil") %></label></div>
          <div class="pf-row"><span class="pf-row-label"><%= Icon("text") %>About you</span><div class="pf-row-value"><asp:TextBox ID="BioBox" runat="server" TextMode="MultiLine" Rows="3" MaxLength="1000" aria-label="About you" /></div><label class="pf-edit" for="<%= BioBox.ClientID %>" aria-label="Edit About you"><%= Icon("pencil") %></label></div>
        </div>
        <div class="actions"><skill:SubmitButton ID="SaveProfileButton" runat="server" CssClass="primary" Text="Save profile" ValidationGroup="profile" OnClick="SaveProfile_Click" /></div>
      </asp:Panel>
    </details>
    <details class="pf-card pf-section" id="pf-account" open>
      <summary><h2>Account</h2><%= Icon("chevron", "pf-chev") %></summary>
      <div class="pf-rows">
        <div class="pf-row pf-row-action"><span class="pf-row-label"><%= Icon("key") %>Password</span><div class="pf-row-value"><span>Change the password you use to log in</span></div><a class="button ghost pf-row-btn" href="#pf-password">Change password</a></div>
      </div>
      <details class="inline-form pf-feedback" id="pf-password"<%= PasswordOpen ? " open" : "" %>>
        <summary>Change your password</summary>
        <asp:Panel runat="server" CssClass="pf-password-form" DefaultButton="PasswordButton">
          <label>Current password <b class="req" title="Required" aria-hidden="true">*</b>
            <span class="auth-field"><asp:TextBox ID="PwCurrent" runat="server" ClientIDMode="Static" TextMode="Password" autocomplete="current-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="PwCurrent" ValidationGroup="password" ErrorMessage="Enter your current password." Display="Dynamic" CssClass="field-error" />
          </label>
          <label>New password <b class="req" title="Required" aria-hidden="true">*</b>
            <span class="auth-field"><asp:TextBox ID="PwNew" runat="server" ClientIDMode="Static" TextMode="Password" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="PwNew" ValidationGroup="password" ErrorMessage="Choose a new password." Display="Dynamic" CssClass="field-error" />
            <asp:CustomValidator runat="server" ControlToValidate="PwNew" ValidationGroup="password" ClientValidationFunction="skillUpPasswordCheck" OnServerValidate="Password_ServerValidate" ErrorMessage="Use 8 to 128 characters." Display="Dynamic" CssClass="field-error" />
          </label>
          <div class="pw-help" data-pw-for="PwNew">
            <div class="pw-meter" role="progressbar" aria-label="Password strength" aria-valuemin="0" aria-valuemax="4" aria-valuenow="0"><i></i><i></i><i></i><i></i></div>
            <p class="pw-label" aria-live="polite">Password strength: <b>not entered yet</b></p>
            <ul class="pw-rules">
              <li data-rule="length">8 to 128 characters</li>
              <li data-rule="capital">A capital letter (A&ndash;Z)</li>
              <li data-rule="number">A number (0&ndash;9)</li>
              <li data-rule="special">A special character (! @ # $ %)</li>
              <li data-rule="different">Different from your current password</li>
            </ul>
          </div>
          <label>Confirm new password <b class="req" title="Required" aria-hidden="true">*</b>
            <span class="auth-field"><asp:TextBox ID="PwConfirm" runat="server" ClientIDMode="Static" TextMode="Password" autocomplete="new-password" /><button type="button" class="auth-eye" data-auth-eye="" aria-label="Show password"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg></button></span>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="PwConfirm" ValidationGroup="password" ErrorMessage="Type the new password again." Display="Dynamic" CssClass="field-error" />
            <asp:CompareValidator runat="server" ControlToValidate="PwConfirm" ControlToCompare="PwNew" ValidationGroup="password" ErrorMessage="Passwords must match." Display="Dynamic" CssClass="field-error" />
          </label>
          <p class="form-error" role="alert"><asp:Literal ID="PasswordError" runat="server" /></p>
          <div class="actions"><skill:SubmitButton ID="PasswordButton" runat="server" CssClass="primary" Text="Update password" ValidationGroup="password" OnClick="Password_Click" /></div>
        </asp:Panel>
      </details>
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
