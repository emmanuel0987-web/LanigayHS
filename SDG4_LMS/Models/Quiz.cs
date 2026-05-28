using System.ComponentModel.DataAnnotations;

namespace SDG4_LMS.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CourseId { get; set; }
        public int TimeLimitMinutes { get; set; } = 30;
        public int PassingScore { get; set; } = 60;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Course Course { get; set; }
        public ICollection<QuizQuestion> Questions { get; set; } = new List< QuizQuestion > ();
        public ICollection<QuizAttempt> Attempts { get; set; } = new List< QuizAttempt > ();
    }
}
