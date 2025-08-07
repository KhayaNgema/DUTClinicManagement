using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DUTClinicManagement.Controllers
{
    public class EmergenciesController : Controller
    {
        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        private readonly DeviceInfoService _deviceInfoService;
        private readonly FileUploadService _fileUploadService;
        private readonly EmergencyPriorityService _priorityService;

        public EmergenciesController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            FileUploadService fileUploadService,
            EmergencyPriorityService priorityService,
            DeviceInfoService deviceInfoService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _deviceInfoService = deviceInfoService;
            _priorityService = priorityService;

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyEmergencyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EmergencyRequest()
        {
            var user = await _userManager.GetUserAsync(User);

            var viewModel = new EmergencyRequestViewModel
            {
                PatientId = user.Id
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EmergencyRequest(EmergencyRequestViewModel viewModel)
        {
            try
            {
                var priority = _priorityService.AnalyzePriority(viewModel.EmergencyDetails);

                var emergencyRequest = new EmergencyRequest
                {
                    PatientId = viewModel.PatientId,
                    EmergencyDetails = viewModel.EmergencyDetails,
                    ModifiedById = viewModel.PatientId,
                    ModifiedDateTime = DateTime.Now,
                    RequestLocation = viewModel.RequestLocation,
                    RequestTime = DateTime.Now,
                    RequestStatus = RequestStatus.Pending,
                    Priority = priority
                };

                _context.Add(emergencyRequest);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"You have successfully requested an emergency assistance. An ambulance is on the way. Priority: {priority}. Please stay calm and monitor your updates.";

                return RedirectToAction(nameof(MyEmergencyRequests));
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to request emergency: " + ex.Message,
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
