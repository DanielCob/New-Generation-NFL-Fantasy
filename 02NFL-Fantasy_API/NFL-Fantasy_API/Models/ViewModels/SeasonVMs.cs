namespace NFL_Fantasy_API.Models.ViewModels
{
    public class SeasonVM
    {
        public int SeasonID { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SeasonWeekVM
    {
        public int SeasonID { get; set; }
        public byte WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
