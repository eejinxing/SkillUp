<%@ Page Title="Unlock Your Potential Future" Language="C#" MasterPageFile="~/Frontend/SkillUp.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="SKILL_UP.Frontend.HomePage" %>
<%-- The landing page. Its sections are static marketing content; topic cards link to matching published courses. --%>
<asp:Content ContentPlaceHolderID="PageContent" runat="server">
<section class="home-hero">
  <div class="home-hero-copy home-reveal">
    <a class="stellar-update" href="Catalog">
      <span>Learn something new</span>
      Find your next course
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
    </a>
    <h1>
      <span>Unlock Your</span>
      <span>Potential Future</span>
    </h1>
    <p>Your next skill starts here. Learn, practise and grow.</p>
    <div class="home-actions">
      <a class="home-button stellar-demo" href="Catalog">
        Explore courses
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"></path></svg>
      </a>
      <a class="home-button stellar-signup" href="Register">
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
    <a href="Catalog">
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
<section class="home-section home-team" id="visitor-instructors">
  <header class="home-reveal">
    <h2>
      Learn from the people
      <br>
      <em>behind your next skill.</em>
    </h2>
    <p>Guidance at every step.</p>
  </header>
  <div class="instructor-carousel">
    <button type="button" class="instructor-arrow" data-instructor-step="-1" aria-label="Previous instructor">‹</button>
    <div class="instructor-track" aria-label="Instructor profiles">
      <button type="button" class="instructor-card" aria-expanded="false" aria-label="About John Doe">
        <span class="instructor-photo" aria-hidden="true">
          <img src="assets/instructor-portraits/john.webp" alt="" loading="lazy" width="1024" height="1536">
        </span>
        <span class="instructor-identity">
          <strong>John Doe</strong>
          <small>Databases & development</small>
        </span>
        <span class="instructor-bio">
          <strong>John Doe</strong>
          <small>Databases & development</small>
          <span>Turn a new concept into a working query. Practise MongoDB, explore your data, and get help with the next activity.</span>
          <i aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"><path d="M7 17 17 7M9 7h8v8"></path></svg>
          </i>
        </span>
      </button>
      <button type="button" class="instructor-card" aria-expanded="false" aria-label="About Sarah Chen">
        <span class="instructor-photo" aria-hidden="true">
          <img src="assets/instructor-portraits/sarah.webp" alt="" loading="lazy" width="1024" height="1536">
        </span>
        <span class="instructor-identity">
          <strong>Sarah Chen</strong>
          <small>Design & creative tools</small>
        </span>
        <span class="instructor-bio">
          <strong>Sarah Chen</strong>
          <small>Design & creative tools</small>
          <span>Build your confidence through clear examples, guided practice and feedback you can apply to your next project.</span>
          <i aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"><path d="M7 17 17 7M9 7h8v8"></path></svg>
          </i>
        </span>
      </button>
      <button type="button" class="instructor-card" aria-expanded="false" aria-label="About Mark Rivera">
        <span class="instructor-photo" aria-hidden="true">
          <img src="assets/instructor-portraits/mark.webp" alt="" loading="lazy" width="1024" height="1536">
        </span>
        <span class="instructor-identity">
          <strong>Mark Rivera</strong>
          <small>Programming fundamentals</small>
        </span>
        <span class="instructor-bio">
          <strong>Mark Rivera</strong>
          <small>Programming fundamentals</small>
          <span>Learn the building blocks, try them in a practical activity, and keep developing your skills one step at a time.</span>
          <i aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"><path d="M7 17 17 7M9 7h8v8"></path></svg>
          </i>
        </span>
      </button>
      <button type="button" class="instructor-card" aria-expanded="false" aria-label="About Lena Kraft">
        <span class="instructor-photo" aria-hidden="true">
          <img src="assets/instructor-portraits/lena.webp" alt="" loading="lazy" width="1024" height="1536">
        </span>
        <span class="instructor-identity">
          <strong>Lena Kraft</strong>
          <small>Data & emerging technology</small>
        </span>
        <span class="instructor-bio">
          <strong>Lena Kraft</strong>
          <small>Data & emerging technology</small>
          <span>Make sense of data and discover the tools behind your next idea, with a clear learning path to follow.</span>
          <i aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"><path d="M7 17 17 7M9 7h8v8"></path></svg>
          </i>
        </span>
      </button>
    </div>
    <button type="button" class="instructor-arrow" data-instructor-step="1" aria-label="Next instructor">›</button>
  </div>
  <div class="instructor-position" aria-hidden="true">
    <i></i>
  </div>
  <p class="instructor-sr">Portraits are AI-generated illustrations of fictional instructors.</p>
