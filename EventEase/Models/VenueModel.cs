using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    public class Venue
    {
        [Key]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Venue name is required")]
        [StringLength(100, ErrorMessage = "Venue name cannot exceed 100 characters")]
        public string VenueName { get; set; } = null!;

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = null!;

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        public int Capacity { get; set; }

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}