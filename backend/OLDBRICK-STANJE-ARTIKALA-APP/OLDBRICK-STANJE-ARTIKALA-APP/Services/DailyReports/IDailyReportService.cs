using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports
{
    public interface IDailyReportService
    {
        Task<DailyReportResponseDto> CreateByDateAsync(DateOnly datum);
        Task<DailyReportResponseDto> GetByDateAsync(DateOnly datum);
    }
}
