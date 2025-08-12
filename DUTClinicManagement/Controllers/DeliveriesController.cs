using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using Hangfire;
using HospitalManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUTClinicManagement.Controllers
{
    public class DeliveriesController : Controller
    {
        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        private readonly DeviceInfoService _deviceInfoService;
        private readonly FileUploadService _fileUploadService;
        private readonly FeedbackService _feedbackService;
        private readonly QrCodeService _qrCodeService;
        private readonly ReceiveMedication _receiveMedication;

        public DeliveriesController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            FileUploadService fileUploadService,
            DeviceInfoService deviceInfoService,
            FeedbackService feedbackService,
            QrCodeService qrCodeService,
            ReceiveMedication receiveMedication)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _deviceInfoService = deviceInfoService;
            _feedbackService = feedbackService;
            _qrCodeService = qrCodeService;
            _receiveMedication = receiveMedication;
        }

        [HttpGet]
        public async Task<IActionResult> DeliveryRequests()
        {
            var requests = await _context.DeliveryRequests
                .Include(i => i.MedicationPescription)
                    .ThenInclude(mp => mp.PrescribedMedication)
                .Include(i => i.Patient)
                .Include(i => i.ModifiedBy)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

            return View(requests);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyDeliveryRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            var deliveryRequests = await _context.DeliveryRequests
                .Where(dr => dr.PatientId == user.Id)
                .Include(dr => dr.MedicationPescription)
                .ThenInclude(dr => dr.PrescribedMedication)
                .Include(dr => dr.Patient)
                .ToListAsync();

            return View(deliveryRequests);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ReceiveDelivery(string deliveryRequestId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var decryptedDeliveryId = _encryptionService.DecryptToInt(deliveryRequestId);

            var delivery = await _context.DeliveryRequests
                .Where(p => p.DeliveryRequestId == decryptedDeliveryId && p.Patient.Id == user.Id)
                .FirstOrDefaultAsync();

            if (delivery == null)
            {
                return RedirectToAction("Home", "Error");
            }

            var viewModel = new ReceiveDeliveryViewModel
            {
                DeliveryId = delivery.DeliveryRequestId
            };

            return View(viewModel);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmDelivery(string qrCodeNumber, int deliveryId)
        {
            if (string.IsNullOrEmpty(qrCodeNumber))
                return Json(new { success = false, message = "QR code cannot be empty. Please scan a valid code." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User authentication required. Please log in." });

            var delivery = await _context.DeliveryRequests
                .FirstOrDefaultAsync(d => d.DeliveryRequestReference == qrCodeNumber);
            if (delivery == null)
                return Json(new { success = false, message = "No delivery found for the scanned QR code." });

            if (delivery.PatientId != user.Id)
                return Json(new { success = false, message = "Access denied: You are not authorized to receive this delivery." });

            if (delivery.DeliveryRequestId != deliveryId)
                return Json(new { success = false, message = "The QR code you scanned does not match the expected delivery. Please use the correct scanner." });

            if (delivery.PatientId == user.Id && delivery.Status == DeliveryRequestStatus.Collected)
                return Json(new { success = false, message = "This delivery has already been collected. For assistance, please contact your pharmacist." });

            delivery.Status = DeliveryRequestStatus.Collected;
            _context.Update(delivery);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Package verified successfully! Thank you for confirming delivery." });
        }




        [HttpGet]
        [Authorize(Roles = "Delivery Personnel")]
        public async Task<IActionResult> PendingDeliveries()
        {
            var deliveryRequests = await _context.DeliveryRequests
                .Where(dr => dr.Status == DeliveryRequestStatus.Prepared ||
                 dr.Status == DeliveryRequestStatus.Delivering)
                .Include(dr => dr.Patient)
                 .Include(dr => dr.MedicationPescription)
                 .ThenInclude(dr => dr.PrescribedMedication)
                .ToListAsync();

            return View(deliveryRequests);
        }


        [HttpPost]
        public async Task<IActionResult> StartDelivery(int deliveryRequestId)
        {
            var deliveryRequest = await _context.DeliveryRequests
                .Where(dr => dr.DeliveryRequestId == deliveryRequestId)
                .FirstOrDefaultAsync();

            deliveryRequest.Status = DeliveryRequestStatus.Delivering;
            deliveryRequest.LastUpdatedAt = DateTime.Now;

            _context.Update(deliveryRequest);
            await _context.SaveChangesAsync();

            var encryptedDeliveryId = _encryptionService.Encrypt(deliveryRequestId);

            BackgroundJob.Enqueue(() => _receiveMedication.NotifyPatientDeliveringAsync(encryptedDeliveryId));

            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> PrepareDelivery(int deliveryRequestId)
        {
            var delivery = await _context.DeliveryRequests
                .Where(d => d.DeliveryRequestId == deliveryRequestId)
                .FirstOrDefaultAsync();

            delivery.Status = DeliveryRequestStatus.Prepared;
            delivery.LastUpdatedAt = DateTime.Now;

            _context.Update(delivery);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"You have successfully prepared the delivery with reference number: {delivery.DeliveryRequestReference}. The responsible driver have been alerted.";

            return RedirectToAction(nameof(DeliveryRequests));
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> NewDeliveryRequest(MedicationCollectionViewModel viewModel)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var pescription = await _context.MedicationPescription
                    .Include(mp => mp.PrescribedMedication)
                    .Where(mp => mp.MedicationPescriptionId == viewModel.MedicationPescriptionId)
                    .FirstOrDefaultAsync();

                var referenceNumber = await GenerateDeliveryReferenceNumber();

                var newDeliveryRequest = new DeliveryRequest
                {
                    PatientId = user.Id,
                    Address = $"{viewModel.Street}, {viewModel.City}, {viewModel.Province}, {viewModel.PostalCode}, {viewModel.Country}",
                    MedicationPescriptionId = viewModel.MedicationPescriptionId,
                    LastUpdatedAt = DateTime.Now,
                    Status = DeliveryRequestStatus.Pending,
                    CreatedAt = DateTime.Now,
                    DeliveryRequestReference = referenceNumber
                };

                _context.DeliveryRequests.Add(newDeliveryRequest);
                await _context.SaveChangesAsync();

                newDeliveryRequest.QrCodeImage = _qrCodeService.GenerateQrCode(referenceNumber);
                _context.Update(newDeliveryRequest);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"You have successfully requested your medication to be delivered to your location.";

                return RedirectToAction(nameof(MyDeliveryRequests));
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to make a delivery request: " + ex.Message,
                    errorDetails = new
                    {
                        InnerException = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }
        }

        private async Task<string> GenerateDeliveryReferenceNumber()
        {

            const string numbers = "0123456789";
            const string fineLetters = "D";
            const string lastLetters = "25";

            var random = new Random();
            var randomNumbers = new string(Enumerable.Repeat(numbers, 7)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            return $"{fineLetters}{randomNumbers}{lastLetters}";
        }
    }
}
