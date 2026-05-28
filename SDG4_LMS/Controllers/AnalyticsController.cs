using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;

namespace SDG4_LMS.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (teacher == null && !User.IsInRole("Admin")) return Forbid();

            var courseIds = teacher != null
                ? await _context.Courses.Where(c => c.TeacherId == teacher.Id).Select(c => c.Id).ToListAsync()
                : await _context.Courses.Select(c => c.Id).ToListAsync();

            // Enrollment stats
            var enrollmentData = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId))
                .GroupBy(e => e.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync();

            var courseNames = await _context.Courses
                .Where(c => courseIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Title })
                .ToDictionaryAsync(c => c.Id, c => c.Title);

            ViewData["CourseLabels"] = enrollmentData.Select(e => courseNames.ContainsKey(e.CourseId) ? courseNames[e.CourseId] : "Unknown").ToList();
            ViewData["EnrollmentCounts"] = enrollmentData.Select(e => e.Count).ToList();

            // Grade distribution
            var grades = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId) && e.FinalGrade.HasValue)
                .Select(e => e.FinalGrade.Value)
                .ToListAsync();

            ViewData["GradeRanges"] = new List<int>
            {
                grades.Count(g => g >= 90),
                grades.Count(g => g >= 80 && g < 90),
                grades.Count(g => g >= 70 && g < 80),
                grades.Count(g => g >= 60 && g < 70),
                grades.Count(g => g < 60)
            };

            // Quiz pass/fail stats
            var quizAttempts = await _context.QuizAttempts
                .Where(qa => courseIds.Contains(qa.Quiz.CourseId))
                .ToListAsync();

            ViewData["QuizPassed"] = quizAttempts.Count(qa => qa.IsPassed);
            ViewData["QuizFailed"] = quizAttempts.Count(qa => !qa.IsPassed);

            // Total stats
            ViewData["TotalStudents"] = await _context.Students.CountAsync();
            ViewData["TotalCourses"] = courseIds.Count;
            ViewData["TotalEnrollments"] = await _context.Enrollments.CountAsync(e => courseIds.Contains(e.CourseId));
            ViewData["AverageGrade"] = grades.Any() ? grades.Average() : 0;

            return View();
        }
    }
}