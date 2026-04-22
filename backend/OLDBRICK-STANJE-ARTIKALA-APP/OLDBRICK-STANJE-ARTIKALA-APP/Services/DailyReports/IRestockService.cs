using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports
{
    public interface IRestockService
    {
        Task<List<RestockForReportDto>> GetRestocksForNalogAsync(int idNaloga);
        Task<bool> DeleteRestockByIdAsync(int id);
    }
}
