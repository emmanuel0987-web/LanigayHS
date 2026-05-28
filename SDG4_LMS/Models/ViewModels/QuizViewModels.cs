namespace SDG4_LMS.Models.ViewModels
{
    public class TakeQuizViewModel
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int TimeLimitMinutes { get; set; }
        public List<QuestionAnswerViewModel> Questions { get; set; } = new List< QuestionAnswerViewModel > ();
    }

    public class QuestionAnswerViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string SelectedAnswer { get; set; }
    }

    public class QuizResultViewModel
    {
        public string QuizTitle { get; set; }
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public bool IsPassed { get; set; }
        public int PassingScore { get; set; }
        public string TimeTaken { get; set; }
        public List<AnswerResult> Answers { get; set; }
    }

    public class AnswerResult
    {
        public string QuestionText { get; set; }
        public string SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public int Points { get; set; }
    }

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