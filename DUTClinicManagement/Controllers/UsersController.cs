using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DUTClinicManagement.ViewModels;

namespace DUTClinicManagement.Controllers
{
    public class UsersController : Controller
    {

        private readonly DUTClinicManagementDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailService _emailService;

        public UsersController(DUTClinicManagementDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
        }

        [Authorize(Roles = "System Administrator, Receptionist")]
        public async Task<IActionResult> Nurses()
        {
            var nurses = await _context.Doctors
                .ToListAsync();

            return View(nurses);
        }

        [Authorize(Roles = "System Administrator, Receptionist")]
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors
                .ToListAsync();

            return View(doctors);
        }

        [Authorize(Roles = "System Administrator, Receptionist")]
        public async Task<IActionResult> Receptionists()
        {
            var receptionists = await _context.Receptionists
                .ToListAsync();

            return View(receptionists);
        }

        [Authorize(Roles = "System Administrator, Receptionist")]
        public async Task<IActionResult> DeliveryGuy()
        {
            var deliveryGuys = await _context.DeliveryGuys
                .ToListAsync();

            return View(deliveryGuys);
        }

        [Authorize(Roles = "System Administrator, Receptionist")]
        public async Task<IActionResult> Pharmacists()
        {
            var pharmacists = await _context.Pharmacists
                .ToListAsync();

            return View(pharmacists);
        }

        [Authorize(Roles = "System Administrator, Receptionist")]
        public async Task<IActionResult> Patients()
        {
            var kitchenStaff = await _context.Patients
                .ToListAsync();

            return View(kitchenStaff);
        }

        [Authorize]
        [HttpGet]

        public async Task<IActionResult> Account()
        {
            var user = await _userManager.GetUserAsync(User);

            var userRole = await _context.UserRoles
                 .Where(ur => ur.UserId == user.Id)
                 .Join(_context.Roles,
                 ur => ur.RoleId,
                 r => r.Id,
                 (ur, r) => r.Name)
                 .FirstOrDefaultAsync();

            var viewModel = new AccountViewModel
            {
                UserRole = userRole,
                UserId = user.Id,
                ProfilePicture = user.ProfilePicture,
                FullNames = $"{user.FirstName} {user.LastName}",
            };

            return View(viewModel);
        }
    }
}