</section>
<section class="home-section home-stories">
  <header class="home-section-head home-reveal">
    <div>
      <h2>Our Learners</h2>
      <p>Clear lessons. Real practice. A next step you can follow.</p>
    </div>
    <small>Illustrative learner stories</small>
  </header>
  <div class="home-story-grid">
    <div>
      <article class="home-story home-reveal">
        <span class="home-story-mark" aria-hidden="true">A</span>
        <blockquote>Clear lessons and practical examples made it easier to get started.</blockquote>
        <footer>
          Alex Morgan
          <span>Sample learner story</span>
        </footer>
      </article>
      <article class="home-story home-reveal">
        <span class="home-story-mark" aria-hidden="true">D</span>
        <blockquote>I can pick up where I left off and keep building on what I’ve learned.</blockquote>
        <footer>
          Daniel Lim
          <span>Sample learner story</span>
        </footer>
      </article>
    </div>
    <article class="home-story home-story-featured home-reveal">
      <img class="home-story-portrait" src="assets/visitor/learner-mei-tan.webp" alt="Illustrative learner portrait" width="68" height="68" loading="lazy" style="object-fit:cover;object-position:center 30%">
      <svg class="home-stars" viewBox="0 0 112 20" aria-label="4 out of 5 stars" role="img"><defs><path id="story-star" d="m10 1 2.7 5.6 6.2.9-4.5 4.4 1.1 6.2L10 15.2l-5.5 2.9 1.1-6.2-4.5-4.4 6.2-.9Z"></path></defs><g fill="#e8d07a"><use href="#story-star"></use><use href="#story-star" x="23"></use><use href="#story-star" x="46"></use><use href="#story-star" x="69"></use></g><use href="#story-star" x="92" fill="none" stroke="#e8d07a"></use></svg>
      <blockquote>The practical lessons helped me turn new ideas into skills I can use. I could follow my progress and learn at my own pace.</blockquote>
      <footer>
        Mei Tan
        <span>Sample learner story</span>
      </footer>
    </article>
    <div>
      <article class="home-story home-reveal">
        <span class="home-story-mark" aria-hidden="true">↗</span>
        <blockquote>The guided activities helped me put each new concept into practice.</blockquote>
        <footer>
          Alex Morgan
          <span>Sample learner story</span>
        </footer>
      </article>
      <article class="home-story home-reveal">
        <span class="home-story-mark" aria-hidden="true">↗</span>
        <blockquote>Seeing my progress gives me a clear view of what to learn next.</blockquote>
        <footer>
          Daniel Lim
          <span>Sample learner story</span>
        </footer>
      </article>
    </div>
  </div>
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
<asp:Content ContentPlaceHolderID="AfterMain" runat="server">
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
  <p class="tm-signup-help">Already have an account? <a href="Login">Log in</a></p>
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
          <a href="Contact" aria-label="Facebook">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 8h3V4h-3a4 4 0 0 0-4 4v2H7v4h3v8h4v-8h3l1-4h-4V8Z"></path></svg>
          </a>
          <a href="Contact" aria-label="X">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 4l16 16M20 4 4 20"></path></svg>
          </a>
          <a href="Contact" aria-label="Instagram">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="4" y="4" width="16" height="16" rx="5"></rect><circle cx="12" cy="12" r="3.5"></circle><path d="M16.5 7.5h.01"></path></svg>
          </a>
          <a href="Contact" aria-label="LinkedIn">
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
    <small>© 2026 Skill UP. Learning, one step at a time.</small>
    <nav aria-label="Legal">
      <a href="Privacy">Privacy information</a>
      <a href="Terms">Terms of use</a>
      <a href="Help">Help center</a>
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
</asp:Content>
