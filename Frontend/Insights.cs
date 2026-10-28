using System;
using System.Collections.Generic;
using System.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>One learner on the leaderboard for a period.</summary>
    public sealed class RankRow
    {
        public UserRow User;
        /// <summary>Courses this learner has fully completed.</summary>
        public int Completed;
        /// <summary>All-time points: 100 per completed course plus the best quiz score of each course.</summary>
        public int AllPoints;
        /// <summary>This week's points: the best quiz score of each course, counting attempts made this week.</summary>
        public int WeekPoints;
        public DateTime? LastActive;
        public int Points;
        /// <summary>Equal scores share a rank; learners with no points are unranked (null).</summary>
        public int? Rank;
    }

    public sealed class Board
    {
        public List<RankRow> All, Week;
        /// <summary>The leaderboard week runs from Monday 00:00 to the next Monday.</summary>
        public DateTime Start, End, UpdatedAt;
    }

    /// <summary>Numbers derived from saved learning records, shared by the dashboard and the leaderboard.</summary>
    public static class Insights
    {
        /// <summary>A quiz or lab result with the course it belongs to.</summary>
        public sealed class Attempt
        {
            public int UserId, CourseId, Score;
            public string Type;
            public DateTime At;
            /// <summary>Topics of the questions answered wrongly, or the lab's title when it was not passed.</summary>
            public List<string> WrongTopics = new List<string>();
        }

        /// <summary>
        /// Quiz attempts for courses the viewer can see, plus lab submissions the viewer may see
        /// (their own, or all of them for staff who manage the course).
        /// </summary>
        public static List<Attempt> Attempts(SkillPage page)
        {
            var visible = new HashSet<int>(page.VisibleCourses.Select(c => c.Id));
            var list = new List<Attempt>();
            foreach (var a in page.Data.QuizAttempts)
            {
                var quiz = page.Data.Quizzes.FirstOrDefault(q => q.Id == a.QuizId);
                if (quiz == null || !visible.Contains(quiz.CourseId)) continue;
                var attempt = new Attempt { UserId = a.UserId, CourseId = quiz.CourseId, Score = (int)Math.Round(a.Score ?? 0), Type = "quiz", At = a.CreatedAt };
                // Which answers were wrong is shown to the learner and to staff who manage the course, as in the attempt details.
                if (page.Me?.Role == "Admin" || a.UserId == page.Me?.Id || page.Managed.Contains(quiz.CourseId))
                    foreach (var q in quiz.Questions)
                    {
                        var answer = a.Answers.FirstOrDefault(x => x.QuestionId == q.Id);
                        if (answer != null && !answer.Correct) attempt.WrongTopics.Add(q.Topic ?? "");
                    }
                list.Add(attempt);
            }
            foreach (var s in page.Data.Submissions)
            {
                var lesson = page.Data.Lessons.FirstOrDefault(l => l.Id == s.LabId);
                if (lesson == null || lesson.Type != "lab") continue;
                if (!(page.Me?.Role == "Admin" || s.UserId == page.Me?.Id || page.Managed.Contains(lesson.CourseId))) continue;
                var lab = new Attempt { UserId = s.UserId, CourseId = lesson.CourseId, Score = (int)Math.Round(s.Score ?? 0), Type = "lab", At = s.SubmittedAt };
                if (s.Result != "Passed") lab.WrongTopics.Add(lesson.Title);
                list.Add(lab);
            }
            return list.OrderBy(a => a.At).ToList();
        }

        /// <summary>Leaderboard of active learners, ranked all-time and for the current week.</summary>
        public static Board Leaderboard(SkillPage page, DateTime now)
        {
            var start = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            var end = start.AddDays(7);
            var courses = page.VisibleCourses.ToDictionary(c => c.Id);
            var attempts = Attempts(page);
            var rows = page.Data.Users.Where(u => u.Role == "Learner" && u.Status == "Active").Select(u =>
            {
                var enrolments = page.Data.Enrollments.Where(e => e.UserId == u.Id && courses.ContainsKey(e.CourseId)).ToList();
                var completed = enrolments.Where(e => page.Progress(u.Id, e.CourseId) == 100).Select(e => e.CourseId).Distinct().Count();
                var mine = attempts.Where(a => a.UserId == u.Id && courses.ContainsKey(a.CourseId) && a.At <= now).ToList();
                var quizzes = mine.Where(a => a.Type == "quiz").ToList();
                var best = quizzes.GroupBy(a => a.CourseId).Sum(g => g.Max(a => Math.Max(0, Math.Min(100, a.Score))));
                var weekly = quizzes.Where(a => a.At >= start && a.At < end).GroupBy(a => a.CourseId).Sum(g => g.Max(a => Math.Max(0, Math.Min(100, a.Score))));
                var times = mine.Select(a => (DateTime?)a.At).Concat(enrolments.Select(e => e.LastActive)).Where(t => t != null && t <= now).ToList();
                return new RankRow { User = u, Completed = completed, AllPoints = completed * 100 + best, WeekPoints = weekly, LastActive = times.Count == 0 ? null : times.Max() };
            }).ToList();
            return new Board { All = Rank(rows, r => r.AllPoints), Week = Rank(rows, r => r.WeekPoints), Start = start, End = end, UpdatedAt = now };
        }

        static List<RankRow> Rank(IEnumerable<RankRow> rows, Func<RankRow, int> points)
        {
            var sorted = rows.Select(r => new RankRow { User = r.User, Completed = r.Completed, AllPoints = r.AllPoints, WeekPoints = r.WeekPoints, LastActive = r.LastActive, Points = points(r) })
                .OrderByDescending(r => r.Points).ThenBy(r => r.User.Name, StringComparer.CurrentCulture).ThenBy(r => r.User.Id).ToList();
            int? previous = null, last = null;
            for (var i = 0; i < sorted.Count; i++)
            {
                var r = sorted[i];
                if (r.Points > 0 && r.Points != previous) last = i + 1;
                previous = r.Points;
                r.Rank = r.Points > 0 ? last : null;
            }
            return sorted;
        }
    }
}
