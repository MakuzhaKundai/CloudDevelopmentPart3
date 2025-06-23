namespace EventEase.Models.ViewModels
{
    public class BookingDetailsViewModel
    {
        public int BookingId { get; set; }
        public string EventName { get; set; } = null!;
        public string VenueName { get; set; } = null!;
        public string Location { get; set; } = null!;
        public DateTime EventDate { get; set; }
        public string CustomerName { get; set; } = null!;
        public string CustomerContact { get; set; } = null!;
        public DateTime BookingDate { get; set; }
        public string? EventType { get; set; }
    }
}