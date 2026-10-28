<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EnquiryForm.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.EnquiryForm" %>
<section class="home-enquiry" id="home-enquiry" aria-labelledby="home-enquiry-title">
  <div class="home-enquiry-intro">
    <span class="home-enquiry-eyebrow">Contact</span>
    <%= "<" + HeadingTag + " id=\"home-enquiry-title\">" %>Ready for your<br>next step?<%= "</" + HeadingTag + ">" %>
    <p>Tell us what&rsquo;s on your mind.<br>We&rsquo;ll help you find a way forward.</p>
    <ol class="enq-steps" aria-label="What happens after you send">
      <li><b>01</b><span>Send your message</span></li>
      <li><b>02</b><span>Get an enquiry ID and access code</span></li>
      <li><b>03</b><span>View, edit or delete it in your portal</span></li>
    </ol>
    <a class="home-enquiry-portal" href="EnquiryPortal">
      <span class="home-enquiry-portal-icon"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 8v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h6l3 5Z"></path><path d="M8 12h8m-8 4h5"></path></svg></span>
      <span class="home-enquiry-portal-copy"><strong>My enquiry portal</strong><em>View, edit or delete your messages.</em></span>
      <span class="home-enquiry-portal-arrow" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 17 17 7M9 7h8v8"></path></svg></span>
    </a>
  </div>
  <div class="home-enquiry-right">
    <asp:MultiView ID="Steps" runat="server" ActiveViewIndex="0">
      <asp:View runat="server">
        <p class="home-enquiry-lead">A question about courses, your account or a practical activity? Leave us a message below.</p>
        <asp:Panel runat="server" CssClass="home-enquiry-form" DefaultButton="SendButton">
          <fieldset class="home-enquiry-topics">
            <legend>What can we help with?</legend>
            <div>
              <label><asp:RadioButton ID="TopicCourse" runat="server" GroupName="topic" /><span><strong>Choosing a course</strong><small>Levels, paths and fit</small></span></label>
              <label><asp:RadioButton ID="TopicActivity" runat="server" GroupName="topic" /><span><strong>A practical activity</strong><small>Labs, quizzes and hints</small></span></label>
              <label><asp:RadioButton ID="TopicAccount" runat="server" GroupName="topic" /><span><strong>My account</strong><small>Sign-in, profile, progress</small></span></label>
              <label><asp:RadioButton ID="TopicOther" runat="server" GroupName="topic" /><span><strong>Something else</strong><small>Anything we missed</small></span></label>
            </div>
          </fieldset>
          <div class="home-enquiry-pair">
            <label class="home-enquiry-field"><span>Your name</span>
              <asp:TextBox ID="NameBox" runat="server" autocomplete="name" placeholder="Alex Morgan" MaxLength="100" />
              <asp:RequiredFieldValidator runat="server" ControlToValidate="NameBox" ValidationGroup="enquiry" ErrorMessage="Enter your name." Display="Dynamic" CssClass="field-error" />
            </label>
            <label class="home-enquiry-field"><span>Email address</span>
              <asp:TextBox ID="EmailBox" runat="server" TextMode="Email" autocomplete="email" placeholder="you@example.com" MaxLength="254" />
              <asp:RequiredFieldValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="enquiry" ErrorMessage="Enter your email." Display="Dynamic" CssClass="field-error" />
              <asp:RegularExpressionValidator runat="server" ControlToValidate="EmailBox" ValidationGroup="enquiry" ValidationExpression="^\S+@\S+\.\S+$" ErrorMessage="Use an address like name@example.com." Display="Dynamic" CssClass="field-error" />
            </label>
          </div>
          <label class="home-enquiry-field"><span>Subject</span>
            <asp:TextBox ID="SubjectBox" runat="server" placeholder="Pick a topic above or write your own" MaxLength="160" />
          </label>
          <label class="home-enquiry-field"><span>Your message <small class="home-enquiry-count">Up to 3000 characters</small></span>
            <asp:TextBox ID="MessageBox" runat="server" TextMode="MultiLine" Rows="3" MaxLength="3000" placeholder="What did you try, and where did you get stuck?" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="MessageBox" ValidationGroup="enquiry" ErrorMessage="Write a few words about your question." Display="Dynamic" CssClass="field-error" />
          </label>
          <p class="form-error" role="alert"><asp:Literal ID="FormError" runat="server" /></p>
          <div class="home-enquiry-actions">
            <div class="home-enquiry-action-copy">
              <p id="home-enquiry-note">Manage your message in the enquiry portal.</p>
              <small class="home-enquiry-ready">All fields are required.</small>
            </div>
            <skill:SubmitButton ID="SendButton" runat="server" CssClass="home-enquiry-submit" ValidationGroup="enquiry" OnClick="Send_Click"><span>Send enquiry</span><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg></skill:SubmitButton>
          </div>
        </asp:Panel>
      </asp:View>
      <asp:View runat="server">
        <div class="home-enquiry-saved" role="status">
          <h3>Enquiry saved</h3>
          <p>View, edit or delete this message in your enquiry portal. Keep the ID and access code to open it on another device.</p>
          <pre>Enquiry ID: <asp:Literal ID="SavedId" runat="server" />
Access code: <asp:Literal ID="SavedToken" runat="server" /></pre>
          <div class="actions"><a class="button primary" href="EnquiryPortal">Open enquiry portal &rarr;</a></div>
        </div>
      </asp:View>
    </asp:MultiView>
  </div>
</section>
