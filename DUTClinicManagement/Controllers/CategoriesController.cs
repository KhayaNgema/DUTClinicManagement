using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUTClinicManagement.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;

        public CategoriesController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;

        }


        [HttpGet]
        public async Task<IActionResult> MedicationCategories()
        {
            var categories = await _context.MedicationCategories
                .Include(c => c.CreatedBy)
                 .Include(c => c.ModifiedBy)
                .ToListAsync();

            return View(categories);
        }
        
        [HttpGet]
        public async Task<IActionResult> NewMedicationCategory()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewMedicationCategory(MedicationCategory model)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                var category = new MedicationCategory
                {
                    CategoryName = model.CategoryName,
                    CreatedAt = DateTime.Now,
                    CreatedById = user.Id,
                    LastUpdatedAt = DateTime.Now,
                    UpdatedById = user.Id
                };

                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"You have successfully created the {category.CategoryName} category.";

                return RedirectToAction(nameof(MedicationCategories));
            }
            catch (Exception ex) 
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to create new category: " + ex.Message,
                    errorDetails = new
                    {
                        InnerException = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }


            return View(model);
        }
    }
}
