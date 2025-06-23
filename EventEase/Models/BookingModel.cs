
using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        [Required(ErrorMessage = "Booking date is required")]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string CustomerName { get; set; } = null!;

        [Required(ErrorMessage = "Customer contact is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Contact cannot exceed 20 characters")]
        public string CustomerContact { get; set; } = null!;

        [Required(ErrorMessage = "Number of tickets is required")]
        [Range(1, 100, ErrorMessage = "Must book between 1-100 tickets")]
        public int NumberOfTickets { get; set; } = 1;
    }
}