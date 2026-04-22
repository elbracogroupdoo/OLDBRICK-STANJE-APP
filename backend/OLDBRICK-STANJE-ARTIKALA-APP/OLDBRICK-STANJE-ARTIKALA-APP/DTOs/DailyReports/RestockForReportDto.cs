namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports
{
    public class RestockForReportDto
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int IdNaloga { get; set; }
        public int IdPiva { get; set; }
        public string NazivPiva { get; set; } = string.Empty;
        public float Quantity { get; set; }
    }
}
