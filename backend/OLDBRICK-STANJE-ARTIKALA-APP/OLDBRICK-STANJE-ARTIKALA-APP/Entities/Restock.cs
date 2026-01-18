namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class Restock
    {
        public long Id { get; set; }

        public int IdNaloga { get; set; }

        public int IdPiva { get; set; }

        public float Quantity { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
