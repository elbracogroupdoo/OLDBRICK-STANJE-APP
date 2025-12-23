namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class DailyBeerState
    {
        public int IdStanja { get; set; }              // id_stanja
        public DateTime CreatedAt { get; set; }        // created_at

        public int IdNaloga { get; set; }              // id_naloga (TAB2)
        public int IdPiva { get; set; }                // id_piva (TAB1)

        public string NazivPiva { get; set;}           // naziv_piva [TAB3]

        public float Izmereno { get; set; }          // izmereno (VAGA_END)
        public float StanjeUProgramu { get; set; }   // stanje_u_programu (POS_END)
        
    }
}
