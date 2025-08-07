using DUTClinicManagement.Models;

namespace DUTClinicManagement.Interfaces
{
    public interface IFeedbackService
    {
        Task<bool> HasPendingFeedbackAsync(string patientId);
        Task FlagFeedbackPendingAsync(int appointmentId, string patientId);
        Task<Feedback> GetPendingFeedbackAsync(string patientId);
    }
}
