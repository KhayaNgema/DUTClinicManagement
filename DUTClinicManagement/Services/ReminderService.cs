using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DUTClinicManagement.Services
{
    public class ReminderService
    {
        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        public readonly SmsService _smsService;

        public ReminderService(
            DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            SmsService smsService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task SendRemindersAsync()
        {
            var today = DateTime.Today;
            var targetDate = today.AddDays(2);

            var appointments = await _context.FollowUpAppointments
                .Include(f => f.OrignalBooking)
                    .ThenInclude(b => b.Patient)
                .Include(b => b.AssignedTo)
                .Where(f => f.BookForDate.Date == targetDate.Date) 
                .ToListAsync();

            foreach (var appointment in appointments)
            {
                var patient = appointment.OrignalBooking?.Patient;
                var assignedTo = appointment.AssignedTo;

                if (patient == null)
                    continue;

                if (!DiseaseToDiseaseTypeMap.Map.TryGetValue(appointment.Disease, out var diseaseType))
                    continue;

                if (diseaseType != DiseaseType.Chronic)
                    continue;

                bool reminderExists = await _context.Reminders
                    .AnyAsync(r =>
                        r.BookingId == appointment.BookingId &&
                        r.Status == ReminderStatus.Sent &&
                        r.ExpiryDate.Date == targetDate.Date);

                if (reminderExists)
                    continue;

                string link = "https://localhost:7175/Appointments/MyAppointments";
                string message =
                    $"Dear {patient.FirstName} {patient.LastName}, you have a follow-up appointment scheduled in 2 days on {appointment.BookForDate:dd MMM yyyy} " +
                    $"with Doctor/Nurse {assignedTo.FirstName} {assignedTo.LastName}. " +
                    $"Please confirm or reschedule your appointment by visiting: {link}";

                if (!string.IsNullOrEmpty(patient.PhoneNumber))
                {
                    try
                    {
                        await _smsService.SendSmsAsync(patient.PhoneNumber, message);
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(patient.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            to: patient.Email,
                            subject: "Follow-up Appointment Reminder",
                            body: message,
                            senderName: "DUT Clinic"
                        );
                    }
                    catch { }
                }

                var reminder = new Reminder
                {
                    BookingId = appointment.BookingId,
                    SentDate = DateTime.Now,
                    ExpiryDate = targetDate,
                    Status = ReminderStatus.Sent,
                    ReminderMessage = message
                };

                _context.Reminders.Add(reminder);
            }

            await _context.SaveChangesAsync(); // ✅ Save once at the end
        }
    }
}
