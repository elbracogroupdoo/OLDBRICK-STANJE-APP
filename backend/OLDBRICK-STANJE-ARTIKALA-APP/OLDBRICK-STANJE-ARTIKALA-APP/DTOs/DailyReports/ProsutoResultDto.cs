using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;

namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports
{
    public class ProsutoResultDto
    {
        public int IdNaloga { get; set; }
        public float TotalProsuto { get; set; } // L
        public float ProsutoKanta { get; set; }
        public List<BeerCalcResultDto> Items { get; set; } = new();

       
    }
}
