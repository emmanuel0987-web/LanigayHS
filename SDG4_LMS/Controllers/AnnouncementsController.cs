using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;

namespace SDG4_LMS.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnnouncementsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var announcements = await _context.Announcements
                .Include(a => a.PostedBy)
                .Include(a => a.Course)
                .OrderByDescending(a => a.PostedAt)
                .ToListAsync();

            return View(announcements);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public IActionResult Create()
        {
            ViewData["Courses"] = _context.Courses.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([Bind("Title,Content,CourseId,IsImportant")] Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                announcement.PostedById = user.Id;
                announcement.PostedAt = DateTime.UtcNow;

                _context.Add(announcement);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Announcement posted!";
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }
    }
}