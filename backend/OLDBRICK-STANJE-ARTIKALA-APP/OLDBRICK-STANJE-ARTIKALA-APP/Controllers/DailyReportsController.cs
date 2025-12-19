using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DailyReportsController : ControllerBase
    {

        private readonly IDailyReportService _dailyReport;
        private readonly IDailyBeerStateService _stateService;
        private readonly IProsutoService _prosutoService;

        public DailyReportsController(IDailyReportService dailyReport, IDailyBeerStateService stateService,
            IProsutoService prosutoService)
        {
            _dailyReport = dailyReport;
            _stateService = stateService;
            _prosutoService = prosutoService;
        }

        [HttpPost("for-date")]
        public async Task<IActionResult> GetorCreateForDate([FromBody] GetOrCreateDailyReportDto dto)
        {
            var result = await _dailyReport.GetorCreateByDateAsync(dto.Datum);
            return Ok(result);
        }

        [HttpPost("{idNaloga:int}/states")]
        public async Task<IActionResult> UpsertStates(int idNaloga, [FromBody] List<UpsertDailyBeerStateDto> items)
        {
            try
            {
                var saved = await _stateService.UpsertForReportAsync(idNaloga, items);
                return Ok(saved);
            }
            catch(ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{idNaloga:int}/calculate-prosuto")]
        public async Task<IActionResult> CalculateProsuto(int idNaloga)
        {
            try
            {
                var res = await _prosutoService.CalculateAndSaveAsync(idNaloga);
                return Ok(res);
            }catch(ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}
