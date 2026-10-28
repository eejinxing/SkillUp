<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="SiteFooter.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.SiteFooter" %>
<footer class="home-footer tm-footer">
  <svg class="tm-edge" viewBox="0 0 1440 48" preserveAspectRatio="none" aria-hidden="true"><path d="M0 48V38C0 28 8 22 18 22H486C516 22 522 0 552 0H888C918 0 924 22 954 22H1422C1432 22 1440 28 1440 38V48Z"></path></svg>
  <section class="tm-subscribe" aria-labelledby="tm-subscribe-title">
    <div class="tm-subscribe-copy">
      <h2 id="tm-subscribe-title">
        <span>Your next skill</span>
        <span>starts here.</span>
      </h2>
      <p class="tm-subscribe-intro">Sign up to explore courses, practise and track your progress.</p>
      <asp:Panel runat="server" CssClass="tm-form" DefaultButton="SignUpButton">
  <div class="tm-field">
    <label class="sr-only" for="SignUpEmail">Email address</label>
    <asp:TextBox ID="SignUpEmail" runat="server" ClientIDMode="Static" TextMode="Email" autocomplete="email" placeholder="Your email address" />
    <skill:SubmitButton ID="SignUpButton" runat="server" CausesValidation="false" OnClick="SignUp_Click"><span>Sign up</span></skill:SubmitButton>
  </div>
  <p class="tm-signup-help">Already have an account? <a href="#login">Log in</a></p>
</asp:Panel>
    </div>
    <div class="tm-side">
      <h3 class="tm-side-title">What you’ll get</h3>
      <ul class="tm-perks">
        <li style="--i:0">
          <span class="tm-perk-icon">
            <svg data-icon="lucide:book-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 5v16m8.001-2A2 2 0 0 0 22 17V5a2 2 0 0 0-1.999-2L16 3.002A5 5 0 0 0 12 5a5 5 0 0 0-4-2H4a2 2 0 0 0-2 2v12a2 2 0 0 0 1.999 2H8a5 5 0 0 1 4 2a5 5 0 0 1 4-2z"></path></svg>
          </span>
          <span>
            <b>Find your next course</b>
            <small>Explore skills at your own pace.</small>
          </span>
        </li>
        <li style="--i:1">
          <span class="tm-perk-icon">
            <svg data-icon="lucide:chart-no-axes-combined" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 16v5m4-6.361V21m4-10.344V21m2-18l-8.646 8.646a.5.5 0 0 1-.708 0L9.354 8.354a.5.5 0 0 0-.707 0L2 15m2 3.463V21m4-6.344V21"></path></svg>
          </span>
          <span>
            <b>See your progress</b>
            <small>Pick up right where you left off.</small>
          </span>
        </li>
        <li style="--i:2">
          <span class="tm-perk-icon">
            <svg data-icon="lucide:flask-conical" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 2v6a2 2 0 0 0 .245.96l5.51 10.08A2 2 0 0 1 18 22H6a2 2 0 0 1-1.755-2.96l5.51-10.08A2 2 0 0 0 10 8V2M6.453 15h11.094M8.5 2h7"></path></svg>
          </span>
          <span>
            <b>Learn by doing</b>
            <small>Put new skills into practice with labs.</small>
          </span>
        </li>
      </ul>
      <div class="tm-reach">
        <a class="tm-mail" href="mailto:hello@skillup.demo">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2"></rect><path d="m3 7 9 6 9-6"></path></svg>
          <span>
            <small>Questions?</small>
            hello@skillup.demo
          </span>
        </a>
        <div class="tm-social" aria-label="Skill UP on social media">
          <a href="<%= SkillPage.HomeContact %>" aria-label="Facebook">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 8h3V4h-3a4 4 0 0 0-4 4v2H7v4h3v8h4v-8h3l1-4h-4V8Z"></path></svg>
          </a>
          <a href="<%= SkillPage.HomeContact %>" aria-label="X">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 4l16 16M20 4 4 20"></path></svg>
          </a>
          <a href="<%= SkillPage.HomeContact %>" aria-label="Instagram">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="4" y="4" width="16" height="16" rx="5"></rect><circle cx="12" cy="12" r="3.5"></circle><path d="M16.5 7.5h.01"></path></svg>
          </a>
          <a href="<%= SkillPage.HomeContact %>" aria-label="LinkedIn">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="4" y="4" width="16" height="16" rx="3"></rect><path d="M8 10v6M8 7.5h.01M12 16v-3.5a2 2 0 0 1 4 0V16M12 10v6"></path></svg>
          </a>
        </div>
      </div>
    </div>
  </section>
  <div class="home-footer-base">
    <a href="Home" class="has-skill-logo">
      <img class="skill-logo" src="assets/skill-logo.png" alt="Skill UP" width="930" height="420">
    </a>
    <small>© 2026 Skill UP · Learning, one step at a time. All rights reserved.</small>
    <nav aria-label="Legal">
      <a href="Privacy">Privacy information</a>
      <a href="Terms">Terms of use</a>
      <a href="<%= SkillPage.HomeContact %>">Help center</a>
      <a href="About">About us</a>
    </nav>
  </div>
  <div class="tm-wordmark" aria-hidden="true">
    <span class="tm-pixel tm-p1"></span>
    <span class="tm-pixel tm-p2"></span>
    <span class="tm-ch" style="--i:0">s</span>
    <span class="tm-ch" style="--i:1">k</span>
    <span class="tm-ch" style="--i:2">i</span>
    <span class="tm-ch" style="--i:3">l</span>
    <span class="tm-ch" style="--i:4">l</span>
    <span class="tm-gap"></span>
    <span class="tm-ch" style="--i:6">u</span>
    <span class="tm-ch" style="--i:7">p</span>
  </div>
</footer>
