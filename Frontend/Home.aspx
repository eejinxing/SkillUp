<%@ Page Title="Learning, one step at a time" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="SKILL_UP.Frontend.HomePage" %>
<%-- The landing page. Its sections are static marketing content; topic cards link to matching published courses. --%>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="home-hero">
  <div class="home-hero-copy home-reveal">
    <a class="stellar-update" href="#discover">
      <span>Learn something new</span>
      Find your next course
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
    </a>
    <h1>
      <span>Learning,</span>
      <span>one step at a time</span>
    </h1>
    <p>Your next skill starts here. Learn, practise and grow.</p>
    <div class="home-actions">
      <a class="home-button stellar-demo" href="#discover">
        Explore courses
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </a>
      <a class="home-button stellar-signup" href="#register">
        Start learning
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </a>
    </div>
  </div>
  <div class="stellar-stage">
    <figure class="stellar-preview stellar-preview--learner" data-ui="learner-dashboard-preview">
      <picture>
        <img class="learner-dashboard-shot" src="assets/public-dashboard/learner-desktop.webp" alt="Skill UP learner dashboard: welcome with next activity, level ring, activity progress, XP activity chart, announcements and course cards" width="1440" height="896" fetchpriority="high">
      </picture>
      <figcaption>Our Learner Dashboard · sample account</figcaption>
    </figure>
  </div>
