using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices
{
    public interface IProsutoService
    {
        Task<ProsutoResultDto> CalculateAndSaveAsync(int idNaloga);
    }
}
