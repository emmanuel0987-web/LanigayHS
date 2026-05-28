namespace SDG4_LMS.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int StudentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Score { get; set; }
        public int? TotalPoints { get; set; }
        public bool IsPassed { get; set; }

        public Quiz Quiz { get; set; }
        public Student Student { get; set; }
        public ICollection<QuizAnswer> Answers { get; set; } = new List< QuizAnswer > ();
    }
}