using System.Web.UI;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// The activity page's practice terminal. A lab shows it before its task, other activities after it, so the
    /// activity page places one instance on each side and shows the right one.
    /// </summary>
    public partial class TerminalLab : UserControl
    {
        public LessonRow Lesson { get; set; }
        /// <summary>Visitors see a read-only terminal.</summary>
        public bool Guest { get; set; }
        public int TaskNumber { get; set; }
        public string Username { get; set; } = "learner";
        /// <summary>What the editor starts with (and Reset command goes back to). Set by the instructor per activity.</summary>
        public string StarterCode { get; set; }
        /// <summary>The task text shown above the editor; when empty, the lesson's own body.</summary>
        public string BodyText { get; set; }
        protected string Starter => string.IsNullOrWhiteSpace(StarterCode) ? "// Write JavaScript here, then press Run.\nconsole.log('Hello!');" : StarterCode;
    }
}
