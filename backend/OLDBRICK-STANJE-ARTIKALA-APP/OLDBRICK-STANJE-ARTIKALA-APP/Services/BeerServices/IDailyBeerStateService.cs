using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices
{
    public interface IDailyBeerStateService
    {
        Task<List<DailyBeerState>> UpsertForReportAsync(int idNaloga, List<UpsertDailyBeerStateDto> items);
        Task<DailyBeerState?> AddQuantityAsync(int idNaloga, int idPiva, float kolicina);

        Task<List<DailyBeerState>> AddQuantityBatchAsync(int idNaloga, List<AddMoreBeerQuantityDto> items);

        Task DeleteReportAsync(int idNaloga);
        Task<ProsutoResultDto> UpdateStatesAndRecalculateAsync(int idNaloga, List<UpdateDailyBeerStateDto> items);

        Task<DailyCleaningSnapshot> UpsertCleaningSnapshotAsync(UpsertCleaningSnapshotDto dto);

        Task<List<DailyCleaningSnapshot>> UpsertCleaningSnapshotsBatchAsync(
           UpsertCleaningSnapshotBatchDto dto);
    }
}
