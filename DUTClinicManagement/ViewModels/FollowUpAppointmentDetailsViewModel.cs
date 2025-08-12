using DUTClinicManagement.Models;

namespace DUTClinicManagement.ViewModels
{
    public class FollowUpAppointmentDetailsViewModel
    {
        public int BookingId { get; set; }

        public int OriginalBookingId { get; set; }

        public string BookingReference { get; set; }

        public string PatientId { get; set; }
        public string PatientFullNames { get; set; }

        public string IdNumber { get; set; }

        public string PatientProfilePicture { get; set; }

        public string PatientEmail { get; set; }

        public string PhoneNumber { get; set; }


        public string DoctorId { get; set; }

        public string DoctorNurseNames { get; set; }

        public DateTime BookForDate { get; set; }

        public string BookForTimeSlot { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdatedAt { get; set; }

        public BookingStatus Status { get; set; }

        public CommonMedicalCondition MedicalCondition { get; set; }

        public string AdditionalNotes { get; set; }
        public ICollection<string>? Instructions { get; set; } = new List<string>();

        public string AssignedToFullNames { get; set; }

        public Disease Disease { get; set; }
    }
}
