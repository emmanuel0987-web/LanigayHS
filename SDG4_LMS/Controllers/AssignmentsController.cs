using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;

namespace SDG4_LMS.Controllers
{
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int courseId)
        {
            var assignments = await _context.Assignments
                .Where(a => a.CourseId == courseId)
                .ToListAsync();

            ViewData["CourseId"] = courseId;
            ViewData["CourseTitle"] = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => c.Title)
                .FirstOrDefaultAsync();

            return View(assignments);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public IActionResult Create(int courseId)
        {
            ViewData["CourseId"] = courseId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([Bind("Title,Description,DueDate,MaxScore,CourseId")] Assignment assignment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { courseId = assignment.CourseId });
            }
            return View(assignment);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            var submissions = await _context.AssignmentSubmissions
                .Where(s => s.AssignmentId == id)
                .Include(s => s.Student)
                .ThenInclude(st => st.User)
                .ToListAsync();

            ViewData["Submissions"] = submissions;

            return View(assignment);
        }

        [Authorize(Roles = "Student")]
        public IActionResult Submit(int assignmentId)
        {
            ViewData["AssignmentId"] = assignmentId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit([Bind("AssignmentId,TextContent")] AssignmentSubmission submission)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null) return NotFound();

                submission.StudentId = student.Id;
                submission.SubmittedAt = DateTime.UtcNow;

                _context.Add(submission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Assignment submitted successfully!";
                return RedirectToAction("Dashboard", "Students");
            }
            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Grade(int id, double score, string feedback)
        {
            var submission = await _context.AssignmentSubmissions.FindAsync(id);
            if (submission == null) return NotFound();

            submission.Score = score;
            submission.Feedback = feedback;
            _context.Update(submission);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = submission.AssignmentId });
        }
    }
}