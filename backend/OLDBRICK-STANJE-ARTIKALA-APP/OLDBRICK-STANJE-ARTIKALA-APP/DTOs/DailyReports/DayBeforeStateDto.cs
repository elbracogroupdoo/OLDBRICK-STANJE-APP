namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports
{
    public class DayBeforeStateDto
    {
        public int IdPiva { get; set; }
        public string NazivPiva { get; set; }
        public string TipMerenja { get; set; }
        public float? PrevVaga { get; set; }
        public float? PrevPos { get; set; }
    }
}
