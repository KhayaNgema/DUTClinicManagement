using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace HospitalManagement.Services
{
    public class ReceiveMedication
    {
        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        private readonly QrCodeService _qrCodeService;
        public readonly SmsService _smsService;

        public ReceiveMedication(DUTClinicManagementDbContext context,
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

        public async Task NotifyPatientDeliveringAsync(string encryptedDeliveryId)
        {
            int deliveryId = int.Parse(_encryptionService.Decrypt(encryptedDeliveryId));
            var delivery = await _context.DeliveryRequests
                .Include(o => o.Patient)
                .FirstOrDefaultAsync(o => o.DeliveryRequestId == deliveryId);

            if (delivery == null || delivery.Patient == null)
                return;

            var patient = delivery.Patient;
            var phone = patient.PhoneNumber;
            var email = patient.Email;

            string baseUrl = "https://20.164.17.133:2001";
            string encodedDeliveryId = WebUtility.UrlEncode(encryptedDeliveryId);
            string receiveMedicationLink = $"{baseUrl}/Deliveries/ReceiveDelivery?deliveryRequestId={encodedDeliveryId}";

            string smsMessage = $"Dear {patient.FirstName}, the driver delivering your medication has dispatched. " +
                                $"Please use this link to receive your medication: {receiveMedicationLink}";

            string emailMessage = $@"Dear {patient.FirstName}, the driver delivering your medication has dispatched. 
                            Please <a href=""{receiveMedicationLink}""> receive your medication using this link</a>.";

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
                        subject: "Your Medication is being delivered",
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
