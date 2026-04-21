namespace OLDBRICK_STANJE_ARTIKALA_APP.Entities
{
    public class Beer
    {
        public int Id { get; set; }                 // id
        public DateTime CreatedAt { get; set; }     // created_at

        public string NazivPiva { get; set; } = null!;   // naziv_piva
        public string TipMerenja { get; set; } = null!;  // tip_merenja
        public bool IsActive { get; set; } = true;
    }
}
