namespace EventEase.Models.ViewModels
{
    public class SearchFilters
    {
        public int? EventTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool VenueAvailable { get; set; }
    }
}