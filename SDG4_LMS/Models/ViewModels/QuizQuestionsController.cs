using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;

namespace SDG4_LMS.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class QuizQuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizQuestionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Create(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (teacher == null || quiz.Course.TeacherId != teacher.Id)
                    return Forbid();
            }

            int existingCount = quiz.Questions.Count;
            int remaining = 20 - existingCount;

            if (remaining <= 0)
            {
                TempData["Warning"] = "This quiz already has 20 questions (maximum reached).";
                return RedirectToAction("Details", "Quizzes", new { id = quizId });
            }

            // Pre-populate 5 empty question slots (or however many remain)
            var vm = new QuizQuestionsCreateViewModel
            {
                QuizId = quizId,
                QuizTitle = quiz.Title,
                Questions = Enumerable.Range(0, Math.Min(5, remaining))
                    .Select(_ => new QuestionInput()).ToList()
            };

            ViewBag.ExistingCount = existingCount;
            ViewBag.Remaining = remaining;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizQuestionsCreateViewModel vm)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == vm.QuizId);

            if (quiz == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (teacher == null || quiz.Course.TeacherId != teacher.Id)
                    return Forbid();
            }

            int existingCount = quiz.Questions.Count;
            var validQuestions = vm.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.QuestionText))
                .ToList();

            if (existingCount + validQuestions.Count > 20)
            {
                ModelState.AddModelError("", $"Maximum 20 questions allowed. You have {existingCount}, can add {20 - existingCount} more.");
                ViewBag.ExistingCount = existingCount;
                ViewBag.Remaining = 20 - existingCount;
                return View(vm);
            }

            foreach (var q in validQuestions)
            {
                _context.QuizQuestions.Add(new QuizQuestion
                {
                    QuizId = vm.QuizId,
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectAnswer = q.CorrectAnswer?.ToUpper(),
                    Points = q.Points > 0 ? q.Points : 1
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{validQuestions.Count} question(s) added!";

            // If still under 20, stay on page to add more
            if (existingCount + validQuestions.Count < 20)
            {
                return RedirectToAction(nameof(Create), new { quizId = vm.QuizId });
            }

            return RedirectToAction("Details", "Quizzes", new { id = vm.QuizId });
        }
    }

    // ViewModels for QuizQuestions
    public class QuizQuestionsCreateViewModel
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public List<QuestionInput> Questions { get; set; } = new List< QuestionInput > ();
    }

    public class QuestionInput
    {
        public string QuestionText { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string CorrectAnswer { get; set; }
        public int Points { get; set; } = 1;
    }
}