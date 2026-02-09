namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers
{
    public class UpsertCleaningSnapshotDto
    {
        public int IdPiva { get; set; }
        public int IdNaloga { get; set; }
        public DateOnly Datum { get; set; }
        public float BrojacStart { get; set; }

    }
}
