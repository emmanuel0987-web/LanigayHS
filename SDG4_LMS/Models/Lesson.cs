using System;
using System.ComponentModel.DataAnnotations;

namespace SDG4_LMS.Models
{
    public class Lesson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? VideoUrl { get; set; }

        public string? FileUrl { get; set; }

        [Display(Name = "Lesson Order")]
        public int Order { get; set; }

        [Display(Name = "Course")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}