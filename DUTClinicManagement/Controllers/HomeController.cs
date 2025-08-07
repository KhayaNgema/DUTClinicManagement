using DUTClinicManagement.Data;
using DUTClinicManagement.Interfaces;
using DUTClinicManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DUTClinicManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly DUTClinicManagementDbContext _context;
        private readonly FeedbackService _feedbackService;
        public HomeController(ILogger<HomeController> logger,
            UserManager<UserBaseModel> userManager,
            DUTClinicManagementDbContext context,
            FeedbackService feedbackService)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _feedbackService = feedbackService;
        }

        public async Task<IActionResult> Index(string tab)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                var roles = await _userManager.GetRolesAsync(user);

                if (user.IsFirstTimeLogin && roles.Any())
                {
                    user.IsFirstTimeLogin = false;
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    return Redirect("/Identity/Account/Manage/ChangeFirstTimeLoginPassword");
                }
                else
                {
                    ViewBag.ActiveTab = string.IsNullOrEmpty(tab) ? "sportnews" : tab;
                    return RedirectToAction("Home");
                }
            }
            else
            {
                ViewBag.ActiveTab = string.IsNullOrEmpty(tab) ? "sportnews" : tab;
                return View();
            }
        }

        public async Task<IActionResult> Home()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.LoggedInUser = $"{user.FirstName}";

            if (roles.Contains("System Administrator"))
            {
                return View("SystemAdministratorDashboard");
            }
            else if (roles.Contains("Doctor"))
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == user.Id);
                ViewBag.Specialization = doctor?.Specialization;
                return View("DoctorDashboard");
            }
            else if (roles.Contains("Delivery Personnel"))
            {
                return View("DelveryGuyDashboard");
            }
            else if (roles.Contains("Pharmacist"))
            {
                return View("PharmacistDashboard");
            }
            else if (roles.Contains("Nurse"))
            {
                return View("NurseDashboard");
            }
            else if (roles.Contains("Receptionist"))
            {
                return View("ReceptionistDashboard");
            }
            else
            {
                var hasPendingFeedback = await _feedbackService.HasPendingFeedbackAsync(user.Id);
                ViewBag.HasPendingFeedback = hasPendingFeedback;

                return View("PatientDashboard");
            }
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new  { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
