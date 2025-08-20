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
            try
            {
                var today = DateTime.Today;
                var targetDates = new[] { today.AddDays(5), today.AddDays(2), today };

                var appointments = await _context.FollowUpAppointments
                    .Include(f => f.OrignalBooking)
                        .ThenInclude(o => o.CreatedBy)
                    .Include(f => f.AssignedTo)
                    .Where(f => targetDates.Contains(f.BookForDate.Date))
                    .ToListAsync();

                foreach (var appointment in appointments)
                {
                    var patient = appointment.CreatedBy;
                    var assignedTo = appointment.AssignedTo;
                    if (patient == null)
                        continue;
                    if (assignedTo == null)
                        continue;

                    bool hasUpcomingAppointments = await _context.FollowUpAppointments
                        .AnyAsync(f => f.OrignalBooking.PatientId == patient.Id &&
                                       f.BookForDate.Date >= today);
                    if (!hasUpcomingAppointments)
                        continue;

                    if (!DiseaseToDiseaseTypeMap.Map.TryGetValue(appointment.Disease, out var diseaseType))
                        continue;
                    if (diseaseType != DiseaseType.Chronic)
                        continue;

                    bool reminderExists = await _context.Reminders
                        .AnyAsync(r =>
                            r.FollowUpAppointmentBookingId == appointment.BookingId &&
                            r.Status == ReminderStatus.Sent &&
                            r.ExpiryDate.Date == appointment.BookForDate.Date);
                    if (reminderExists)
                        continue;

                    string link = "https://localhost:7175/Appointments/MyAppointments";
                    var daysLeft = (appointment.BookForDate.Date - today).Days;
                    string dayDescription = daysLeft switch
                    {
                        5 => "in 5 days",
                        2 => "in 2 days",
                        0 => "today",
                        _ => $"on {appointment.BookForDate:dd MMM yyyy}"
                    };

                    string message =
                        $"Dear {patient.FirstName} {patient.LastName}, you have a follow-up appointment scheduled {dayDescription} on {appointment.BookForDate:dd MMM yyyy} " +
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
                        SentDate = DateTime.Now,
                        ExpiryDate = appointment.BookForDate.Date,
                        Status = ReminderStatus.Sent,
                        ReminderMessage = message,
                        FollowUpAppointmentBookingId = appointment.BookingId
                    };
                    _context.Reminders.Add(reminder);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }
    }
}
