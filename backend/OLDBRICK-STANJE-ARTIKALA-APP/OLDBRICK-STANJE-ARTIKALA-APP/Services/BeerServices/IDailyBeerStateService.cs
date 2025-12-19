using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices
{
    public interface IDailyBeerStateService
    {
        Task<List<DailyBeerState>> UpsertForReportAsync(int idNaloga, List<UpsertDailyBeerStateDto> items);
    }
}