</section>
<section class="home-section home-learn" id="discover">
  <header class="home-section-head home-reveal">
    <h2>What You’ll Learn</h2>
    <a href="<%= Me == null ? "#register" : "Catalog" %>">
      Explore all courses
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
    </a>
  </header>
  <div class="home-topic-grid">
    <a class="home-topic home-reveal" href="<%= TopicUrl("computer") %>" aria-label="Explore Computer basics courses">
      <h3>Computer basics</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="m58 43 82 46v88l-82-46Z" fill="#101214" stroke="#e7e9eb" stroke-width="4"></path><path d="m58 43 8-5 82 46-8 5Z" fill="#fafafa"></path><path d="m140 89 8-5v88l-8 5Z" fill="#aeb3b7"></path><path d="m88 151 25 14v16l-25-14Z" fill="#777e83"></path><path d="m72 169 44 25 20-11-44-25Z" fill="#17191b" stroke="#7a8186" stroke-width="2"></path><path d="m91 83 16 9v19l-8 8-8-17Z" fill="#e9ecee"></path><path d="m44 157 30 17-11 7-30-17Z" fill="#101214" stroke="#92999e" stroke-width="2"></path><path d="m142 184 20 11c10 6 23-1 13-8l-20-11c-10-6-23 2-13 8Z" fill="#16191b" stroke="#91999e" stroke-width="2"></path></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
    <a class="home-topic home-reveal" href="<%= TopicUrl("programming") %>" aria-label="Explore Programming courses">
      <h3>Programming</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="m76 68 39-23 41 24-40 24Z" fill="#111416" stroke="#adb4b8" stroke-width="3"></path><path d="m76 68 40 25v78l-40-24Z" fill="#f2f4f5"></path><path d="m116 93 40-24v78l-40 24Z" fill="#b7bfc4"></path><path d="m39 91 18-11 18 11-18 11Z" fill="#e8ebed"></path><path d="m39 91 18 11v42l-18-11Z" fill="#70797e"></path><path d="m57 102 18-11v42l-18 11Z" fill="#3d454a"></path><path d="m166 130 17-10 18 10-18 11Z" fill="#f0f2f4"></path><path d="m166 130 17 11v43l-17-10Z" fill="#888f94"></path><path d="m183 141 18-11v43l-18 11Z" fill="#363d42"></path><path d="m94 113-9 2 9 12m37-5 10 12-10 2" fill="none" stroke="#777f84" stroke-width="3"></path></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
    <a class="home-topic home-reveal" href="<%= TopicUrl("database") %>" aria-label="Explore Databases courses">
      <h3>Databases</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="M75 73v86c0 26 85 26 85 0V73" fill="#dddfe1" stroke="#a3abb0" stroke-width="2"></path><ellipse cx="117.5" cy="73" rx="42.5" ry="24" fill="#0c0e10" stroke="#e6e9eb" stroke-width="3"></ellipse><path d="M75 105c0 24 85 24 85 0m-85 29c0 24 85 24 85 0" fill="none" stroke="#8c959b" stroke-width="2"></path><ellipse cx="58" cy="165" rx="27" ry="15" fill="#111416" stroke="#8e969c" stroke-width="2"></ellipse><path d="M31 165v10c0 19 54 19 54 0v-10" fill="none" stroke="#737e85" stroke-width="2"></path><path d="m114 108 16 9v23l-16-9Z" fill="#171b1e"></path><path d="m117 113 10 6m-10 3 10 6" stroke="#c2c8cc" stroke-width="2"></path></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
    <a class="home-topic home-reveal" href="<%= TopicUrl("web") %>" aria-label="Explore Web development courses">
      <h3>Web development</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="m71 71 47-27 49 28-49 28Z" fill="#101315" stroke="#aeb7bd" stroke-width="3"></path><path d="m71 71 47 29v69l-47-27Z" fill="#f5f6f7"></path><path d="m118 100 49-28v69l-49 28Z" fill="#a9b3ba"></path><path d="m108 68-9 5 11 6m20-4 8-5-10-6m-13 0 8 15" fill="none" stroke="#e6eaec" stroke-width="3"></path><path d="m36 150 32-19 30 17-32 19Z" fill="#171a1d" stroke="#828c93" stroke-width="2"></path><path d="m154 162 35-21 19 11-35 21Z" fill="#101416" stroke="#8c969d" stroke-width="2"></path><path d="m164 173 35-21m-26 27 35-21" stroke="#5a646c" stroke-width="3"></path></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
    <a class="home-topic home-reveal" href="<%= TopicUrl("networks") %>" aria-label="Explore Networks courses">
      <h3>Networks</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="m58 43 82 46v88l-82-46Z" fill="#101214" stroke="#e7e9eb" stroke-width="4"></path><path d="m58 43 8-5 82 46-8 5Z" fill="#fafafa"></path><path d="m140 89 8-5v88l-8 5Z" fill="#aeb3b7"></path><path d="m88 151 25 14v16l-25-14Z" fill="#777e83"></path><path d="m72 169 44 25 20-11-44-25Z" fill="#17191b" stroke="#7a8186" stroke-width="2"></path><path d="m94 102 11 7m-5-15v23M54 145l-23 14 29 17m85-22 36 21-24 14M112 183v16" fill="none" stroke="#b5bdc2" stroke-width="2"></path><path d="m25 156 11-7 13 7-12 7Zm24 25 12-7 13 7-12 7Zm95 13 12-7 13 7-12 7Z" fill="#13181b" stroke="#c4cbd0" stroke-width="2"></path></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
    <a class="home-topic home-reveal" href="<%= TopicUrl("security") %>" aria-label="Explore Cybersecurity courses">
      <h3>Cybersecurity</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="m58 43 82 46v88l-82-46Z" fill="#101214" stroke="#e7e9eb" stroke-width="4"></path><path d="m58 43 8-5 82 46-8 5Z" fill="#fafafa"></path><path d="m140 89 8-5v88l-8 5Z" fill="#aeb3b7"></path><path d="m88 151 25 14v16l-25-14Z" fill="#777e83"></path><path d="m72 169 44 25 20-11-44-25Z" fill="#17191b" stroke="#7a8186" stroke-width="2"></path><path d="m88 80 22 12v27l-11 12-11-25Z" fill="#e2e7e9"></path><path d="m94 100 5 8 7-6" fill="none" stroke="#12181b" stroke-width="3"></path><path d="m40 166 24 13 16-9m61 15 19 11 22-13" fill="none" stroke="#89969e" stroke-width="2"></path><circle cx="160" cy="155" r="18" fill="#111719" stroke="#a7b2b9" stroke-width="2"></circle><path d="m154 155 5 5 8-11" fill="none" stroke="#e8edef" stroke-width="3"></path></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
    <a class="home-topic home-reveal" href="<%= TopicUrl("data") %>" aria-label="Explore Data science courses">
      <h3>Data science</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="m58 43 82 46v88l-82-46Z" fill="#101214" stroke="#e7e9eb" stroke-width="4"></path><path d="m58 43 8-5 82 46-8 5Z" fill="#fafafa"></path><path d="m140 89 8-5v88l-8 5Z" fill="#aeb3b7"></path><path d="m88 151 25 14v16l-25-14Z" fill="#777e83"></path><path d="m72 169 44 25 20-11-44-25Z" fill="#17191b" stroke="#7a8186" stroke-width="2"></path><path d="m79 105 10 6v20l-10-6Zm17-2 10 6v31l-10-6Zm17-10 10 6v50l-10-6Z" fill="#e4e9ed"></path><path d="m30 143 20-12v35l-20 12Z" fill="#111619" stroke="#9ba7af" stroke-width="2"></path><path d="m152 171 30 17-28 17-30-18Z" fill="#161c20" stroke="#77858e" stroke-width="2"></path><path d="m144 187 10 6 17-10" fill="none" stroke="#d8e0e5" stroke-width="2"></path></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
    <a class="home-topic home-reveal" href="<%= TopicUrl("ai") %>" aria-label="Explore AI & automation courses">
      <h3>AI & automation</h3>
      <svg class="home-topic-art" viewBox="0 0 240 240" fill="none" aria-hidden="true"><path d="m56 115 59-34 61 35-60 35Z" fill="#101518" stroke="#9faab1" stroke-width="3"></path><path d="m56 115 60 36v12l-60-35Z" fill="#dbe3e8"></path><path d="m116 151 60-35v12l-60 35Z" fill="#93a2ac"></path><path d="m90 112 26-15 29 17-28 16Z" fill="#1c252c" stroke="#e5ebef" stroke-width="2"></path><path d="m98 114 19-10 18 10-18 10Z" fill="#c7d5de"></path><path d="m84 100-14-8m24 1-2-15m42 18 16-9m-4 32 22 1m-44 23 2 14m-36-22-13 9" stroke="#a5b1b9" stroke-width="2"></path><circle cx="70" cy="92" r="6" fill="#1b252c" stroke="#8998a3"></circle><circle cx="151" cy="87" r="6" fill="#1b252c" stroke="#8998a3"></circle><circle cx="126" cy="158" r="6" fill="#1b252c" stroke="#8998a3"></circle></svg>
      <span class="home-topic-arrow" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </span>
    </a>
  </div>
