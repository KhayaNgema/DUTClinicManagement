using DUTClinicManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace DUTClinicManagement.ViewModels
{
    public class FeedbackFormViewModel
    {
        public int BookingId { get; set; }

        [Required]
        public string Occupation { get; set; }

        public string? SelectedDoctorId { get; set; }
        public string? SelectedNurseId { get; set; }

        [Required]
        public Rating CommunicationRating { get; set; }

        [Required]
        public Rating ProfessionalismRating { get; set; }

        [Required]
        public Rating HelpfulnessRating { get; set; }

        public string Comments {  get; set; }
    }
}
