namespace SDG4_LMS.Models
{
    public class QuizAnswer
    {
        public int Id { get; set; }
        public int QuizAttemptId { get; set; }
        public int QuizQuestionId { get; set; }
        public string SelectedAnswer { get; set; }
        public bool IsCorrect { get; set; }

        public QuizAttempt QuizAttempt { get; set; }
        public QuizQuestion QuizQuestion { get; set; }
    }
}