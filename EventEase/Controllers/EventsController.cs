using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventEase.Models;
using EventEase.Data;
using EventEase.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading;

namespace EventEase.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            ApplicationDbContext context,
            IBlobStorageService blobStorageService,
            ILogger<EventsController> logger)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string searchString,
            int? eventTypeId,
            int? venueId,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var events = _context.Events
                    .Include(e => e.EventType)
                    .Include(e => e.Venue)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    events = events.Where(e =>
                        e.EventName.Contains(searchString) ||
                        e.Description.Contains(searchString));
                }

                if (eventTypeId.HasValue)
                {
                    events = events.Where(e => e.EventTypeId == eventTypeId);
                }

                if (venueId.HasValue)
                {
                    events = events.Where(e => e.VenueId == venueId);
                }

                var paginatedEvents = await events
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                ViewData["EventTypeId"] = new SelectList(_context.EventTypes, "EventTypeId", "Name");
                ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName");
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = await events.CountAsync(cancellationToken);

                return View(paginatedEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return RedirectToAction("Error", "Home", new { message = "Failed to load events" });
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var @event = await _context.Events
                    .Include(e => e.EventType)
                    .Include(e => e.Venue)
                    .FirstOrDefaultAsync(m => m.EventId == id, cancellationToken);

                if (@event == null)
                {
                    return NotFound();
                }

                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving event with ID {id}");
                return RedirectToAction("Error", "Home", new { message = $"Failed to load event {id}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            try
            {
                ViewData["EventTypeId"] = new SelectList(
                    await _context.EventTypes.ToListAsync(cancellationToken),
                    "EventTypeId", "Name");
                ViewData["VenueId"] = new SelectList(
                    await _context.Venues.ToListAsync(cancellationToken),
                    "VenueId", "VenueName");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                return RedirectToAction("Error", "Home", new { message = "Failed to load event creation form" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("EventId,EventName,EventDate,Description,VenueId,EventTypeId,ImageFile")]
            Event @event,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (@event.ImageFile != null)
                    {
                        if (!IsValidImage(@event.ImageFile))
                        {
                            ModelState.AddModelError("ImageFile", "Invalid image file. Only JPG, PNG, or GIF under 5MB allowed.");
                            await PopulateDropdowns(@event.EventTypeId, @event.VenueId, cancellationToken);
                            return View(@event);
                        }

                        @event.ImageUrl = await _blobStorageService.UploadImageToBlob(@event.ImageFile);
                    }

                    _context.Add(@event);
                    await _context.SaveChangesAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(@event.EventTypeId, @event.VenueId, cancellationToken);
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                await PopulateDropdowns(@event?.EventTypeId, @event?.VenueId, cancellationToken);
                return RedirectToAction("Error", "Home", new { message = "Failed to create event" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var @event = await _context.Events.FindAsync(new object[] { id }, cancellationToken);
                if (@event == null)
                {
                    return NotFound();
                }

                await PopulateDropdowns(@event.EventTypeId, @event.VenueId, cancellationToken);
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit form for event ID {id}");
                return RedirectToAction("Error", "Home", new { message = $"Failed to load edit form for event {id}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("EventId,EventName,EventDate,Description,VenueId,EventTypeId,ImageUrl,ImageFile")]
            Event @event,
            CancellationToken cancellationToken = default)
        {
            if (id != @event.EventId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(@event.EventTypeId, @event.VenueId, cancellationToken);
                return View(@event);
            }

            try
            {
                var existingEvent = await _context.Events.FindAsync(new object[] { id }, cancellationToken);
                if (existingEvent == null)
                {
                    return NotFound();
                }

                // Update properties
                existingEvent.EventName = @event.EventName;
                existingEvent.EventDate = @event.EventDate;
                existingEvent.Description = @event.Description;
                existingEvent.VenueId = @event.VenueId;
                existingEvent.EventTypeId = @event.EventTypeId;

                // Handle image update
                if (@event.ImageFile != null)
                {
                    if (!IsValidImage(@event.ImageFile))
                    {
                        ModelState.AddModelError("ImageFile", "Invalid image file. Only JPG, PNG, or GIF under 5MB allowed.");
                        await PopulateDropdowns(@event.EventTypeId, @event.VenueId, cancellationToken);
                        return View(@event);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingEvent.ImageUrl))
                    {
                        await _blobStorageService.DeleteImageFromBlob(existingEvent.ImageUrl);
                    }

                    // Upload new image
                    existingEvent.ImageUrl = await _blobStorageService.UploadImageToBlob(@event.ImageFile);
                }

                _context.Update(existingEvent);
                await _context.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await EventExistsAsync(@event.EventId, cancellationToken))
                {
                    return NotFound();
                }
                _logger.LogError(ex, $"Concurrency error updating event ID {id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating event ID {id}");
                await PopulateDropdowns(@event.EventTypeId, @event.VenueId, cancellationToken);
                return RedirectToAction("Error", "Home", new { message = $"Failed to update event {id}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var @event = await _context.Events
                    .Include(e => e.EventType)
                    .Include(e => e.Venue)
                    .FirstOrDefaultAsync(m => m.EventId == id, cancellationToken);

                if (@event == null)
                {
                    return NotFound();
                }

                if (await _context.Bookings.AnyAsync(b => b.EventId == id, cancellationToken))
                {
                    TempData["ErrorMessage"] = "Cannot delete event with existing bookings";
                    return RedirectToAction(nameof(Index));
                }

                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading delete view for event ID {id}");
                return RedirectToAction("Error", "Home", new { message = $"Failed to load delete confirmation for event {id}" });
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var @event = await _context.Events.FindAsync(new object[] { id }, cancellationToken);
                if (@event == null)
                {
                    return NotFound();
                }

                if (await _context.Bookings.AnyAsync(b => b.EventId == id, cancellationToken))
                {
                    TempData["ErrorMessage"] = "Cannot delete event with existing bookings";
                    return RedirectToAction(nameof(Index));
                }

                // Delete associated image
                if (!string.IsNullOrEmpty(@event.ImageUrl))
                {
                    await _blobStorageService.DeleteImageFromBlob(@event.ImageUrl);
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting event ID {id}");
                return RedirectToAction("Error", "Home", new { message = $"Failed to delete event {id}" });
            }
        }

        private async Task<bool> EventExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Events.AnyAsync(e => e.EventId == id, cancellationToken);
        }

        private bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return false;
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                return false;
            }

            // 5MB max file size
            if (file.Length > 5 * 1024 * 1024)
            {
                return false;
            }

            return true;
        }

        private async Task PopulateDropdowns(int? eventTypeId = null, int? venueId = null, CancellationToken cancellationToken = default)
        {
            ViewData["EventTypeId"] = new SelectList(
                await _context.EventTypes.ToListAsync(cancellationToken),
                "EventTypeId", "Name", eventTypeId);

            ViewData["VenueId"] = new SelectList(
                await _context.Venues.ToListAsync(cancellationToken),
                "VenueId", "VenueName", venueId);
        }
    }
}