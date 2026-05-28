using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDG4_LMS.Data;
using SDG4_LMS.Models;
using SDG4_LMS.Models.ViewModels;

namespace SDG4_LMS.Controllers
{
    public class QuizzesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizzesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ADDED: Index action to fix 404 error
        public async Task<IActionResult> Index(int? courseId)
        {
            var quizzes = _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .AsQueryable();

            if (courseId.HasValue)
            {
                quizzes = quizzes.Where(q => q.CourseId == courseId.Value);
                ViewBag.CourseId = courseId.Value;
            }

            return View(await quizzes.OrderByDescending(q => q.CreatedAt).ToListAsync());
        }

        // GET: Quizzes/Create?courseId=1
        [Authorize(Roles = "Teacher,Admin")]
        public IActionResult Create(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        // POST: Quizzes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create([Bind("Title,Description,CourseId,TimeLimitMinutes,PassingScore")] Quiz quiz)
        {
            if (ModelState.IsValid)
            {
                _context.Add(quiz);
                await _context.SaveChangesAsync();
                return RedirectToAction("Create", "QuizQuestions", new { quizId = quiz.Id });
            }
            ViewBag.CourseId = quiz.CourseId;
            return View(quiz);
        }

        // GET: Quizzes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();

            return View(quiz);
        }

        // STUDENT: Take Quiz
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Take(int? id)
        {
            if (id == null) return NotFound();

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null || !quiz.Questions.Any())
            {
                TempData["Error"] = "This quiz has no questions yet.";
                return RedirectToAction("Dashboard", "Students");
            }

            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null) return Forbid();

            var enrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == quiz.CourseId && e.StudentId == student.Id);
            if (!enrolled)
            {
                TempData["Error"] = "You are not enrolled in this course.";
                return RedirectToAction("Details", new { id = quiz.Id });
            }

            var attempt = new QuizAttempt
            {
                QuizId = quiz.Id,
                StudentId = student.Id,
                StartTime = DateTime.Now
            };
            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            var vm = new TakeQuizViewModel
            {
                AttemptId = attempt.Id,
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                Questions = quiz.Questions.Select(q => new QuestionAnswerViewModel
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD
                }).ToList()
            };

            return View(vm);
        }

        // STUDENT: Submit Answers
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(TakeQuizViewModel vm)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz).ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(a => a.Id == vm.AttemptId);

            if (attempt == null || attempt.EndTime != null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null || attempt.StudentId != student.Id) return Forbid();

            int totalPoints = attempt.Quiz.Questions.Sum(q => q.Points);
            int earned = 0;

            foreach (var ans in vm.Questions)
            {
                var q = attempt.Quiz.Questions.FirstOrDefault(x => x.Id == ans.QuestionId);
                if (q == null) continue;

                bool correct = q.CorrectAnswer?.ToUpper() == ans.SelectedAnswer?.ToUpper();
                if (correct) earned += q.Points;

                _context.QuizAnswers.Add(new QuizAnswer
                {
                    QuizAttemptId = attempt.Id,
                    QuizQuestionId = q.Id,
                    SelectedAnswer = ans.SelectedAnswer,
                    IsCorrect = correct
                });
            }

            double pct = totalPoints > 0 ? (earned / (double)totalPoints) * 100 : 0;
            attempt.EndTime = DateTime.Now;
            attempt.Score = (int)Math.Round(pct);
            attempt.TotalPoints = totalPoints;
            attempt.IsPassed = pct >= attempt.Quiz.PassingScore;

            await _context.SaveChangesAsync();
            return RedirectToAction("Results", new { id = attempt.Id });
        }

        // Results page
        public async Task<IActionResult> Results(int? id)
        {
            if (id == null) return NotFound();

            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.Answers).ThenInclude(ans => ans.QuizQuestion)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attempt == null) return NotFound();

            if (User.IsInRole("Student"))
            {
                var user = await _userManager.GetUserAsync(User);
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student == null || attempt.StudentId != student.Id) return Forbid();
            }

            var vm = new QuizResultViewModel
            {
                QuizTitle = attempt.Quiz.Title,
                Score = attempt.Score ?? 0,
                TotalPoints = attempt.TotalPoints ?? 0,
                IsPassed = attempt.IsPassed,
                PassingScore = attempt.Quiz.PassingScore,
                TimeTaken = attempt.EndTime.HasValue ? (attempt.EndTime.Value - attempt.StartTime).ToString(@"mm\:ss") : "N/A",
                Answers = attempt.Answers.Select(a => new AnswerResult
                {
                    QuestionText = a.QuizQuestion.QuestionText,
                    SelectedAnswer = a.SelectedAnswer,
                    CorrectAnswer = a.QuizQuestion.CorrectAnswer,
                    IsCorrect = a.IsCorrect,
                    Points = a.QuizQuestion.Points
                }).ToList()
            };

            return View(vm);
        }
    }
}
