using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FutureTechAcademy.Models;
using FutureTechAcademy.Services.Interfaces;
using FutureTechAcademy.Services;
using System.Security.Claims;

namespace FutureTechAcademy.Controllers
{
    [Authorize]
    public class AdminManagementController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IAdminService _adminService;

        public AdminManagementController(ICosmosDbService cosmosDbService, IAdminService adminService)
        {
            _cosmosDbService = cosmosDbService;
            _adminService = adminService;
        }

        private string GetCurrentUserEmail()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        public async Task<IActionResult> Index()
        {
            var currentEmail = GetCurrentUserEmail();
            var masterEmail = "fundilebungane0@gmail.com";

            // Only master admin can access admin management
            if (currentEmail != masterEmail)
            {
                TempData["Error"] = "Only master admin can manage admins";
                return RedirectToAction("Index", "Students");
            }

            var admins = await _cosmosDbService.GetAdminUsersAsync();
            ViewBag.MasterAdminEmail = masterEmail;
            return View(admins);
        }

        [HttpPost]
        public async Task<IActionResult> AddAdmin(string email)
        {
            var currentEmail = GetCurrentUserEmail();
            var masterEmail = "fundilebungane0@gmail.com";

            if (currentEmail != masterEmail)
            {
                TempData["Error"] = "Only master admin can add admins";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Email is required";
                return RedirectToAction("Index");
            }

            var existingAdmins = await _cosmosDbService.GetAdminUsersAsync();
            if (existingAdmins.Any(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "This email is already an admin";
                return RedirectToAction("Index");
            }

            var newAdmin = new AdminUser
            {
                Email = email,
                AddedBy = currentEmail,
                AddedAt = DateTime.UtcNow
            };

            await _cosmosDbService.AddAdminUserAsync(newAdmin);
            TempData["Success"] = $"Added {email} as an admin";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAdmin(string email)
        {
            var currentEmail = GetCurrentUserEmail();
            var masterEmail = "fundilebungane0@gmail.com";

            if (currentEmail != masterEmail)
            {
                TempData["Error"] = "Only master admin can remove admins";
                return RedirectToAction("Index");
            }

            if (email == masterEmail)
            {
                TempData["Error"] = "Cannot remove master admin";
                return RedirectToAction("Index");
            }

            await _cosmosDbService.RemoveAdminUserAsync(email);
            TempData["Success"] = $"Removed {email} as an admin";
            return RedirectToAction("Index");
        }
    }
}