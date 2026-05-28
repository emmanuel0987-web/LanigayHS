using System;
using System.ComponentModel.DataAnnotations;

namespace SDG4_LMS.Models
{
    public class AssignmentSubmission
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Assignment")]
        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        [Display(Name = "Student")]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public string? FileUrl { get; set; }

        public string? TextContent { get; set; }

        public double? Score { get; set; }

        public string? Feedback { get; set; }

        [Display(Name = "Submitted At")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}