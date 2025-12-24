using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.RangeReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.RangeReports
{
    public class BeerProsutoRangeReportDto
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public float TotalMeasuredProsuto { get; set; } 
        public float TotalAppProsuto { get; set; }
        public float TotalDifference => TotalMeasuredProsuto - TotalAppProsuto;
        public List<BeerProsutoByBeerDto> ByBeer { get; set; } = new();
    }
}
