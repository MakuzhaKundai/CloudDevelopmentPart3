using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [StringLength(100, ErrorMessage = "Event name cannot exceed 100 characters")]
        public string EventName { get; set; } = null!;

        [Required(ErrorMessage = "Event date is required")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = null!;

        public int? VenueId { get; set; }
        public Venue? Venue { get; set; }

        public int EventTypeId { get; set; }
        public EventType? EventType { get; set; }

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is DateTime date && date > DateTime.Now;
        }
    }
}