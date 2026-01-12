using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.RangeReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports
{
    public interface IDailyReportService
    {
        Task<DailyReportResponseDto> CreateByDateAsync(DateOnly datum);
        Task<DailyReportResponseDto> GetByDateAsync(DateOnly datum);
        Task<List<DailyReportDateDto>> GetAllDatesNalogaAsync();
        Task<DailyReportDateDto> GetTodayAsync(DateOnly date);
        Task<List<int>> GetReportIdsForRangeAsync(DateOnly from, DateOnly to);
        Task<(float totalMeasured, float totalApp)> GetTotalsForRangeAsync(List<int> reportIds);
        Task<List<BeerProsutoByBeerDto>> GetAppProsutoByBeerForRangeAsync(List<int> reportIds);
        Task RecalculateProsutoJednogPivaAsync(int idNaloga);
        Task<TotalPotrosnjaDto> PostTotalPotrosnjaVagaAndPOS(int idNaloga);

        Task<ProsutoWithTotalPotrosnjaDto> GetTotalPotrosnjaVagaAndPOS(int idNaloga);

        Task<List<DayBeforeStateDto>> GetDayBeforeStates(int idNaloga);

    }
}
