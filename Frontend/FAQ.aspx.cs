using System;

namespace SKILL_UP.Frontend
{
    public partial class FAQPage : SkillPage
    {
        public override string Route => "faq";
        public override PageLayout Layout => PageLayout.Public;

        /// <summary>The questions shown on this page and on the landing page's FAQ section.</summary>
        public static readonly object[] Entries =
        {
            new { Question = "How do I start learning?", Answer = "Create a learner account, choose a published course and enrol. Your progress is saved as you complete activities." },
            new { Question = "Can I learn at my own pace?", Answer = "Yes. Work through readings, practical labs and quizzes in order. Course deadlines help you plan your schedule." },
            new { Question = "How do I earn a certificate?", Answer = "Complete every activity and pass each quiz with at least 70%. Your certificate then appears in Certificates." },
            new { Question = "Who reviews the courses?", Answer = "Instructors prepare the content. A Program Leader reviews it before learners can enrol." }
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            Questions.DataSource = Entries;
            Questions.DataBind();
        }
    }
}
