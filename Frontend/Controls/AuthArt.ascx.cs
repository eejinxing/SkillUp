using System;
using System.Web.UI;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>The artwork half of the sign-in screens: brand, feature chips and the three steps of the current flow.</summary>
    public partial class AuthArt : UserControl
    {
        sealed class Step
        {
            public string Title { get; set; }
            public string Detail { get; set; }
        }

        /// <summary>login, register, forgot or reset.</summary>
        public string Kind { get; set; } = "login";

        protected string Heading, Lead;
        protected int CurrentStep;

        protected void Page_Load(object sender, EventArgs e)
        {
            Step[] steps;
            switch (Kind)
            {
                case "register":
                    Heading = "Learn practical skills, one lesson at a time.";
                    Lead = "Join as a learner to take courses, or as an instructor to share what you know.";
                    steps = new[] { S("Create your account", "Your name, email and a password."), S("Choose what to learn", "Browse programs and enrol in a course."), S("Start your first lesson", "Progress saves as you complete activities.") };
                    break;
                case "forgot":
                case "reset":
                    Heading = Kind == "forgot" ? "Locked out? It happens." : "Almost there.";
                    Lead = Kind == "forgot" ? "Getting back in takes three short steps." : "Set a new password to get back to your courses.";
                    steps = new[] { S("Enter your email", "The address you registered with."), S("Open the reset link", "It stays valid for 15 minutes."), S("Set a new password", "8+ characters, a capital, a number and a symbol.") };
                    CurrentStep = Kind == "forgot" ? 0 : 2;
                    break;
                default:
                    Heading = "Pick up where you left off.";
                    Lead = "Your courses, progress and messages are kept in your workspace.";
                    steps = new[] { S("Log in", "With your email or username."), S("Open your dashboard", "See your courses and announcements."), S("Keep learning", "Resume your next unfinished activity.") };
                    break;
            }
            StepList.DataSource = steps;
            StepList.DataBind();
        }

        static Step S(string title, string detail) => new Step { Title = title, Detail = detail };
    }
}
