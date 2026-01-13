namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class InventoryReset
    {
        public long Id { get; set; }
        public DateTime DatumPopisa { get; set; }
        public string? Napomena { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
