using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventEase.Models;
using EventEase.Data;
using EventEase.Models.ViewModels;

namespace EventEase.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(ApplicationDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var bookings = _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                bookings = bookings.Where(b =>
                    b.BookingId.ToString().Contains(searchString) ||
                    b.Event!.EventName.Contains(searchString));
            }

            var bookingDetails = await bookings
                .Select(b => new BookingDetailsViewModel
                {
                    BookingId = b.BookingId,
                    EventName = b.Event!.EventName,
                    VenueName = b.Venue!.VenueName,
                    Location = b.Venue.Location,
                    EventDate = b.Event.EventDate,
                    CustomerName = b.CustomerName,
                    CustomerContact = b.CustomerContact,
                    BookingDate = b.BookingDate
                })
                .ToListAsync();

            return View(bookingDetails);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null) return NotFound();

            var bookingDetails = new BookingDetailsViewModel
            {
                BookingId = booking.BookingId,
                EventName = booking.Event!.EventName,
                VenueName = booking.Venue!.VenueName,
                Location = booking.Venue.Location,
                EventDate = booking.Event.EventDate,
                CustomerName = booking.CustomerName,
                CustomerContact = booking.CustomerContact,
                BookingDate = booking.BookingDate
            };

            return View(bookingDetails);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["EventId"] = new SelectList(await _context.Events.ToListAsync(), "EventId", "EventName");
            ViewData["VenueId"] = new SelectList(await _context.Venues.ToListAsync(), "VenueId", "VenueName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,EventId,VenueId,CustomerName,CustomerContact")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                booking.BookingDate = DateTime.Now;
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking.EventId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName", booking.VenueId);
            return View(booking);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            return booking == null ? NotFound() : View(booking);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}