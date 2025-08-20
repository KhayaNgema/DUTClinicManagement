using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace HospitalManagement.Services
{
    public class EmergencyContactService
    {
        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        private readonly QrCodeService _qrCodeService;
        public readonly SmsService _smsService;

        public EmergencyContactService(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            QrCodeService qrCodeService,
            EmailService emailService,
            SmsService smsService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _qrCodeService = qrCodeService;
            _smsService = smsService;
        }

        public async Task NotifyEmergencyContactsAsync(string encryptedRequestId)
        {
            int emergencyId = int.Parse(_encryptionService.Decrypt(encryptedRequestId));

            var emergency = await _context.EmergencyRequests
                .Include(o => o.Patient)
                .FirstOrDefaultAsync(o => o.RequestId == emergencyId);

            if (emergency == null || emergency.Patient == null)
                return;

            var patient = emergency.Patient;
            var phone = patient.EmergencyContactNumber;
            var email = patient.Email;

            string smsMessage = $"Dear {patient.EmergencyContactPerson}, please note that {patient.FirstName} {patient.LastName} is currently in an emergency health situation and has requested an ambulance. Kind regards, DUT Clinic.";

            string emailMessage = $@"<p>Dear {patient.EmergencyContactPerson},</p>
                        <p>Please note that {patient.FirstName} {patient.LastName} is currently in an emergency health situation and has requested an ambulance.</p>
                        <p>Kind regards, DUT Clinic.</p>";

            if (!string.IsNullOrEmpty(phone))
            {
                try
                {
                    await _smsService.SendSmsAsync(phone, smsMessage);
                }
                catch (Exception smsEx)
                {
                    Console.WriteLine($"Failed to send SMS to {phone}: {smsEx.Message}");
                }
            }

            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        to: email,
                        subject: "Emergency Alert: Ambulance Requested",
                        body: emailMessage,
                        senderName: "Medi Care"
                    );
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Failed to send email to {email}: {emailEx.Message}");
                }
            }
        }
    }
}
