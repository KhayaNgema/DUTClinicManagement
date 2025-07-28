using System.ComponentModel.DataAnnotations;

namespace DUTClinicManagement.Models
{
    public class AvailableTimeRange
    {
        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Available From")]
        public TimeSpan From { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Available To")]
        public TimeSpan To { get; set; }
    }
}
