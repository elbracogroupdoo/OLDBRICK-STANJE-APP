namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers
{
    public class UpsertCleaningSnapshotBatchDto
    {
        public DateOnly Datum { get; set; }
        public int IdNaloga { get; set; }
        public List<Item> Items { get; set; } = new();

        public class Item
        {
            public int IdPiva { get; set; }
            public float BrojacStart { get; set; }
        }
    }
}
