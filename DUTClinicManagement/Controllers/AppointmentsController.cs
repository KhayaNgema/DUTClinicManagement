using Hangfire;
using Hangfire.Dashboard;
using DUTClinicManagement.Data;
using DUTClinicManagement.Helpers;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Web;

namespace DUTClinicManagement.Controllers
{
    public class AppointmentsController : Controller
    {

        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        private readonly DeviceInfoService _deviceInfoService;
        private readonly FileUploadService _fileUploadService;

        public AppointmentsController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            FileUploadService fileUploadService,
            DeviceInfoService deviceInfoService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _deviceInfoService = deviceInfoService;

        }

        [Authorize(Roles = "Nurse, Doctor, Receptionist, System Administrator")]
        [HttpGet]
        public async Task<IActionResult> Appointments()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            IQueryable<Booking> query = _context.Bookings
                .Include(a => a.Patient)
                .Include(a => a.CreatedBy)
                .Include(a => a.AssignedTo)
                .Include(a => a.ModifiedBy);

            if (userRoles.Contains("System Administrator") || userRoles.Contains("Receptionist"))
            {
                var allAppointments = await query
                    .Where(ap => ap.Status == BookingStatus.Awaiting ||
                    ap.Status == BookingStatus.Assigned ||
                    ap.Status == BookingStatus.Completed)
                    .OrderBy(ap => ap.BookForDate)
                    .ThenBy(ap => ap.BookForTimeSlot)
                    .ToListAsync();

                return View(allAppointments);
            }
            else if (userRoles.Contains("Doctor"))
            {
                var doctor = await _context.Doctors
                    .Where(d => d.Id == user.Id)
                    .FirstOrDefaultAsync();

                var doctorSpecialization = doctor.Specialization;

                var allowedConditions = ConditionToSpecializationsMap.Map
                    .Where(entry => entry.Value.Contains(doctorSpecialization))
                    .Select(entry => entry.Key)
                    .ToList();

                var filteredAppointments = await query
                    .Where(a =>
                        allowedConditions.Contains(a.MedicalCondition) &&
                        (a.Status == BookingStatus.Assigned && a.AssignedUserId == user.Id))
                    .Include(a => a.AssignedTo)
                    .OrderBy(a => a.BookForDate)
                    .ThenBy(a => a.BookForTimeSlot)
                    .ToListAsync();

                return View(filteredAppointments);
            }

