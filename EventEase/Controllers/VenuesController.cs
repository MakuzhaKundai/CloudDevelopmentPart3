using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventEase.Models;
using EventEase.Data;
using EventEase.Services;
using Microsoft.Extensions.Logging;

namespace EventEase.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<VenuesController> _logger;

        public VenuesController(
            ApplicationDbContext context,
            IBlobStorageService blobStorageService,
            ILogger<VenuesController> logger)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        // GET: Venues
        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        // GET: Venues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .FirstOrDefaultAsync(m => m.VenueId == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // GET: Venues/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Venues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VenueId,VenueName,Location,Capacity,ImageFile")] Venue venue)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (venue.ImageFile != null && venue.ImageFile.Length > 0)
                    {
                        if (!IsValidImage(venue.ImageFile))
                        {
                            ModelState.AddModelError("ImageFile", "Only JPG, PNG or GIF images under 5MB are allowed");
                            return View(venue);
                        }

                        venue.ImageUrl = await _blobStorageService.UploadImageToBlob(venue.ImageFile);
                        _logger.LogInformation("Uploaded image for venue {VenueName} to {ImageUrl}", venue.VenueName, venue.ImageUrl);
                    }

                    _context.Add(venue);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Venue '{venue.VenueName}' created successfully";
                    return RedirectToAction(nameof(Index));
                }
                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating venue");
                TempData["ErrorMessage"] = "An error occurred while creating the venue";
                return View(venue);
            }
        }

        // GET: Venues/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: Venues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VenueId,VenueName,Location,Capacity,ImageUrl,ImageFile")] Venue venue)
        {
            if (id != venue.VenueId) return NotFound();

            try
            {
                if (ModelState.IsValid)
                {
                    var existingVenue = await _context.Venues.FindAsync(id);
                    if (existingVenue == null) return NotFound();

                    // Update properties
                    existingVenue.VenueName = venue.VenueName;
                    existingVenue.Location = venue.Location;
                    existingVenue.Capacity = venue.Capacity;

                    // Handle image update
                    if (venue.ImageFile != null && venue.ImageFile.Length > 0)
                    {
                        if (!IsValidImage(venue.ImageFile))
                        {
                            ModelState.AddModelError("ImageFile", "Only JPG, PNG or GIF images under 5MB are allowed");
                            return View(venue);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingVenue.ImageUrl))
                        {
                            await _blobStorageService.DeleteImageFromBlob(existingVenue.ImageUrl);
                        }

                        // Upload new image
                        existingVenue.ImageUrl = await _blobStorageService.UploadImageToBlob(venue.ImageFile);
                        _logger.LogInformation("Updated image for venue {VenueName} to {ImageUrl}", venue.VenueName, existingVenue.ImageUrl);
                    }

                    _context.Update(existingVenue);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Venue '{venue.VenueName}' updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                return View(venue);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!VenueExists(venue.VenueId))
                    return NotFound();

                _logger.LogError(ex, "Concurrency error updating venue {VenueId}", venue.VenueId);
                TempData["ErrorMessage"] = "The venue was modified by another user. Please refresh and try again.";
                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating venue {VenueId}", venue.VenueId);
                TempData["ErrorMessage"] = "An error occurred while updating the venue";
                return View(venue);
            }
        }

        // GET: Venues/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .FirstOrDefaultAsync(m => m.VenueId == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: Venues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var venue = await _context.Venues.FindAsync(id);
                if (venue != null)
                {
                    if (!string.IsNullOrEmpty(venue.ImageUrl))
                    {
                        await _blobStorageService.DeleteImageFromBlob(venue.ImageUrl);
                        _logger.LogInformation("Deleted image for venue {VenueName} from {ImageUrl}", venue.VenueName, venue.ImageUrl);
                    }

                    _context.Venues.Remove(venue);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Venue '{venue.VenueName}' deleted successfully";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting venue {VenueId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the venue";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.VenueId == id);
        }

        private bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension)) return false;
            if (!file.ContentType.StartsWith("image/")) return false;
            if (file.Length > 5 * 1024 * 1024) return false; // 5MB max

            return true;
        }
    }
}