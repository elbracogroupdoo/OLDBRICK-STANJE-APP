namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports
{
    public class CreateInventoryResetDto
    {
        public DateOnly DatumPopisa { get; set; }
        public string? Napomena { get; set; }

        public List<KesaPosOverrideDto> KesaPosOverrides { get; set; }
    }
}
