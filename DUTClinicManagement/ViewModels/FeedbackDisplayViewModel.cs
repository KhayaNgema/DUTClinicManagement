using DUTClinicManagement.Models;

namespace DUTClinicManagement.ViewModels
{
    public class FeedbackDisplayViewModel
    {
        public string Occupation {  get; set; }
        public string StaffName { get; set; }
        public string StaffEmail { get; set; }
        public string StaffPhone { get; set; }
        public Rating CommunicationRating { get; set; }
        public Rating ProfessionalismRating { get; set; }
        public Rating CareSatisfactionRating { get; set; }
        public string Comments { get; set; }
        public DateTime SubmittedOn { get; set; }
    }
}
