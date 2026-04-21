namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers
{
    public class CreateBeerResponseDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string NazivPiva { get; set; } = null!;
        public string TipMerenja { get; set; } = null!;
    }
}
