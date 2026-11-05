namespace SkillUp.Models;

/// <summary>What the course tile partial (Views/Shared/_CourseCard.cshtml) draws.</summary>
public sealed record CourseCardModel(CourseRow Course, bool Manage = false);
