using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
namespace FutureTechAcademy.Models
{
    public class Student
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }

        [Required(ErrorMessage = "Enrolment status is required")]
        [Display(Name = "Enrolment Status")]
        public string EnrolmentStatus { get; set; } // "Active" or "Inactive"

        [Display(Name = "Profile Image")]
        public string ProfileImageUrl { get; set; }

        // For soft delete functionality
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Full name helper (not stored in DB)
        [JsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
}