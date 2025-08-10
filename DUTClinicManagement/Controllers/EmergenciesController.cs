using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly ParamedicAssignmentService _paramedicAssignmentService;

        public EmergenciesController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            FileUploadService fileUploadService,
            EmergencyPriorityService priorityService,
            DeviceInfoService deviceInfoService,
            ParamedicAssignmentService paramedicAssignmentService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _deviceInfoService = deviceInfoService;
            _priorityService = priorityService;
            _paramedicAssignmentService = paramedicAssignmentService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyEmergencyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            return View();
        }

        [Authorize(Roles ="Paramedic")]
        [HttpGet]
        public async Task<IActionResult> EmergencyRequests()
        {
            var emergencyRequests = await _context.EmergencyRequests
                .Include(er=>er.Patient)
                .OrderByDescending(er => er.Priority)
                .ToListAsync();

            return View(emergencyRequests);
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DriveToLocation(int requestId, double latitude, double longitude)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var emergencyRequest = await _context.EmergencyRequests.FindAsync(requestId);
                if (emergencyRequest == null)
                    return NotFound(new { success = false, message = "Emergency request not found." });

                emergencyRequest.ParamedicLatitude = latitude;
                emergencyRequest.ParamedicLongitude = longitude;
                emergencyRequest.RequestStatus = RequestStatus.Departed;
                emergencyRequest.ModifiedDateTime = DateTime.UtcNow;
                emergencyRequest.ModifiedById = user.Id;

                _context.EmergencyRequests.Update(emergencyRequest);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Drive started successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                // Return detailed inner exception message for debugging (remove or log in production)
                var innerMessage = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                return StatusCode(500, new { success = false, message = $"Database update error: {innerMessage}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }




        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EmergencyRequest(EmergencyRequestViewModel viewModel)
        {
            try
            {
                var priority = _priorityService.AnalyzePriority(viewModel.EmergencyDetails);

                var referenceNumber = GenerateEmergencyReferenceNumber();

                string assignedParamedicId = await _paramedicAssignmentService.AssignSingleAvailableParamedicAsync();

                var assignedParamedicIds = new List<string>();

                if (!string.IsNullOrEmpty(assignedParamedicId))
                {
                    assignedParamedicIds.Add(assignedParamedicId);
                }

                var emergencyRequest = new EmergencyRequest
                {
                    PatientId = viewModel.PatientId,
                    EmergencyDetails = viewModel.EmergencyDetails,
                    ModifiedById = viewModel.PatientId,
                    ModifiedDateTime = DateTime.Now,
                    RequestLocation = viewModel.RequestLocation,
                    RequestTime = DateTime.Now,
                    RequestStatus = RequestStatus.Pending,
                    Priority = priority,
                    ReferenceNumber = referenceNumber,
                    AssignedParamedicIds = assignedParamedicIds,
                    PatientLatitude = viewModel.PatientLatitude,
                    PatientLongitude = viewModel.PatientLongitude,
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


        private string GenerateEmergencyReferenceNumber()
        {
            var now = DateTime.Now;

            const string prefix = "EMR";
            string datePart = now.ToString("yyMMdd");

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var randomPart = new string(Enumerable.Range(0, 5)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());

            return $"{prefix}{datePart}{randomPart}";
        }
    }
}
