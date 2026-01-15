namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class InventoryResetItem
    {
        public int Id { get; set; }

        public long InventoryResetId { get; set; }

        public int IdPiva { get; set; }

        public string NazivPiva { get; set; }

        public float IzmerenoSnapshot { get; set; }

        public float PosSnapshot { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
