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
    }
}
