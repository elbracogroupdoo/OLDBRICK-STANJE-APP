namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports
{
    public class PotrosnjaSinceLastInventoryDto
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public float TotalVagaFromInventoryPotrosnja { get; set; }
        public float TotalPosFromInventoryPotrosnja { get; set; }

        //otpis -> TotalFromInventoryProsuto
        public float TotalFromInventoryProsuto { get; set; }

        //manjak vaga-pos -> TotalFromInventoryProsutoPoApp
        public float TotalFromInventoryProsutoPoApp => TotalPosFromInventoryPotrosnja - TotalVagaFromInventoryPotrosnja;
    }
}
