namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class DailyReport
    {
        public int IdNaloga { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateOnly Datum {  get; set; }
        public float Prosuto { get; set; }
    }
}
