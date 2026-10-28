using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// Course files: anyone with access downloads them; staff upload (stored as a data URL in COURSE_RESOURCE) and delete.
    /// Set CourseId for one course, or leave it empty to list every course the viewer can open.
    /// </summary>
    public partial class CourseFiles : UserControl
    {
        static readonly Dictionary<string, string> Types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf", [".txt"] = "text/plain", [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".webp"] = "image/webp"
        };

        public int? CourseId { get; set; }

        protected bool Manage;
        SkillPage Host => (SkillPage)Page;

        protected void Page_Load(object sender, EventArgs e)
        {
            Manage = Host.Me != null && Host.Me.Role != "Learner";
            UploadPanel.Visible = Manage;
            CoursePicker.Visible = Manage && CourseId == null;
            if (CoursePicker.Visible && !IsPostBack)
            {
                UploadCourse.DataSource = SkillPage.CourseOptions(Host.OwnCourses);
                UploadCourse.DataBind();
            }
        }

        protected void Upload_Click(object sender, EventArgs e)
        {
            if (!UploadFile.HasFile)
            {
                Host.ShowError("Choose a file to upload.");
                return;
            }
            string type;
            if (!Types.TryGetValue(Path.GetExtension(UploadFile.FileName), out type))
            {
                Host.ShowError("Supported files: PDF, TXT, PNG, JPG and WebP.");
                return;
            }
            var bytes = UploadFile.FileBytes;
            if (bytes.Length > 2 * 1024 * 1024)
            {
                Host.ShowError("Files must be smaller than 2 MB.");
                return;
            }
            var payload = new
            {
                courseId = (CourseId?.ToString() ?? UploadCourse.SelectedValue),
                name = Path.GetFileName(UploadFile.FileName),
                size = bytes.Length,
                dataUrl = "data:" + type + ";base64," + Convert.ToBase64String(bytes)
            };
            if (Host.TryRun("resourceSave", payload)) Host.ReloadPage("File uploaded. The course is back in Draft for review.");
        }

        protected void Delete_Command(object sender, CommandEventArgs e)
        {
            if (Host.TryRun("resourceDelete", new { id = (string)e.CommandArgument })) Host.ReloadPage("File deleted.");
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            var courses = Host.AccessibleCourses.Where(c => CourseId == null || c.Id == CourseId).ToDictionary(c => c.Id);
            var files = Host.Data.Resources.Where(r => courses.ContainsKey(r.CourseId)).Select(r =>
            {
                long bytes;
                long.TryParse(r.Size, out bytes);
                return new { r.Id, r.Name, r.DataUrl, Extension = Path.GetExtension(r.Name).TrimStart('.'), Kb = Math.Round(bytes / 1024.0), Course = courses[r.CourseId].Title };
            }).ToList();
            Files.DataSource = files;
            Files.DataBind();
            NoFiles.Visible = files.Count == 0;
        }
    }
}
