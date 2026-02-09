namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class DailyCleaningSnapshot
    {
        public long Id { get; set; }
        public int IdPiva { get; set; }
        public int IdNaloga { get; set; }
        public DateOnly Datum { get; set; }
        public float BrojacStartAfterCleaning { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
