using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDG4_LMS.Models
{
    public class Enrollment
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Student")]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Display(Name = "Course")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Display(Name = "Enrollment Date")]
        [DataType(DataType.Date)]
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Active";

        [Range(0, 100)]
        public double? FinalGrade { get; set; }
    }
}