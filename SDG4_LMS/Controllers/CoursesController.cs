using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;

namespace SDG4_LMS.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string searchString, string category)
        {
            var courses = from c in _context.Courses select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(s => s.Title.Contains(searchString) || s.Description.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(category))
            {
                courses = courses.Where(s => s.Category == category);
            }

            courses = courses.Include(c => c.Teacher).ThenInclude(t => t.User);

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = category;

            var categories = await _context.Courses.Select(c => c.Category).Distinct().ToListAsync();
            ViewData["Categories"] = categories;

            return View(await courses.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Teacher).ThenInclude(t => t.User)
                .Include(c => c.Enrollments).ThenInclude(e => e.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // GET: Courses/Create
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateTeacherDropDownAsync();
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([Bind("Title,Description,Category,Credits,StartDate,EndDate,TeacherId")] Course course)
        {
            // If Teacher is not selected from dropdown and user is a Teacher, auto-assign
            if (course.TeacherId == 0 && User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (teacher != null)
                {
                    course.TeacherId = teacher.Id;
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdown if validation fails so user doesn't lose the list
            await PopulateTeacherDropDownAsync(course.TeacherId);
            return View(course);
        }

        // GET: Courses/Edit/5
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (teacher == null || course.TeacherId != teacher.Id)
                {
                    return Forbid();
                }
            }

            await PopulateTeacherDropDownAsync(course.TeacherId);
            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Category,Credits,StartDate,EndDate,IsActive,TeacherId")] Course course)
        {
            if (id != course.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateTeacherDropDownAsync(course.TeacherId);
            return View(course);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Teacher).ThenInclude(t => t.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        // Helper: fills ViewBag.TeacherId with all teachers
        private async Task PopulateTeacherDropDownAsync(object selectedTeacher = null)
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .OrderBy(t => t.User.LastName)
                .Select(t => new
                {
                    t.Id,
                    FullName = t.User.FirstName + " " + t.User.LastName
                })
                .ToListAsync();

            ViewBag.TeacherId = new SelectList(teachers, "Id", "FullName", selectedTeacher);
        }
    }
}