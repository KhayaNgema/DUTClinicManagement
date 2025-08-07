using System.ComponentModel.DataAnnotations.Schema;

namespace DUTClinicManagement.Models
{
    public class Feedback
    {
            public int FeedbackId { get; set; }

            public string PatientId { get; set; }
            [ForeignKey("PatientId")]
            public Patient Patient { get; set; }

            public string? DoctorId { get; set; }
            [ForeignKey("DoctorId")]
            public Doctor Doctor { get; set; }

            public string? NurseId { get; set; }
            [ForeignKey("NurseId")]
            public Nurse Nurse { get; set; }

            public int BookingId { get; set; }
            public virtual Booking Booking { get; set; }

            public Rating CommunicationRating { get; set; }

            public Rating ProfessionalismRating { get; set; }

            public Rating CareSatisfactionRating { get; set; }

            public string? Comments { get; set; }

            public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;
        }

    }

