namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports
{
    public class DailyReportResponseDto
    {
        public int IdNaloga { get; set; }
        public DateOnly Datum { get; set; }
        public float Prosuto { get; set; }
    }
}
