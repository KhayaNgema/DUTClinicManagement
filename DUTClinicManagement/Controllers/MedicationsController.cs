using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using HospitalManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class MedicationsController : Controller
    {
        private readonly SignInManager<UserBaseModel> _signInManager;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IUserStore<UserBaseModel> _userStore;
        private readonly IUserEmailStore<UserBaseModel> _emailStore;
        private readonly FileUploadService _fileUploadService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly EmailService _emailService;
        private readonly DUTClinicManagementDbContext _context;
        private readonly IActivityLogger _activityLogger;
        private readonly IEncryptionService _encryptionService;

        public MedicationsController(
            UserManager<UserBaseModel> userManager,
            IUserStore<UserBaseModel> userStore,
            SignInManager<UserBaseModel> signInManager,
            IEmailSender emailSender,
            FileUploadService fileUploadService,
            RoleManager<IdentityRole> roleManager,
            EmailService emailService,
            IEncryptionService encryptionService,
           DUTClinicManagementDbContext db,
            IActivityLogger activityLogger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _encryptionService = encryptionService;
            _emailSender = emailSender;
            _fileUploadService = fileUploadService;
            _roleManager = roleManager;
            _emailService = emailService;
            _context = db;
            _activityLogger = activityLogger;
        }

        [Authorize(Roles = "Pharmacist")]
        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var inventory = await _context.MedicationInventory
                .Include(i => i.Medication)
                .OrderBy(i => i.Medication.MedicationName)
                .ToListAsync();

            return View(inventory);
        }

        [HttpGet]
        public async Task<IActionResult> CollectMedication()
        {
            var user = await _userManager.GetUserAsync(User);

            var medications = await _context.MedicationPescription
                .Include(mp => mp.PrescribedMedication)
                .Include(mp => mp.Booking)
                    .ThenInclude(b => b.CreatedBy)
                .Include(mp => mp.Booking)
                    .ThenInclude(b => b.Patient)
                .Where(mp => mp.Booking != null
                    && mp.Booking.CreatedBy != null
                    && mp.Booking.CreatedBy.Id == user.Id
                    && (mp.Status == MedicationPescriptionStatus.Pending
                        || mp.Status == MedicationPescriptionStatus.Collecting)
                )
                .ToListAsync();


            var medicationViewModels = new List<MedicationCollectionViewModel>();

            foreach (var mp in medications)
            {
                var address = mp.Booking?.CreatedBy?.Address;

                string street = null, city = null, postalCode = null, country = null;
                Province province = default;

                if (!string.IsNullOrWhiteSpace(address))
                {
                    var parts = address.Split(',')
                        .Select(p => p.Trim())
                        .ToArray();

                    if (parts.Length >= 5)
                    {
                        street = parts[0];
                        city = parts[1];

                        var provinceString = parts[2].Replace(" ", "").Replace("-", "");
                        if (Enum.TryParse<Province>(provinceString, ignoreCase: true, out var prov))
                        {
                            province = prov;
                        }

                        postalCode = parts[3];
                        country = parts[4];
                    }
                }

                medicationViewModels.Add(new MedicationCollectionViewModel
                {
                    MedicationPescriptionId = mp.MedicationPescriptionId, // Keep as in your ViewModel [note spelling: Pescription]
                    Booking = mp.Booking,
                    PrescribedMedication = mp.PrescribedMedication.Cast<Medication>().ToList(),
                    NextCollectionDate = mp.NextCollectionDate,
                    LastCollectionDate = mp.LastCollectionDate,
                    Status = mp.Status,
                    ExpiresAt = mp.ExpiresAt,

                    Street = street,
                    City = city,
                    Province = province,
                    PostalCode = postalCode,
                    Country = country
                });
            }

            return View(medicationViewModels);
        }




        [Authorize(Roles = "Pharmacist")]
        [HttpGet]
        public async Task<IActionResult> MedicationPescriptionRequests()
        {
            var medications = await _context.MedicationPescription
                .Include(mp => mp.PrescribedMedication)
                .Include(mp => mp.Booking)
                .ThenInclude(mp => mp.CreatedBy)
                .Include(mp => mp.CreatedBy)
                .Include(mp => mp.ModifiedBy)
                .Where(mp => mp.Status == MedicationPescriptionStatus.Pending ||
                mp.Status == MedicationPescriptionStatus.Collecting)
                .OrderByDescending(mp => mp.CreatedAt)
                .ToListAsync();

            return View(medications);
        }

        [Authorize(Roles = "Pharmacist")]
        [HttpGet]
        public async Task<IActionResult> PreviousMedicationRequests()
        {
            var medications = await _context.MedicationPescription
                .Include(mp => mp.PrescribedMedication)
                .Include(mp => mp.Booking)
                .ThenInclude(mp => mp.CreatedBy)
                .Include(mp => mp.CreatedBy)
                .Include(mp => mp.ModifiedBy)
                .Where(mp => mp.Status == MedicationPescriptionStatus.Collected)
                .OrderByDescending(mp => mp.CreatedAt)
                .ToListAsync();

            return View(medications);
        }

        [Authorize(Roles = "Pharmacist")]
        [HttpGet]
        public async Task<IActionResult> Medications()
        {
            var medications = await _context.Medications
                .Include(m => m.Category)
                .ToListAsync();

            return View(medications);
        }

        [Authorize(Roles = "Pharmacist, Doctor")]
        [HttpGet]
        public async Task<IActionResult> PescriptionRequest(string medicationPescriptionId)
        {
            var decryptedMedicationPescriptionId = _encryptionService.DecryptToInt(medicationPescriptionId);

            var pescriptionRequest = await _context.MedicationPescription
                .Where(pr => pr.MedicationPescriptionId == decryptedMedicationPescriptionId)
                .Include(pr => pr.Booking)
                    .ThenInclude(b => b.CreatedBy)
                .Include(pr => pr.Booking)
                    .ThenInclude(b => b.AssignedTo)
                 .Include(pr => pr.PrescribedMedication)
                .FirstOrDefaultAsync();

            if (pescriptionRequest == null)
            {
                return NotFound(); 
            }

            var booking = pescriptionRequest.Booking;

            var patient = booking?.CreatedBy;
            var doctor = booking?.AssignedTo;

            if (patient == null || doctor == null)
            {
                return BadRequest("Incomplete prescription data.");
            }

            var viewModel = new PescriptionRequestViewModel
            {
                AccessCode = pescriptionRequest.AccessCode,
                PatientFirstName = patient.FirstName,
                PatientLastName = patient.LastName,
                PatientIdNumber = patient.IdNumber,
                Email = patient.Email,
                ProfilePicture = patient.ProfilePicture,
                PhoneNumber = patient.PhoneNumber,
                DoctorFirstName = doctor.FirstName,
                DoctorLastName = doctor.LastName,
                DoctorEmail = doctor.Email,
                DoctorPhoneNumber = doctor.PhoneNumber,
                LastCollectionDate = pescriptionRequest.LastCollectionDate,
                NextCollectionDate = pescriptionRequest.NextCollectionDate,
                PescribedMedication = pescriptionRequest.PrescribedMedication,
                PescriptionRequestId = decryptedMedicationPescriptionId,
                AdditionalNotes = pescriptionRequest.AdditionalNotes,
                PrescriptionType = pescriptionRequest.PrescriptionType,
                CollectAfterCount = pescriptionRequest.CollectAfterCount,
                CollectInterval = pescriptionRequest.CollectionInterval,
                CollectUntilDate = pescriptionRequest.ExpiresAt,
                Status = pescriptionRequest.Status,
                
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Pharmacist")]
        [HttpGet]
        public async Task<IActionResult> NewMedication()
        {

            var categories = await _context.MedicationCategories
                .ToListAsync();

            ViewBag.Categories = categories;

            return View();
        }

        [Authorize(Roles = "Pharmacist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewMedication(MedicationViewModel viewModel, IFormFile MedicationImages)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var menuItem = new Medication
                {
                    MedicationName = viewModel.MedicationName,
                    Description = viewModel.Description,
                    Price = viewModel.Price,
                    Manufacturer = viewModel.Manufacturer,
                    DosageForm = viewModel.DosageForm,
                    Strength = viewModel.Strength,
                    UnitOfMeasure = viewModel.UnitOfMeasure,
                    ExpiryDate = viewModel.ExpiryDate,
                    IsExpired = false,
                    CreatedAt = DateTime.Now,
                    CreatedById = user.Id,
                    LastUpdatedAt = DateTime.Now,
                    UpdatedById = user.Id,
                    IsPrescriptionRequired = true,
                    CategoryId = viewModel.CategoryId,
                };

                if (viewModel.MedicationImages != null && viewModel.MedicationImages.Length > 0)
                {
                    var playerProfilePicturePath = await _fileUploadService.UploadFileAsync(viewModel.MedicationImages);
                    menuItem.MedicationImage= playerProfilePicturePath;
                }

                _context.Add(menuItem);
                await _context.SaveChangesAsync();


                var newInventory = new MedicationInventory
                {
                    MedicationId = menuItem.MedicationId,
                    Quantity = 0,
                    StockLevel = StockLevel.Critical,
                    Availability = MedicationAvailability.OutOfStock
                };

                _context.Add(newInventory);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"You have successfully added {viewModel.MedicationName} as your new hospital medication";

                return RedirectToAction(nameof(Medications));
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to add new menu item: " + ex.Message,
                    errorDetails = new
                    {
                        InnerException = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }
            var categories = await _context.MedicationCategories
                .ToListAsync();

            ViewBag.Categories = categories;

            return View(viewModel);
        }


        [Authorize(Roles = "Pharmacist, System Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int prescriptionRequestId, MedicationPescriptionStatus status)
        {
            var user = await _userManager.GetUserAsync(User);

            var medicationprescriptionRequest = await _context.MedicationPescription
                .Include(mpr => mpr.PrescribedMedication)
                .FirstOrDefaultAsync(mpr => mpr.MedicationPescriptionId == prescriptionRequestId);

            if (medicationprescriptionRequest == null)
            {
                TempData["Error"] = "Prescription request not found.";
                return RedirectToAction(nameof(PescriptionRequest));
            }

            medicationprescriptionRequest.Status = status;
            medicationprescriptionRequest.LastUpdatedAt = DateTime.Now;
            medicationprescriptionRequest.UpdatedById = user.Id;

            _context.Update(medicationprescriptionRequest);
            await _context.SaveChangesAsync();

            if (status == MedicationPescriptionStatus.Collected || status == MedicationPescriptionStatus.Collecting)
            {
                foreach (var prescribed in medicationprescriptionRequest.PrescribedMedication)
                {
                    var inventory = await _context.MedicationInventory
                        .Include(i => i.Medication)
                        .FirstOrDefaultAsync(i => i.Medication.MedicationId == prescribed.MedicationId);

                    if (inventory == null || inventory.Quantity <= 0)
                        continue;

                    inventory.Quantity--;

                    if (inventory.Quantity <= 5)
                        inventory.StockLevel = StockLevel.Critical;
                    else if (inventory.Quantity <= 10)
                        inventory.StockLevel = StockLevel.Low;
                    else if (inventory.Quantity <= 25)
                        inventory.StockLevel = StockLevel.Moderate;
                    else
                        inventory.StockLevel = StockLevel.High;

                    if (inventory.Quantity == 0)
                        inventory.Availability = MedicationAvailability.OutOfStock;

                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                }
            }

            var booking = await _context.Bookings
                .Include(b => b.Patient)
                .FirstOrDefaultAsync(b => b.BookingId == medicationprescriptionRequest.BookingId);

            if (booking != null)
            {
                TempData["Message"] = $"You have successfully updated medication request status.";
            }
            else
            {
                TempData["Message"] = $"You have successfully updated the medication prescription request status to {medicationprescriptionRequest.Status}.";
            }

            var encryptedPrescriptionId = _encryptionService.Encrypt(medicationprescriptionRequest.MedicationPescriptionId);
            return RedirectToAction(nameof(PescriptionRequest), new { medicationPescriptionId = encryptedPrescriptionId });
        }

    }
}
