namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers
{
    public class UpsertDailyBeerStateDto
    {

        public int BeerId { get; set; }                 
        public float Izmereno { get; set; }           
        public float StanjeUProgramu { get; set; }
    }
}