</section>
<% if (InstructorCount > 0) { %>
<section class="home-section home-team" id="visitor-instructors">
  <header class="home-reveal">
    <h2>
      Learn from the people
      <br>
      <em>behind your next skill.</em>
    </h2>
    <p>Guidance at every step.</p>
  </header>
  <%-- Active instructors with a published course (Home.aspx.cs, BuildInstructors). With fewer than four, the cards sit centred and the arrows hide. --%>
  <div class="instructor-carousel<%= InstructorCount < 4 ? " is-few" : "" %>">
    <button type="button" class="instructor-arrow" data-instructor-step="-1" aria-label="Previous instructor">‹</button>
    <div class="instructor-track" aria-label="Instructor profiles"><%= InstructorCards %></div>
    <button type="button" class="instructor-arrow" data-instructor-step="1" aria-label="Next instructor">›</button>
  </div>
  <div class="instructor-position" aria-hidden="true">
    <i></i>
  </div>
  <p class="instructor-sr">Portraits are AI-generated illustrations of fictional instructors.</p>
</section>
<% } %>
<section class="home-section home-stories" id="visitor-stories">
  <header class="home-section-head home-reveal">
    <div>
      <h2>Our Learners</h2>
      <p>Clear lessons. Real practice. A next step you can follow.</p>
    </div>
    <small><%= StoryNote %></small>
  </header>
  <%-- Public feedback from the database (Home.aspx.cs, BuildStories). --%>
  <%= StoryGrid %>
</section>
<section class="home-section home-faq" id="visitor-faq">
  <header class="home-reveal">
    <h2>
      Questions You
      <br>
      Might Have
    </h2>
    <p>Find answers about courses, practical activities, progress and the support available along the way.</p>
  </header>
  <div class="home-accordion">
    <details class="home-reveal" open="">
      <summary>
        Can I start learning without any prior experience?
        <span aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
        </span>
      </summary>
      <p>Absolutely. Choose a beginner course and follow its activities in order. Clear explanations and guided practice help you build confidence, one step at a time.</p>
    </details>
    <details class="home-reveal">
      <summary>
        What can I learn on Skill UP?
        <span aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
        </span>
      </summary>
      <p>Explore computer fundamentals, programming, databases, web development, networks, cybersecurity, data science and AI.</p>
    </details>
    <details class="home-reveal">
      <summary>
        How long does a typical course take?
        <span aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
        </span>
      </summary>
      <p>Each course shows an estimated duration and deadline. You can learn at your own pace and resume where you left off.</p>
    </details>
    <details class="home-reveal">
      <summary>
        How do I track my progress and earn a certificate?
        <span aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
        </span>
      </summary>
      <p>Complete the course activities and pass its assessments. Your progress updates automatically, and a certificate becomes available when the course is complete.</p>
    </details>
    <details class="home-reveal">
      <summary>
        Can I get help if I have a question?
        <span aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
        </span>
      </summary>
      <p>Message your instructor, or use the learning companion inside an activity for guidance.</p>
    </details>
  </div>
  <skill:EnquiryForm runat="server" HeadingTag="h3" />
</section>
</asp:Content>
