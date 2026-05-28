using System;
using System.ComponentModel.DataAnnotations;

namespace SDG4_LMS.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        [Display(Name = "Posted By")]
        public string PostedById { get; set; } = string.Empty;
        public ApplicationUser? PostedBy { get; set; }

        [Display(Name = "Course")]
        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        [Display(Name = "Posted At")]
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        public bool IsImportant { get; set; } = false;
    }
}