using DUTClinicManagement.Data;
using DUTClinicManagement.Helpers;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using DUTClinicManagement.ViewModels;
using Hangfire;
using Hangfire.Dashboard;
using HospitalManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
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
        private readonly FeedbackService _feedbackService;

        public AppointmentsController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            FileUploadService fileUploadService,
            DeviceInfoService deviceInfoService,
            FeedbackService feedbackService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _deviceInfoService = deviceInfoService;
            _feedbackService = feedbackService;
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


        [Authorize(Roles = "Doctor, Nurse, System Administrator")]
        [HttpGet]
        public async Task<IActionResult> FollowUpAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var appointments = await _context.FollowUpAppointments
                .Where(a => (a.Status == BookingStatus.Completed || a.Status == BookingStatus.Assigned)
                 && a.AssignedUserId == user.Id)
                .Include(a => a.Booking)
                .ThenInclude(a=>a.CreatedBy)
                .Include(a => a.CreatedBy)
                .Include(a => a.Doctor)
                .Include(a => a.Nurse)
                .Include(a => a.ModifiedBy)
                .Include(a => a.AssignedTo)
                .ToListAsync();

            return View(appointments);
        }


        [Authorize(Roles = "Doctor, System Administrator, Nurse")]
        [HttpGet]
        public async Task<IActionResult> CompletedFollowUpAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var appointments = await _context.FollowUpAppointments
                 .Where(a => a.Status == BookingStatus.Awaiting ||
                 a.Status == BookingStatus.Assigned &&
                 a.AssignedUserId == user.Id)
                .Include(a => a.CreatedBy)
                .Include(a => a.Doctor)
                .Include(a => a.ModifiedBy)
                .Include(a => a.AssignedTo)
                .Include(a => a.Booking)
                .ToListAsync();

            return View(appointments);
        }


        [Authorize(Roles = "Doctor,Nurse, System Administrator")]
        [HttpGet]
        public async Task<IActionResult> MyFollowUpAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var appointments = await _context.FollowUpAppointments
                .Where(a =>
                 (a.Status == BookingStatus.Assigned || a.Status == BookingStatus.Completed) &&
                  a.AssignedUserId == user.Id)
                .Include(a => a.CreatedBy)
                .Include(a => a.AssignedTo)
                .Include(a => a.Doctor)
                .Include(a => a.ModifiedBy)
                .Include(a => a.Booking)
                .ToListAsync();

            return View(appointments);
        }

        [Authorize(Roles = "Doctor,Nurse,System Administrator")]
        [HttpGet]
        public async Task<IActionResult> FollowUpAppointmentDetails(string appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);

            var decryptedAppointmentId = _encryptionService.DecryptToInt(appointmentId);

            var appointment = await _context.FollowUpAppointments
                .Where(a => a.BookingId == decryptedAppointmentId)
                .Include(a => a.Booking)
                .ThenInclude(b => b.CreatedBy)   
                .Include(a => a.ModifiedBy)      
                .Include(a => a.AssignedTo)     
                .Include(a => a.Doctor)
                .Include(a => a.Nurse)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound();
            }

            var condition = appointment.MedicalCondition;

            var viewModel = new FollowUpAppointmentDetailsViewModel
            {
                PatientId = appointment.Booking.CreatedBy.Id,  
                DoctorId = appointment.ModifiedBy?.Id,     

                BookingId = appointment.BookingId,
                CreatedAt = appointment.CreatedAt,
                LastUpdatedAt = appointment.LastUpdatedAt,
                BookForTimeSlot = appointment.BookForTimeSlot,
                BookForDate = appointment.BookForDate,
                Status = appointment.Status,
                AdditionalNotes = appointment.AdditionalNotes,
                BookingReference = appointment.BookingReference,

                DoctorFullNames = appointment.ModifiedBy != null
                    ? $"{appointment.ModifiedBy.FirstName} {appointment.ModifiedBy.LastName}"
                    : null,

                PatientFullNames = $"{appointment.Booking.CreatedBy.FirstName} {appointment.Booking.CreatedBy.LastName}",
                Instructions = appointment.Instructions,
                MedicalCondition = condition,
                OriginalBookingId = appointment.OriginalBookingId,
                PatientEmail = appointment.Booking.CreatedBy.Email,
                PatientProfilePicture = appointment.Booking.CreatedBy.ProfilePicture,
                PhoneNumber = appointment.Booking.CreatedBy.PhoneNumber,
                IdNumber = appointment.Booking.CreatedBy.IdNumber,

                AssignedToFullNames = appointment.AssignedTo != null
                    ? $"{appointment.AssignedTo.FirstName} {appointment.AssignedTo.LastName}"
                    : null
            };

            var doctor = await _context.Doctors
                .Where(d => d.Id == user.Id)
                .FirstOrDefaultAsync();

            ViewBag.Specialization = doctor?.Specialization;

            return View(viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var myAppointments = await _context.Bookings
                .Include(ma => ma.AssignedTo)
                .Include(ma => ma.AssignedTo)
                .Where(ma => ma.CreatedById == user.Id)
                .OrderByDescending(ma => ma.CreatedAt)
                .ToListAsync();

            return View(myAppointments);
        }

        [Authorize(Roles = "Patient, Doctor, Nurse, System Administrator")]
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
                    Status = BookingStatus.Assigned,
                    CreatedById = user.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedById = user.Id,
                    LastUpdatedAt = DateTime.Now,
                    BookingReference = bookingReference,
                    BookForTimeSlot = viewModel.BookForTimeSlot,
                    AssignedUserId = availableNurse.Id,
                    AppointmentType = viewModel.AppointmentType
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

                TempData["Message"] = $"You have successfully booked an appointment.";

                return RedirectToAction(nameof(MyAppointments));
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


        [HttpGet]
        public async Task<IActionResult> FollowUpAppointment(string appointmentId, DateTime? date)
        {
            var decryptedAppointmentId = _encryptionService.DecryptToInt(appointmentId);
            var selectedDate = date ?? DateTime.Today;

            var appointment = await _context.Bookings
                .Include(a => a.CreatedBy)
                .Include(a => a.ModifiedBy)
                .FirstOrDefaultAsync(a => a.BookingId == decryptedAppointmentId);

            if (appointment == null || appointment.CreatedBy == null)
            {
                return NotFound("Appointment or patient not found.");
            }

            var patient = appointment.CreatedBy;

            var viewModel = new BookFollowUpAppointmentViewModel
            {
                PatientId = patient.Id,
                AdditionalNotes = appointment.AdditionalNotes,
                BookForDate = selectedDate,
                MedicalCondition = appointment.MedicalCondition,
                BookingId = decryptedAppointmentId,
                Address = patient.Address,
                AlternatePhoneNumber = patient.AlternatePhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                Email = patient.Email,
                FirstName = patient.FirstName,
                Gender = patient.Gender,
                IdNumber = patient.IdNumber,
                LastName = patient.LastName,
                PhoneNumber = patient.PhoneNumber,
                ProfilePicture = patient.ProfilePicture,
                NextPersonToSee = NextPersonToSee.Doctor,
                AvailableTimeSlots = GetTimeSlotsByDate(selectedDate, NextPersonToSee.Doctor),
                
            };


            var diseaseList = Enum.GetValues(typeof(Disease))
                .Cast<Disease>()
                .Select(d => new SelectListItem
                {
                    Text = d.GetType()
                            .GetMember(d.ToString())[0]
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? d.ToString(),
                    Value = d.ToString()
                })
                .ToList();

            ViewBag.Diseases = diseaseList;

            return View(viewModel);
        }



        [Authorize(Roles = "Doctor, Nurse, System Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FollowUpAppointment(BookFollowUpAppointmentViewModel viewModel)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(user);

                var appointment = await _context.Bookings
                    .Include(a => a.CreatedBy)
                    .Include(a => a.ModifiedBy)
                    .FirstOrDefaultAsync(a => a.BookingId == viewModel.BookingId);

                if (appointment == null)
                {
                    return NotFound("Original booking not found.");
                }

                appointment.Status = BookingStatus.Completed;

                UserBaseModel assignedUser = null;

                if (viewModel.NextPersonToSee == NextPersonToSee.Doctor)
                {
                    assignedUser = await _context.Doctors
                        .Where(d => !_context.Bookings.Any(b =>
                            b.AssignedUserId == d.Id &&
                            b.BookForDate == viewModel.BookForDate &&
                            b.BookForTimeSlot == viewModel.BookForTimeSlot))
                        .FirstOrDefaultAsync();

                    if (assignedUser == null)
                    {
                        viewModel.AvailableTimeSlots = GetTimeSlotsByDate(viewModel.BookForDate);
                        ModelState.AddModelError("", "No available Doctor found for the selected time slot.");
                        return View(viewModel);
                    }
                }
                else if (viewModel.NextPersonToSee == NextPersonToSee.Nurse)
                {
                    assignedUser = await _context.Nurses
                        .Where(n => !_context.Bookings.Any(b =>
                            b.AssignedUserId == n.Id &&
                            b.BookForDate == viewModel.BookForDate &&
                            b.BookForTimeSlot == viewModel.BookForTimeSlot))
                        .FirstOrDefaultAsync();

                    if (assignedUser == null)
                    {
                        viewModel.AvailableTimeSlots = GetTimeSlotsByDate(viewModel.BookForDate);
                        ModelState.AddModelError("", "No available Nurse found for the selected time slot.");
                        return View(viewModel);
                    }
                }

                string bookerId = null;
                string doctorId = null;
                string nurseId = null;

                if (userRoles.Contains("Doctor"))
                {
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == user.Id);
                    doctorId = doctor?.Id;
                }
                else if (userRoles.Contains("Nurse"))
                {
                    var nurse = await _context.Nurses.FirstOrDefaultAsync(n => n.Id == user.Id);
                    nurseId = nurse?.Id;
                }

                ICollection<string> instructionsList = string.IsNullOrWhiteSpace(viewModel.InstructionsInput)
                    ? null
                    : viewModel.InstructionsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(i => i.Trim()).ToList();

                var newFollowUp = new FollowUpAppointment
                {
                    PatientId = viewModel.PatientId,
                    BookForDate = viewModel.BookForDate,
                    BookForTimeSlot = viewModel.BookForTimeSlot,
                    BookingReference = GenerateBookingReferenceNumber(),
                    AdditionalNotes = viewModel.AdditionalNotes,
                    CreatedAt = DateTime.Now,
                    CreatedById = appointment.CreatedById,
                    OriginalBookingId = viewModel.BookingId,
                    LastUpdatedAt = DateTime.Now,
                    Instructions = instructionsList,
                    MedicalCondition = viewModel.MedicalCondition,
                    Status = BookingStatus.Assigned,
                    UpdatedById = user.Id,
                    AssignedUserId = assignedUser.Id,
                    DoctorId = doctorId, 
                    NurseId = nurseId,
                    NextPersonToSee = viewModel.NextPersonToSee,
                    Disease = viewModel.Disease
                };

                _context.Update(appointment);
                _context.Add(newFollowUp);
                await _context.SaveChangesAsync();

                var patient = await _context.Patients.FindAsync(viewModel.PatientId);

                TempData["Message"] = $"You have successfully booked a follow-up appointment for {patient?.FirstName} {patient?.LastName} " +
                                      $"on {viewModel.BookForDate:dd/MM/yyyy} at {viewModel.BookForTimeSlot}.";

                var encryptedAppointmentId = _encryptionService.Encrypt(viewModel.BookingId);

                return RedirectToAction(nameof(AppointmentDetails), new { appointmentId = encryptedAppointmentId });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to book follow-up appointment: " + ex.Message,
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

        private List<SelectListItem> GetTimeSlotsByDate(DateTime date, NextPersonToSee nextPersonToSee)
        {
            var slots = TimeSlotGenerator.GenerateDefaultSlots(date);
            var availableSlots = new List<SelectListItem>();

            List<string> bookedUserIds = _context.Bookings
                .Where(b => b.BookForDate.Date == date.Date)
                .Select(b => b.AssignedUserId)
                .ToList();

            List<UserBaseModel> users = nextPersonToSee == NextPersonToSee.Doctor
                ? _context.Doctors.Cast<UserBaseModel>().ToList()
                : _context.Nurses.Cast<UserBaseModel>().ToList();

            foreach (var slot in slots)
            {
                string slotValue = slot.From.ToString(@"hh\:mm");

                bool anyAvailable = users.Any(u =>
                    !_context.Bookings.Any(b =>
                        b.AssignedUserId == u.Id &&
                        b.BookForDate.Date == date.Date &&
                        b.BookForTimeSlot == slotValue));

                if (anyAvailable)
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

            return availableSlots;
        }


        [HttpGet]
        public JsonResult GetAvailableTimeSlots(DateTime date, CommonMedicalCondition condition)
        {
            var nurses = _context.Nurses.ToList();
            if (!nurses.Any())
            {
                return Json(new List<object>());
            }

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


        [HttpGet]
        public JsonResult GetFollowUpAvailableTimeSlots(DateTime date, string nextPersonToSee)
        {
            var parsed = Enum.TryParse<NextPersonToSee>(nextPersonToSee, out var targetRole);
            if (!parsed)
            {
                return Json(new List<object>());
            }

            var slots = GetTimeSlotsByDate(date, targetRole);
            var result = slots.Select(s => new { value = s.Value, text = s.Text }).ToList();

            return Json(result);
        }


        [HttpGet]
        public JsonResult GetFollowUpAvailableTimeSlots(DateTime date, CommonMedicalCondition condition)
        {
            var allSlots = TimeSlotGenerator.GenerateDefaultSlots(date);
            var availableSlots = new List<object>();

            var specializedDoctors = _context.Doctors
                .ToList();

            if (!specializedDoctors.Any())
            {
                return Json(new List<object>());
            }

            foreach (var slot in allSlots)
            {
                string slotValue = slot.From.ToString(@"hh\:mm");

                var bookedDoctorIds = _context.Bookings
                    .Where(b => b.BookForDate.Date == date.Date && b.BookForTimeSlot == slotValue)
                    .Select(b => b.AssignedUserId)
                    .ToList();

                var availableDoctor = specializedDoctors
                    .FirstOrDefault(d => !bookedDoctorIds.Contains(d.Id));

                if (availableDoctor != null)
                {
                    string fromText = DateTime.Today.Add(slot.From).ToString("HH:mm");
                    string toText = DateTime.Today.Add(slot.To).ToString("HH:mm");

                    availableSlots.Add(new
                    {
                        value = slotValue,
                        text = $"{fromText} - {toText}",
                        doctorId = availableDoctor.Id,
                        doctorName = $"{availableDoctor.FirstName} {availableDoctor.LastName}"
                    });
                }
            }

            return Json(availableSlots);
        }

        [Authorize(Roles = "Nurse, Doctor, System Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusRedirect(int appointmentId, BookingStatus status, IFormFile XRayImages)
        {
            var user = await _userManager.GetUserAsync(User);

            var followUpAppointment = await _context.Bookings
                .OfType<FollowUpAppointment>()
                .Include(b => b.Patient)
                .Include(b => b.ModifiedBy)
                .FirstOrDefaultAsync(b => b.BookingId == appointmentId);

            if (followUpAppointment != null)
            {
                followUpAppointment.Status = status;
                followUpAppointment.LastUpdatedAt = DateTime.Now;
                followUpAppointment.UpdatedById = user.Id;

                _context.Update(followUpAppointment);
                await _context.SaveChangesAsync();

                if (status == BookingStatus.Completed)
                {
                    await _feedbackService.FlagFeedbackPendingAsync(appointmentId, followUpAppointment.PatientId);
                }

                TempData["Message"] = $"You have successfully updated this appointment to {status}.";

                var encryptedId = _encryptionService.Encrypt(appointmentId);
                return RedirectToAction(nameof(AppointmentDetails), new { appointmentId = encryptedId });
            }

            var booking = await _context.Bookings
                .Where(b => !(b is FollowUpAppointment))
                .Include(b => b.Patient)
                .Include(b => b.ModifiedBy)
                .FirstOrDefaultAsync(b => b.BookingId == appointmentId);

            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = status;
            booking.LastUpdatedAt = DateTime.Now;
            booking.UpdatedById = user.Id;

            _context.Update(booking);
            await _context.SaveChangesAsync();

            if (status == BookingStatus.Completed)
            {
                await _feedbackService.FlagFeedbackPendingAsync(appointmentId, booking.PatientId);
            }

            TempData["Message"] = $"You have successfully updated this appointment to {status}.";

            var encryptedBookingId = _encryptionService.Encrypt(appointmentId);
            return RedirectToAction(nameof(AppointmentDetails), new { appointmentId = encryptedBookingId });
        }
    }
}
