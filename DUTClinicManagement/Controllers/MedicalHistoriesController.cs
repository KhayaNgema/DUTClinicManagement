using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using DUTClinicManagement.ViewModels;
using HospitalManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DUTClinicManagement.Controllers
{
    public class MedicalHistoriesController : Controller
    {

        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;
        private readonly QrCodeService _qrCodeService;

        public MedicalHistoriesController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService,
            QrCodeService qrCodeService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _qrCodeService = qrCodeService;
        }

        [Authorize(Roles = "Doctor, Nurse")]
        [HttpGet]
        public async Task<IActionResult> PatientsMedicalHistory()
        {
            var medicalHistory = await _context.PatientMedicalHistories
                .Include(x => x.Patient)
                .ToListAsync();

            return View(medicalHistory);
        }

        [Authorize(Roles = "Doctor, Nurse")]
        [HttpGet]
        public async Task<IActionResult> PatientMedicalRecord(string medicalHistoryId)
        {
            var decryptedMedicalHistoryId = _encryptionService.DecryptToInt(medicalHistoryId);

            var medicalRecord = await _context.PatientMedicalHistories
                .Where(mr => mr.PatientMedicalHistoryId == decryptedMedicalHistoryId)
                .Include(x => x.Patient) 
                .FirstOrDefaultAsync();

            if (medicalRecord != null)
            {
                await _context.Entry(medicalRecord)
                    .Collection(mr => mr.MedicalHistories)
                    .Query()
                    .Include(mh => mh.Doctor)
                    .Include(mh => mh.Nurse)
                    .OrderByDescending(mh => mh.CreatedAt)
                    .LoadAsync();
            }

            return View(medicalRecord);
        }

        [Authorize(Roles = "Doctor, Nurse")]
        [HttpGet]
        public async Task<IActionResult> MedicalHistory(string medicalHistoryId)
        {
            var decryptedMedicalHistoryId = _encryptionService.DecryptToInt(medicalHistoryId);

            var medicalHistory = await _context.MedicalHistorys
                .Where(mh => mh.MedicalHistoryId == decryptedMedicalHistoryId)
                .Include(mh => mh.Patient)
                .FirstOrDefaultAsync();

            var viewModel = new MedicalHistoryViewModel
            {
                CollectAfterCount = medicalHistory.CollectAfterCount,
                ChiefComplaint = medicalHistory.ChiefComplaint,
                CollectionInterval = medicalHistory.CollectionInterval,
                FirstName = medicalHistory.Patient.FirstName,
                DateOfBirth = medicalHistory.Patient.DateOfBirth,
                Diagnosis = medicalHistory.Diagnosis,
                FollowUpInstructions = medicalHistory.FollowUpInstructions,
                HeightCm = medicalHistory.HeightCm,
                Immunizations = medicalHistory.Immunizations,
                LabResults = medicalHistory.LabResults,
                LastName  = medicalHistory.Patient?.LastName,
                PrescribedMedication = medicalHistory.PrescribedMedication,
                PrescriptionType = medicalHistory.PrescriptionType,
                ProfilePicture = medicalHistory.Patient.ProfilePicture,
                Notes = medicalHistory.Notes,
                Surgeries = medicalHistory.Surgeries,
                Symptoms = medicalHistory.Symptoms,
                Treatment = medicalHistory.Treatment,
                UntilDate = medicalHistory.UntilDate,
                Vitals = medicalHistory.Vitals,
                WeightKg = medicalHistory.WeightKg
            };

            return View(viewModel);
        }


        [Authorize(Roles = "Doctor, Nurse")]
        [HttpGet]
        public async Task<IActionResult> NewMedicalRecord(string medicalHistoryId)
        {
            var decryptedMedicalHistoryId = _encryptionService.DecryptToInt(medicalHistoryId);

            var medicalHistory = await _context.PatientMedicalHistories
                .Where(p => p.PatientMedicalHistoryId == decryptedMedicalHistoryId)
                .Include(p => p.Patient)
                .FirstOrDefaultAsync();

            var appointment = await _context.Bookings
                .Where(a => a.BookingReference == medicalHistory.AccessCode)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            var patient = medicalHistory.Patient;

            var viewModel = new NewMedicalRecordViewModel
            {
                BookingId = appointment.BookingId,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                ProfilePicture = patient.ProfilePicture,
                PatientId = medicalHistory.PatientId,
                PatientMedicalHistoryId= decryptedMedicalHistoryId
            };

            var medication = await _context.Medications
                .ToListAsync();

            ViewBag.Medications = medication;

            return View(viewModel);
        }

        [Authorize(Roles = "Doctor, Nurse")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewMedicalRecord(NewMedicalRecordViewModel viewModel)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var roles = await _userManager.GetRolesAsync(user);

                bool isDoctor = roles.Contains("Doctor");
                bool isNurse = roles.Contains("Nurse");

                var newMedicalRecord = new MedicalHistory
                {
                    PatientId = viewModel.PatientId,
                    ChiefComplaint = viewModel.ChiefComplaint,
                    Diagnosis = viewModel.Diagnosis,
                    DoctorId = isDoctor ? user.Id : null,
                    NurseId = isNurse ? user.Id : null,
                    FollowUpInstructions = viewModel.FollowUpInstructions,
                    Immunizations = viewModel.Immunizations,
                    HeightCm = viewModel.HeightCm,
                    LabResults = viewModel.LabResults,
                    Notes = viewModel.Notes,
                    PatientMedicalHistoryId = viewModel.PatientMedicalHistoryId,
                    Surgeries = viewModel.Surgeries,
                    WeightKg = viewModel.WeightKg,
                    Vitals = viewModel.Vitals,
                    VisitDate = DateTime.Now,
                    Treatment = viewModel.Treatment,
                    Symptoms = viewModel.Symptoms,
                    CreatedAt = DateTime.Now,
                    LastUpdatedAt = DateTime.Now,
                    RecordedAt = DateTime.Now,
                    CreatedById = user.Id,
                    UpdatedById = user.Id,
                    CollectAfterCount = viewModel.CollectAfterCount,
                    CollectionInterval = viewModel.CollectionInterval,
                    UntilDate = viewModel.UntilDate ?? DateTime.Now,
                    PrescriptionType = viewModel.PrescriptionType
                };

                _context.MedicalHistorys.Add(newMedicalRecord);
                await _context.SaveChangesAsync();

                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == viewModel.BookingId);

                if (viewModel.PrescribedMedication != null && viewModel.PrescribedMedication.Any())
                {
                    DateTime baseDate = viewModel.LastCollectionDate ?? DateTime.Now;
                    int count = viewModel.CollectAfterCount ?? 0;

                    DateTime tentativeNextCollectionDate = baseDate;

                    switch (viewModel.CollectionInterval)
                    {
                        case CollectionInterval.Day:
                            tentativeNextCollectionDate = baseDate.AddDays(count);
                            break;
                        case CollectionInterval.Week:
                            tentativeNextCollectionDate = baseDate.AddDays(count * 7);
                            break;
                        case CollectionInterval.Month:
                            tentativeNextCollectionDate = baseDate.AddMonths(count);
                            break;
                        case CollectionInterval.Year:
                            tentativeNextCollectionDate = baseDate.AddYears(count);
                            break;
                    }

                    if (viewModel.UntilDate.HasValue && tentativeNextCollectionDate > viewModel.UntilDate.Value)
                    {
                        tentativeNextCollectionDate = viewModel.UntilDate.Value;
                    }

                    DateTime finalNextCollectionDate = tentativeNextCollectionDate;

                    var prescribedMedicationIds = viewModel.PrescribedMedication.Select(pm => pm.MedicationId).ToList();

                    var medications = await _context.Medications
                        .Where(m => prescribedMedicationIds.Contains(m.MedicationId))
                        .ToListAsync();

                    var medicationPescription = new MedicationPescription
                    {
                        AdditionalNotes = viewModel.AdditionalNotes,
                        BookingId = booking?.BookingId ?? 0,
                        CollectAfterCount = viewModel.CollectAfterCount,
                        CreatedAt = DateTime.Now,
                        LastUpdatedAt = DateTime.Now,
                        CollectionInterval = viewModel.CollectionInterval,
                        CreatedById = user.Id,
                        HasDoneCollecting = false,
                        NextCollectionDate = finalNextCollectionDate,
                        PrescribedMedication = medications,
                        PrescriptionType = viewModel.PrescriptionType,
                        UpdatedById = user.Id,
                        ExpiresAt = viewModel.UntilDate,
                        AccessCode = booking?.BookingReference ?? string.Empty,
                        LastCollectionDate = baseDate
                    };

                    _context.Add(medicationPescription);
                    await _context.SaveChangesAsync();

                    medicationPescription.QrCodeImage = _qrCodeService.GenerateQrCode(medicationPescription.AccessCode);
                    _context.Update(medicationPescription);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = $"You have successfully added new medical record for {viewModel.FirstName} {viewModel.LastName}";

                    var encryptedMedicalRecordId = _encryptionService.Encrypt(viewModel.PatientMedicalHistoryId);
                    return RedirectToAction(nameof(PatientMedicalRecord), new { medicalHistoryId = encryptedMedicalRecordId });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to add new medical record: " + ex.Message,
                    errorDetails = new
                    {
                        InnerException = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }

            return View(viewModel);
        }



    }
}