            return Forbid();
        }


        

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var myAppointments = await _context.Bookings
                .Include(ma => ma.AssignedTo)
                .Where(ma => ma.CreatedById == user.Id)
                .OrderByDescending(ma => ma.CreatedAt)
                .ToListAsync(); 
                
            return View(myAppointments);
        }

        [Authorize(Roles = "Patient, Doctor, System Administrator")]
        [HttpGet]
        public async Task<IActionResult> AppointmentDetails(string appointmentId)
        {
            var decryptedAppointmentId = _encryptionService.DecryptToInt(appointmentId);

            var appointment = await _context.Bookings
                .Where(a => a.BookingId == decryptedAppointmentId)
                .Include(a => a.CreatedBy)
                .Include(a => a.ModifiedBy)
                .Include(a => a.ModifiedBy)
                .Include(a => a.AssignedTo)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound();
            }

            var condition = appointment.MedicalCondition;

            if (ConditionToSpecializationsMap.Map.TryGetValue(condition, out var specializations))
            {
                ViewBag.AssignedTeam = specializations;
            }
            else
            {
                ViewBag.AssignedTeam = new List<Specialization>();
            }

            return View(appointment);
        }

      

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MakeAppointment(DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            var selectedDate = date ?? DateTime.Today;

            var availableSlots = new List<SelectListItem>();

            var viewModel = new MakeAppointmentViewModel
            {
                PatientId = user.Id,
                BookForDate = selectedDate,
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAppointment(MakeAppointmentViewModel viewModel)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var activeBookings = await _context.Bookings
                    .Where(ab => ab.CreatedById == user.Id &&            
                                 ab.Status == BookingStatus.Assigned && 
                                 ab.MedicalCondition == viewModel.MedicalCondition)
                    .ToListAsync();

                if (activeBookings.Any())
                {
                    TempData["Message"] = $"You cannot book another appointment while you have incomplete appointments for the same medical condition. " +
                        $"Please visit your appointments section to see all your active appointments and cancel them if you want to book a new appointment.";

                    return View(viewModel);
                }


                var bookingReference = GenerateBookingReferenceNumber();
                var deviceInfo = await _deviceInfoService.GetDeviceInfo();

                var condition = viewModel.MedicalCondition;

                if (!ConditionToSpecializationsMap.Map.TryGetValue(condition, out var requiredSpecializations))
                {
                    viewModel.AvailableTimeSlots = GetTimeSlotsByDate(viewModel.BookForDate);
                    return Json(new
                    {
                        success = false,
                        message = "No specialization mapped to the selected condition."
                    });
                }

                var availableDoctor = await _context.Doctors
                    .Where(d => requiredSpecializations.Contains(d.Specialization)
                        && !_context.Bookings.Any(b =>
                            b.AssignedUserId == d.Id &&
                            b.BookForDate == viewModel.BookForDate &&
                            b.BookForTimeSlot == viewModel.BookForTimeSlot))
                    .FirstOrDefaultAsync();

                if (availableDoctor == null)
                {
                    viewModel.AvailableTimeSlots = GetTimeSlotsByDate(viewModel.BookForDate);
                    return Json(new
                    {
                        success = false,
                        message = "No available doctor found for the selected condition and time slot."
                    });
                }


                var newAppointment = new Booking
                {
                    PatientId = viewModel.PatientId,
                    BookForDate = viewModel.BookForDate,
                    MedicalCondition = viewModel.MedicalCondition,
                    AdditionalNotes = viewModel.AdditionalNotes,
                    Status = BookingStatus.Pending,
                    CreatedById = user.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedById = user.Id,
                    LastUpdatedAt = DateTime.Now,
                    BookingReference = bookingReference,
                    BookForTimeSlot = viewModel.BookForTimeSlot,
                    AssignedUserId = availableDoctor.Id  
                };

                _context.Add(newAppointment);
                await _context.SaveChangesAsync();

                _context.Update(newAppointment);
                await _context.SaveChangesAsync();

                var patientMedicalRecord = await _context.PatientMedicalHistories
                    .FirstOrDefaultAsync(pmr => pmr.PatientId == user.Id);

                if (patientMedicalRecord != null)
                {
                    patientMedicalRecord.AccessCode = newAppointment.BookingReference;
                    patientMedicalRecord.QrCodeImage = newAppointment.QrCodeImage;
                    _context.Update(patientMedicalRecord);
                    await _context.SaveChangesAsync();
                }

                var newPayment = new Payment
                {
                    ReferenceNumber = GeneratePaymentReferenceNumber(),
                    PaymentMethod = PaymentMethod.Credit_Card,
                    AmountPaid = 90,
                    PaymentDate = DateTime.Now,
                    PaymentMadeById = user.Id,
                    Status = PaymentPaymentStatus.Unsuccessful,
                    DeviceInfoId = deviceInfo.DeviceInfoId,
                };

                _context.Add(newPayment);
                await _context.SaveChangesAsync();

                var appointment = await _context.Bookings
                    .Include(a => a.CreatedBy)
                    .FirstOrDefaultAsync(a => a.BookingId == newAppointment.BookingId);

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == newPayment.PaymentId);

                var encryptedAppointmentId = _encryptionService.Encrypt(appointment.BookingId);
                int paymentId = payment.PaymentId;
                decimal amount = newPayment.AmountPaid;
                string appointmentId = encryptedAppointmentId;

                var returnUrl = Url.Action("PayFastReturn", "Appointments", new { paymentId, appointmentId, amount }, Request.Scheme);
                returnUrl = HttpUtility.UrlEncode(returnUrl);
                var cancelUrl = "https://102.37.16.88:2002/Appointments/MyAppointments";

                string paymentUrl = await GeneratePayFineFastPaymentUrl(paymentId, amount, appointmentId, returnUrl, cancelUrl);

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                viewModel.AvailableTimeSlots = GetTimeSlotsByDate(viewModel.BookForDate);
                return Json(new
                {
                    success = false,
                    message = "Failed to make an appointment: " + ex.Message,
                    errorDetails = new
                    {
                        InnerException = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }
        }

        private List<SelectListItem> GetTimeSlotsByDate(DateTime date)
        {
            var slots = TimeSlotGenerator.GenerateDefaultSlots(date);

            var selectList = slots.Select(slot =>
            {
                string fromText = DateTime.Today.Add(slot.From).ToString("HH:mm");
                string toText = DateTime.Today.Add(slot.To).ToString("HH:mm");


                return new SelectListItem
                {
                    Value = slot.From.ToString(@"hh\:mm"),
                    Text = $"{fromText} - {toText}"
                };
            }).ToList();

            return selectList;
        }

        [HttpGet]
        public JsonResult GetAvailableTimeSlots(DateTime date, CommonMedicalCondition condition)
        {
            var allSlots = TimeSlotGenerator.GenerateDefaultSlots(date);
            var availableSlots = new List<SelectListItem>();

            if (!ConditionToSpecializationsMap.Map.TryGetValue(condition, out var requiredSpecializations))
            {
                return Json(new List<object>()); 
            }

            foreach (var slot in allSlots)
            {
                string slotValue = slot.From.ToString(@"hh\:mm");

                var specializedDoctors = _context.Doctors
                    .Where(d => requiredSpecializations.Contains(d.Specialization))
                    .ToList();

                if (!specializedDoctors.Any())
                {
                    continue;
                }

                var bookedDoctorIds = _context.Bookings
                    .Where(b => b.BookForDate.Date == date.Date && b.BookForTimeSlot == slotValue)
                    .Select(b => b.AssignedUserId)
                    .ToList();

                var availableDoctors = specializedDoctors
                    .Where(d => !bookedDoctorIds.Contains(d.Id))
                    .ToList();

                if (availableDoctors.Any())
                {
                    string fromText = DateTime.Today.Add(slot.From).ToString("HH:mm");
                    string toText = DateTime.Today.Add(slot.To).ToString("HH:mm");

                    availableSlots.Add(new SelectListItem
                    {
                        Value = slotValue,
                        Text = $"{fromText} - {toText}"
                    });
                }
            }

            var freeSlots = availableSlots
                .Select(slot => new { value = slot.Value, text = slot.Text })
                .ToList();

            return Json(freeSlots);
        }
    }
}
