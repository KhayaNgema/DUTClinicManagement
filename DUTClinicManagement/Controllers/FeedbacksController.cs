using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUTClinicManagement.Controllers
{
    public class FeedbacksController : Controller
    {
        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        private readonly DeviceInfoService _deviceInfoService;
        private readonly FileUploadService _fileUploadService;
        private readonly EmergencyPriorityService _priorityService;
        private readonly FeedbackService _feedbackService;

        public FeedbacksController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            FileUploadService fileUploadService,
            EmergencyPriorityService priorityService,
            DeviceInfoService deviceInfoService,
            FeedbackService feedbackService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _deviceInfoService = deviceInfoService;
            _priorityService = priorityService;
            _feedbackService = feedbackService;
        }

        [HttpGet]
        public async Task<IActionResult> Feedback()
        {
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Doctor)
                .Include(f => f.Nurse)
                .Include(f => f.Patient)
                .ToListAsync();

            var viewModelList = feedbacks.Select(f => new FeedbackDisplayViewModel
            {
                StaffName = f.Doctor != null
                     ? f.Doctor.FirstName + " " + f.Doctor.LastName
                     : f.Nurse != null
                     ? f.Nurse.FirstName + " " + f.Nurse.LastName
                     : "Unknown",

                StaffEmail = f.Doctor != null ? f.Doctor.Email : f.Nurse?.Email,
                StaffPhone = f.Doctor != null ? f.Doctor.PhoneNumber : f.Nurse?.PhoneNumber,
                Occupation = f.Doctor != null ? "Doctor" : "Nurse",

                CommunicationRating = f.CommunicationRating,
                ProfessionalismRating = f.ProfessionalismRating,
                CareSatisfactionRating = f.CareSatisfactionRating,

                Comments = f.Comments,
                SubmittedOn = f.SubmittedOn
            }).ToList();

            return View(viewModelList);
        }


        public async Task<IActionResult> LoadFeedbackForm()
        {
            var user = await _userManager.GetUserAsync(User);
            var pendingFeedback = await _feedbackService.GetPendingFeedbackAsync(user.Id);

            if (pendingFeedback == null)
                return NoContent(); 

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == pendingFeedback.BookingId);

            if (booking == null)
                return NoContent();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == booking.AssignedUserId);
            var nurse = await _context.Nurses.FirstOrDefaultAsync(n => n.Id == booking.AssignedUserId);

            var viewModel = new FeedbackFormViewModel
            {
                BookingId = booking.BookingId,
                SelectedDoctorId = doctor?.Id,
                SelectedNurseId = nurse?.Id,
                Occupation = doctor != null ? "Doctor" : nurse != null ? "Nurse" : null
            };

            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            ViewBag.Nurses = await _context.Nurses.ToListAsync();

            return PartialView("_SubmitFeedbackPartial", viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(FeedbackFormViewModel viewModel)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == user.Id);

                if (patient == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unable to identify patient record."
                    });
                }

                var feedback = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.PatientId == patient.Id && f.BookingId == viewModel.BookingId);

                if (feedback == null)
                {
                    feedback = new Feedback
                    {
                        PatientId = patient.Id,
                        BookingId = viewModel.BookingId,
                        SubmittedOn = DateTime.UtcNow
                    };
                    _context.Feedbacks.Add(feedback);
                }
                else
                {
                    feedback.SubmittedOn = DateTime.UtcNow;
                }

                feedback.CommunicationRating = viewModel.CommunicationRating;
                feedback.ProfessionalismRating = viewModel.ProfessionalismRating;
                feedback.CareSatisfactionRating = viewModel.HelpfulnessRating;
                feedback.Comments = viewModel.Comments;

                if (viewModel.Occupation == "Doctor" && !string.IsNullOrEmpty(viewModel.SelectedDoctorId))
                {
                    feedback.DoctorId = viewModel.SelectedDoctorId;
                    feedback.NurseId = null;
                }
                else if (viewModel.Occupation == "Nurse" && !string.IsNullOrEmpty(viewModel.SelectedNurseId))
                {
                    feedback.NurseId = viewModel.SelectedNurseId;
                    feedback.DoctorId = null;
                }
                else
                {
                    feedback.DoctorId = null;
                    feedback.NurseId = null;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Feedback submitted successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to submit feedback: " + ex.Message,
                    errorDetails = new
                    {
                        InnerException = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }
        }
    }
}
