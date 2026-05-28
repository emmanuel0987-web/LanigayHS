using System;
using System.ComponentModel.DataAnnotations;

namespace SDG4_LMS.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        [Display(Name = "User")]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public bool IsRead { get; set; } = false;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? LinkUrl { get; set; }
    }
}