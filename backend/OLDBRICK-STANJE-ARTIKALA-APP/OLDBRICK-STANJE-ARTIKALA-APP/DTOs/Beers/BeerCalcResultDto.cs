namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers
{
    public class BeerCalcResultDto
    {
        public int IdPiva { get; set; }
        public string NazivPiva { get; set; }

        public float VagaStart { get; set; }
        public float VagaEnd { get; set; }
        public float VagaPotrosnja { get; set; }

        public float PosStart { get; set; }
        public float PosEnd { get; set; }
        public float PosPotrosnja { get; set; }

        public float Odstupanje { get; set; }

    }
}
