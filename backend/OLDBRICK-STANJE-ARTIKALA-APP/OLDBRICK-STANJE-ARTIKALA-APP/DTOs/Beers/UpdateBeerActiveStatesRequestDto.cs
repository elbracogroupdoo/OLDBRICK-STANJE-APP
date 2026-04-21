namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers
{
    public class UpdateBeerActiveStatesRequestDto
    {
        public List<UpdateBeerActiveStateItemDto> Beers { get; set; } = new();
    }
}
