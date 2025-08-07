using DUTClinicManagement.Data;
using DUTClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class FeedbackService
{
    private readonly DUTClinicManagementDbContext _context;

    public FeedbackService(DUTClinicManagementDbContext context)
    {
        _context = context;
    }

    // Check if the patient has any completed appointment without feedback submitted
    public async Task<bool> HasPendingFeedbackAsync(string patientId)
    {
        // Completed bookings for patient without feedback record
        return await _context.Bookings
            .Where(b => b.PatientId == patientId && b.Status == BookingStatus.Completed)
            .AnyAsync(b => !_context.Feedbacks.Any(f => f.PatientId == patientId && f.FeedbackId == b.BookingId));
    }

    // Mark feedback as pending by creating empty feedback record if none exists for appointment
    public async Task FlagFeedbackPendingAsync(int appointmentId, string patientId)
    {
        var feedbackExists = await _context.Feedbacks.AnyAsync(f => f.FeedbackId == appointmentId);
        if (!feedbackExists)
        {
            var feedback = new Feedback
            {
                BookingId = appointmentId,
                PatientId = patientId,
                CommunicationRating = Rating.NotRated,
                ProfessionalismRating = Rating.NotRated,
                CareSatisfactionRating = Rating.NotRated,
                Comments = null,
                SubmittedOn = DateTime.MinValue 
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Feedback> GetPendingFeedbackAsync(string patientId)
    {
        return await _context.Feedbacks
            .Where(f => f.PatientId == patientId && f.SubmittedOn == DateTime.MinValue)
            .OrderByDescending(f => f.FeedbackId)
            .FirstOrDefaultAsync();
    }
}
