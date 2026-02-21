namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class DailyReportTotalsCache
    {
        public int IdNaloga { get; set; }
        public decimal TotalVagaPotrosnja { get; set; }
        public decimal TotalPosPotrosnja { get; set; }
        public decimal TotalProsuto { get; set; }
        public decimal TotalProsutoPoApp { get; set; }

        public DateTime CalculatedAt { get; set; }

    }
}
