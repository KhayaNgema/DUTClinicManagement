using System.ComponentModel.DataAnnotations;

namespace DUTClinicManagement.Models
{
    public class Doctor : UserBaseModel
    {
        [Required(ErrorMessage = "Specialization is required.")]
        public Specialization Specialization { get; set; }

        [Required(ErrorMessage = "Medical license number is required.")]
        [Display(Name = "License Number")]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Display(Name = "Years of Experience")]
        [Range(0, 60, ErrorMessage = "Experience must be between 0 and 60 years.")]
        public int YearsOfExperience { get; set; }

        [Display(Name = "Education")]
        [StringLength(200)]
        public string? Education { get; set; }

        [Display(Name = "Biography / About")]
        [StringLength(1000)]
        public string? Biography { get; set; }

        public Department Department { get; set; }
    }
}
