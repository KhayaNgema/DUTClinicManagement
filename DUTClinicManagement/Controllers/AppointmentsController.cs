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

        [Authorize(Roles = "Nurse, System Administrator")]
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

            if (userRoles.Contains("System Administrator"))
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
            else if (userRoles.Contains("Nurse"))
            {
                var nurseAppointments = await query
                    .Where(a =>
                        a.AssignedUserId == user.Id &&
                        (a.Status == BookingStatus.Assigned || a.Status == BookingStatus.Completed))
                    .OrderBy(a => a.BookForDate)
                    .ThenBy(a => a.BookForTimeSlot)
                    .ToListAsync();

                return View(nurseAppointments);
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
                .Include(a => a.AssignedTo)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound();
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

                var availableNurse = await _context.Nurses
                    .Where(n => !_context.Bookings.Any(b =>
                                b.AssignedUserId == n.Id &&
                                b.BookForDate == viewModel.BookForDate &&
                                b.BookForTimeSlot == viewModel.BookForTimeSlot))
                    .FirstOrDefaultAsync();

                if (availableNurse == null)
                {
                    viewModel.AvailableTimeSlots = GetTimeSlotsByDate(viewModel.BookForDate);
                    return Json(new
                    {
                        success = false,
                        message = "No available nurse found for the selected time slot."
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
                    AssignedUserId = availableNurse.Id
                };

                _context.Add(newAppointment);
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

                return RedirectToAction("Index", "Home");
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

        private string GenerateBookingReferenceNumber()
        {
            var year = DateTime.Now.Year.ToString().Substring(2);
            var month = DateTime.Now.Month.ToString("D2");
            var day = DateTime.Now.Day.ToString("D2");

            const string numbers = "0123456789";
            const string bookingLetters = "REF";

            var random = new Random();
            var randomNumbers = new string(Enumerable.Repeat(numbers, 4)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            var bookLetters = bookingLetters.ToString();

            return $"{year}{month}{day}{randomNumbers}{bookLetters}";
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

            foreach (var slot in allSlots)
            {
                string slotValue = slot.From.ToString(@"hh\:mm");

                bool isSlotBooked = _context.Bookings
                    .Any(b => b.BookForDate.Date == date.Date && b.BookForTimeSlot == slotValue);

                if (!isSlotBooked)
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
