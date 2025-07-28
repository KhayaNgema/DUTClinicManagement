using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.ViewModels;
using DUTClinicManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUTClinicManagement.Controllers
{
    public class MedicalHistoriesController : Controller
    {

        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;

        public MedicalHistoriesController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
        }

        [Authorize(Roles = "Doctor")]
        [HttpGet]
        public async Task<IActionResult> PatientsMedicalHistory()
        {
            var medicalHistory = await _context.PatientMedicalHistories
                .Include(x => x.Patient)
                .ToListAsync();

            return View(medicalHistory);
        }

        [Authorize(Roles = "Doctor")]
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
                    .OrderByDescending(mh => mh.CreatedAt)
                    .LoadAsync();
            }

            return View(medicalRecord);
        }

        [Authorize(Roles = "Doctor")]
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


        [Authorize(Roles = "Doctor")]
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

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        public async Task<IActionResult> NewMedicalRecord(NewMedicalRecordViewModel viewModel)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var newMedicalRecord = new MedicalHistory
                {
                    PatientId = viewModel.PatientId,
                    ChiefComplaint = viewModel.ChiefComplaint,
                    Diagnosis = viewModel.Diagnosis,
                    DoctorId = user.Id,
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
                    PrescribedMedication = null,
                    CollectAfterCount = viewModel.CollectAfterCount,
                    CollectionInterval = viewModel.CollectionInterval,
                    UntilDate = DateTime.Now,
                    PrescriptionType = viewModel.PrescriptionType
                };

                _context.MedicalHistorys.Add(newMedicalRecord);
                await _context.SaveChangesAsync();

                var booking = await _context.Bookings
                    .Where(b => b.BookingId == viewModel.BookingId)
                    .FirstOrDefaultAsync();

                if(booking != null)
                {
                    var medicationPescription = new MedicationPescription
                    {
                        AdditionalNotes = viewModel.Notes,
                        AdmissionId = null,
                        CollectAfterCount = null,
                        CreatedAt = DateTime.Now,
                        LastUpdatedAt = DateTime.Now,
                        CollectionInterval = null,
                        CreatedById = user.Id,
                        HasDoneCollecting = false,
                        ExpiresAt = null,
                        NextCollectionDate = null,
                        PrescriptionType = null,
                        UpdatedById = user.Id,
                        BookingId = viewModel.BookingId,
                        PrescribedMedication = new List<Medication>(),
                        AccessCode = booking.BookingReference,

                    };

                    if (viewModel.PrescribedMedication != null && viewModel.PrescribedMedication.Any())
                    {
                        foreach (var med in viewModel.PrescribedMedication)
                        {
                            var medicationEntity = await _context.Medications
                                .FirstOrDefaultAsync(m => m.MedicationId == med.MedicationId);

                            if (medicationEntity != null)
                            {
                                medicationPescription.PrescribedMedication.Add(medicationEntity);
                            }
                            else
                            {

                            }
                        }
                    }

                    _context.Add(medicationPescription);
                    await _context.SaveChangesAsync();

                    _context.Update(medicationPescription);
                    await _context.SaveChangesAsync();
                }


                TempData["Message"] = $"You have successfully added new medical record for {viewModel.FirstName} {viewModel.LastName}";

                var encryptedMedicalRecordId = _encryptionService.Encrypt(viewModel.PatientMedicalHistoryId);

                return RedirectToAction(nameof(PatientMedicalRecord), new { medicalHistoryId = encryptedMedicalRecordId });
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
        }


    }
}
