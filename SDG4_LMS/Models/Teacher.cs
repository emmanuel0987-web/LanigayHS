using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDG4_LMS.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public string Department { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;

        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}