using EventEase.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                if (context.Venues.Any() || context.Events.Any() || context.Bookings.Any())
                {
                    return; // DB has been seeded
                }

                // Add sample venues
                var venues = new Venue[]
                {
                    new Venue { VenueName = "Grand Ballroom", Location = "Downtown", Capacity = 500, ImageUrl = "https://example.com/ballroom.jpg" },
                    new Venue { VenueName = "Conference Center", Location = "Business District", Capacity = 300, ImageUrl = "https://example.com/conference.jpg" },
                    new Venue { VenueName = "Garden Pavilion", Location = "City Park", Capacity = 200, ImageUrl = "https://example.com/garden.jpg" }
                };
                await context.Venues.AddRangeAsync(venues);
                await context.SaveChangesAsync();

                // Add sample events
                var events = new Event[]
                {
                    new Event { EventName = "Tech Conference 2023", EventDate = DateTime.Now.AddDays(30), Description = "Annual technology conference", VenueId = venues[1].VenueId, EventTypeId = 1 },
                    new Event { EventName = "Summer Wedding", EventDate = DateTime.Now.AddDays(45), Description = "Outdoor summer wedding", VenueId = venues[2].VenueId, EventTypeId = 2 },
                    new Event { EventName = "Music Festival", EventDate = DateTime.Now.AddDays(60), Description = "Weekend music festival", VenueId = venues[0].VenueId, EventTypeId = 3 }
                };
                await context.Events.AddRangeAsync(events);
                await context.SaveChangesAsync();

                // Add sample bookings
                var bookings = new Booking[]
                {
                    new Booking { EventId = events[0].EventId, VenueId = events[0].VenueId!.Value, CustomerName = "John Smith", CustomerContact = "555-1234" },
                    new Booking { EventId = events[1].EventId, VenueId = events[1].VenueId!.Value, CustomerName = "Sarah Johnson", CustomerContact = "555-5678" },
                    new Booking { EventId = events[2].EventId, VenueId = events[2].VenueId!.Value, CustomerName = "Mike Brown", CustomerContact = "555-9012" }
                };
                await context.Bookings.AddRangeAsync(bookings);
                await context.SaveChangesAsync();
            }
        }
    }
}