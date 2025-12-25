using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;

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
    }
}
