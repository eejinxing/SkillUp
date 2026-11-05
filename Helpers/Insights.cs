using System;
using System.Collections.Generic;
using System.Linq;
using SkillUp.Models;
using SkillUp.Services;

namespace SkillUp.Helpers
{
    /// <summary>One learner on the leaderboard for a period.</summary>
    public sealed class RankRow
    {
        public UserRow User;
        /// <summary>Courses this learner has fully completed (all-time ranking only).</summary>
        public int Completed;
        /// <summary>Courses this learner is enrolled in (visible courses only).</summary>
        public int Enrolled;
        /// <summary>Best and average quiz score (0-100) in the ranking period; null without quiz attempts.</summary>
        public int? Best;
        public double? Average;
        /// <summary>Time taken for the quiz that reached the best score (the fastest such attempt); null when it was not recorded. The first tie-breaker after the scores.</summary>
        public TimeSpan? QuizTaken;
        /// <summary>Total time taken from enrolling to completing the finished courses; the last tie-breaker.</summary>
        public TimeSpan? CourseTaken;
        public DateTime? LastActive;
        /// <summary>Display points: 100 per completed course plus the best quiz score. Order is decided by the ranking order, not by this number.</summary>
        public int Points;
        /// <summary>Learners tied on every part of the ranking order share a rank; learners with no points are unranked (null).</summary>
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
            /// <summary>How long a quiz attempt took (opened to submitted); null when the opening time was not recorded.</summary>
            public TimeSpan? Taken;
            /// <summary>Topics of the questions answered wrongly, or the lab's title when it was not passed.</summary>
            public List<string> WrongTopics = new List<string>();
        }

        /// <summary>
        /// Quiz attempts for courses the viewer can see, plus lab submissions the viewer may see
        /// (their own, or all of them for staff who manage the course).
        /// </summary>
        public static List<Attempt> Attempts(SkillContext page)
        {
            var visible = new HashSet<int>(page.VisibleCourses.Select(c => c.Id));
            var list = new List<Attempt>();
            foreach (var a in page.Data.QuizAttempts)
            {
                var quiz = page.Data.Quizzes.FirstOrDefault(q => q.Id == a.QuizId);
                if (quiz == null || !visible.Contains(quiz.CourseId)) continue;
                var attempt = new Attempt { UserId = a.UserId, CourseId = quiz.CourseId, Score = (int)Math.Round(a.Score ?? 0), Type = "quiz", At = a.CreatedAt,
                    Taken = a.StartedAt != null && a.CreatedAt > a.StartedAt.Value ? a.CreatedAt - a.StartedAt.Value : (TimeSpan?)null };
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

        /// <summary>
        /// Leaderboard of active learners, ranked all-time and for the current week. Order: courses completed, then best quiz
        /// score, then quiz average, then the faster quiz time, then the faster total course completion time.
        /// </summary>
        public static Board Leaderboard(SkillContext page, DateTime now)
        {
            var start = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            var end = start.AddDays(7);
            var courses = page.VisibleCourses.ToDictionary(c => c.Id);
            var attempts = Attempts(page);
            var learners = page.Data.Users.Where(u => u.Role == "Learner" && u.Status == "Active").ToList();
            Func<UserRow, bool, RankRow> make = (u, weekOnly) =>
            {
                var enrolments = page.Data.Enrollments.Where(e => e.UserId == u.Id && courses.ContainsKey(e.CourseId)).ToList();
                var done = enrolments.Where(e => page.Progress(u.Id, e.CourseId) == 100).GroupBy(e => e.CourseId).Select(g => g.First()).ToList();
                var mine = attempts.Where(a => a.UserId == u.Id && courses.ContainsKey(a.CourseId) && a.At <= now).ToList();
                var quizzes = mine.Where(a => a.Type == "quiz" && (!weekOnly || (a.At >= start && a.At < end))).Select(a => new { a.At, a.Taken, Score = Math.Max(0, Math.Min(100, a.Score)) }).ToList();
                int? best = quizzes.Count == 0 ? (int?)null : quizzes.Max(q => q.Score);
                var completed = weekOnly ? 0 : done.Count;
                var fastest = quizzes.Where(q => q.Score == best && q.Taken != null).Select(q => q.Taken.Value).ToList();
                var courseTimes = done.Where(e => (e.CompletedAt ?? e.LastActive) != null).Select(e => (e.CompletedAt ?? e.LastActive).Value - e.EnrolledAt).Where(t => t > TimeSpan.Zero).ToList();
                var times = mine.Select(a => (DateTime?)a.At).Concat(enrolments.Select(e => e.LastActive)).Where(t => t != null && t <= now).ToList();
                return new RankRow
                {
                    User = u, Completed = completed, Enrolled = enrolments.Select(e => e.CourseId).Distinct().Count(),
                    Best = best, Average = quizzes.Count == 0 ? (double?)null : quizzes.Average(q => q.Score),
                    QuizTaken = fastest.Count == 0 ? (TimeSpan?)null : fastest.Min(),
                    CourseTaken = weekOnly || courseTimes.Count == 0 ? (TimeSpan?)null : new TimeSpan(courseTimes.Sum(t => t.Ticks)),
                    LastActive = times.Count == 0 ? null : times.Max(), Points = completed * 100 + (best ?? 0)
                };
            };
            return new Board { All = Rank(learners.Select(u => make(u, false))), Week = Rank(learners.Select(u => make(u, true))), Start = start, End = end, UpdatedAt = now };
        }

        /// <summary>Sorts by the ranking order; learners tied on every part (including both times) share a rank, and learners with no points are unranked (null).</summary>
        static List<RankRow> Rank(IEnumerable<RankRow> rows)
        {
            var sorted = rows.OrderByDescending(r => r.Points > 0).ThenByDescending(r => r.Completed).ThenByDescending(r => r.Best ?? -1).ThenByDescending(r => r.Average ?? -1)
                .ThenBy(r => r.QuizTaken ?? TimeSpan.MaxValue).ThenBy(r => r.CourseTaken ?? TimeSpan.MaxValue).ThenBy(r => r.User.Name, StringComparer.CurrentCulture).ThenBy(r => r.User.Id).ToList();
            Func<RankRow, string> key = r => r.Completed + "|" + r.Best + "|" + (r.Average == null ? "" : r.Average.Value.ToString("0.###")) + "|" + r.QuizTaken + "|" + r.CourseTaken;
            string previous = null;
            int? last = null;
            for (var i = 0; i < sorted.Count; i++)
            {
                var r = sorted[i];
                if (r.Points > 0 && key(r) != previous) last = i + 1;
                previous = key(r);
                r.Rank = r.Points > 0 ? last : null;
            }
            return sorted;
        }
    }
}
