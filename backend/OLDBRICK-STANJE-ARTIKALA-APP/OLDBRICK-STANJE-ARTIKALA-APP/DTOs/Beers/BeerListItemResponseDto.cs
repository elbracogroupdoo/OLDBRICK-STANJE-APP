namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers
{
    public class BeerListItemResponseDto
    {
        public int Id { get; set; }
        public string NazivPiva { get; set; } = null!;
        public string TipMerenja { get; set; } = null!;
        public bool IsActive { get; set; }

    }
}
