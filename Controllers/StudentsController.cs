using Microsoft.AspNetCore.Mvc;
using FutureTechAcademy.Models;
using FutureTechAcademy.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FutureTechAcademy.Services;

namespace FutureTechAcademy.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<StudentsController> _logger;
        private readonly IAdminService _adminService;  // ADD THIS BACK

        public StudentsController(
            ICosmosDbService cosmosDbService,
            IBlobStorageService blobStorageService,
            ILogger<StudentsController> logger,
            IAdminService adminService)  // ADD THIS BACK
        {
            _cosmosDbService = cosmosDbService;
            _blobStorageService = blobStorageService;
            _logger = logger;
            _adminService = adminService;  // ADD THIS BACK
        }

        // Helper method to get user email
        private string GetUserEmail()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        // Helper method to check if user is admin
        private async Task<bool> IsAdmin()
        {
            var userEmail = GetUserEmail();
            return await _adminService.IsAdmin(userEmail);
        }

        // GET: Students
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            var students = await _cosmosDbService.GetStudentsAsync(page, 10);
            return View(students);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile? profileImage)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            ModelState.Remove("ProfileImageUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        student.ProfileImageUrl = await _blobStorageService.UploadImageAsync(profileImage, student.Id);
                    }

                    await _cosmosDbService.AddStudentAsync(student);
                    TempData["Success"] = "Student created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating student");
                    ModelState.AddModelError("", "An error occurred while saving the student: " + ex.Message);
                }
            }

            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Student student, IFormFile? newProfileImage)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            if (id != student.Id)
            {
                return NotFound();
            }

            ModelState.Remove("ProfileImageUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    if (newProfileImage != null && newProfileImage.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(student.ProfileImageUrl))
                        {
                            await _blobStorageService.DeleteImageAsync(student.ProfileImageUrl);
                        }

                        student.ProfileImageUrl = await _blobStorageService.UploadImageAsync(newProfileImage, student.Id);
                    }

                    await _cosmosDbService.UpdateStudentAsync(id, student);
                    TempData["Success"] = "Student updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating student");
                    ModelState.AddModelError("", "An error occurred while updating the student: " + ex.Message);
                }
            }
            return View(student);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, bool permanent = false)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            try
            {
                var student = await _cosmosDbService.GetStudentAsync(id);

                if (student != null && !string.IsNullOrEmpty(student.ProfileImageUrl))
                {
                    await _blobStorageService.DeleteImageAsync(student.ProfileImageUrl);
                }

                if (permanent)
                {
                    await _cosmosDbService.HardDeleteStudentAsync(id);
                    TempData["Success"] = "Student permanently deleted!";
                }
                else
                {
                    await _cosmosDbService.SoftDeleteStudentAsync(id);
                    TempData["Success"] = "Student soft deleted!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                TempData["Error"] = "An error occurred while deleting the student.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Students/Search
        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (!await IsAdmin())
            {
                return View("AccessDenied");
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }

            var results = await _cosmosDbService.SearchStudentsAsync(searchTerm);
            return View("Index", results);
        }
    }
}