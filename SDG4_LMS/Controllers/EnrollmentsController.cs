using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SDG4_LMS.Controllers
{
    [Authorize(Roles = "Student,Teacher,Admin")]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrollmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            IQueryable<Enrollment> enrollments = _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .ThenInclude(s => s.User);

            if (User.IsInRole("Student"))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student != null)
                {
                    enrollments = enrollments
                        .Where(e => e.StudentId == student.Id);
                }
            }
            else if (User.IsInRole("Teacher"))
            {
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == user.Id);

                if (teacher != null)
                {
                    var teacherCourseIds = await _context.Courses
                        .Where(c => c.TeacherId == teacher.Id)
                        .Select(c => c.Id)
                        .ToListAsync();

                    enrollments = enrollments
                        .Where(e => teacherCourseIds.Contains(e.CourseId));
                }
            }

            return View(await enrollments.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student == null)
                return NotFound();

            var existing = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == student.Id &&
                    e.CourseId == courseId);

            if (existing != null)
            {
                TempData["ErrorMessage"] =
                    "You are already enrolled in this course.";

                return RedirectToAction(
                    "Details",
                    "Courses",
                    new { id = courseId });
            }

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                CourseId = courseId,
                EnrollmentDate = DateTime.UtcNow,
                Status = "Active"
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "You have successfully enrolled!";

            return RedirectToAction(
                "Details",
                "Courses",
                new { id = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Grade(int id, double finalGrade)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);

            if (enrollment == null)
                return NotFound();

            enrollment.FinalGrade = finalGrade;

            _context.Update(enrollment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}