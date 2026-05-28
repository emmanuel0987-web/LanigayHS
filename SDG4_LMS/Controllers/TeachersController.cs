using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;

namespace SDG4_LMS.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeachersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeachersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Courses)
                .ThenInclude(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (teacher == null)
            {
                TempData["Error"] = "Teacher profile not found.";
                return RedirectToAction("Index", "Home");
            }

            var enrollments = await _context.Enrollments
                .Where(e => e.Course.TeacherId == teacher.Id)
                .Include(e => e.Course)
                .Include(e => e.Student).ThenInclude(s => s.User)
                .OrderBy(e => e.Course.Title)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;
            // SAFE: build display name from FirstName + LastName, fallback to Email
            ViewBag.TeacherName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(ViewBag.TeacherName))
                ViewBag.TeacherName = user.Email;

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(int enrollmentId, double? finalGrade)
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher == null) return Forbid();

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                TempData["Error"] = "Enrollment not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (enrollment.Course.TeacherId != teacher.Id && !User.IsInRole("Admin"))
                return Forbid();

            if (finalGrade.HasValue)
            {
                enrollment.FinalGrade = finalGrade.Value;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Grade updated successfully.";
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}