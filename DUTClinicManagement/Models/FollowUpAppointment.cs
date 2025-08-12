using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUTClinicManagement.Models
{
    public class FollowUpAppointment : Booking
    {
        public string? NurseId { get; set; }
        [ForeignKey("NurseId")]
        public virtual Nurse Nurse { get; set; }

        public string? DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }

        public int OriginalBookingId { get; set; }
        [ForeignKey("OriginalBookingId")]
        public virtual Booking OrignalBooking { get; set; }

        [Display(Name = "Instructions/Notes")]
        public ICollection<string>? Instructions { get; set; } = new List<string>();

        [NotMapped] 
        public string InstructionsInput { get; set; }

        public NextPersonToSee NextPersonToSee { get; set; }

        public Disease Disease { get; set; }
    }

    public enum NextPersonToSee
    {
        Doctor,
        Nurse
    }

}
