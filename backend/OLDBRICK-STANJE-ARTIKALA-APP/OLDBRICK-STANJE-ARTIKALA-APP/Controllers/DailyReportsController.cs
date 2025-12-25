using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.RangeReports;
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
        public async Task<IActionResult> CreateForDate([FromBody] GetOrCreateDailyReportDto dto)
        {
            var result = await _dailyReport.CreateByDateAsync(dto.Datum);
            return Ok(result);
        }
        [HttpGet("use-date")]
        public async Task<ActionResult<DailyReportResponseDto>> GetByDate(
    [FromQuery] DateOnly datum)
        {
            try
            {
                var result = await _dailyReport.GetByDateAsync(datum);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
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
        [HttpPut("{idNaloga}/prosuto-kanta")]
        public async Task<IActionResult> UpdateProsutoKanta(int idNaloga, [FromBody] ProsutoKantaDto dto)
        {
            await _prosutoService.UpdateProsutoKantaAsync(idNaloga, dto.ProsutoKanta);
            return NoContent();
        }

        [HttpGet("{idNaloga:int}/state")]
        public async Task<ActionResult<ProsutoResultDto>> GetStates(int idNaloga)
        {
            var result = await _prosutoService.GetAllStatesByIdNaloga(idNaloga);
            return Ok(result);
        }

        [HttpPost("{idNaloga}/calculate-prosuto-razlika")]
        public async Task<IActionResult> CalculateProsutoRazlika(int idNaloga)
        {
            var razlika = await _prosutoService.CalculateAndSaveProsutoRazlikaAsync(idNaloga);
            return Ok(new
            {
                idNaloga,
                prosutoRazlika = razlika
            });
        }
        [HttpGet("dates")]
        public async Task<IActionResult> GetAllReportDates()
        {
            var dates = await _dailyReport.GetAllDatesNalogaAsync();
            return Ok(dates);
        }

        [HttpGet("by-date")]
        public async Task<IActionResult> GetToday([FromQuery] DateOnly date)
        {
            var result = await _dailyReport.GetTodayAsync(date);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("ids-by-range")]
        public async Task<IActionResult> GetIdsByRange([FromQuery] DateOnly from,  [FromQuery] DateOnly to)
        {
            var ids = await _dailyReport.GetReportIdsForRangeAsync(from, to);
            
            return Ok(ids);
        }

        [HttpGet("total-by-range")]
        public async Task<IActionResult> GetTotalsByRange([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var ids = await _dailyReport.GetReportIdsForRangeAsync(from, to);

            var (measured, app) = await _dailyReport.GetTotalsForRangeAsync(ids);

            return Ok(new
            {
                from,
                to,
                totalMEasuredProsuto = measured,
                totalAppProsuto = app,
                totalDifference = app - measured
            });

        }

        [HttpGet("range-report-for-oneBeer")]
        public async Task<ActionResult<BeerProsutoByBeerDto>> GetRangeReportForBeer(
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            if (from > to) return BadRequest("From datum ne sme biti posle To datuma.");

            var ids = await _dailyReport.GetReportIdsForRangeAsync(from, to);

            var result = await _dailyReport.GetAppProsutoByBeerForRangeAsync(ids);

            return Ok(result);
        }

        [HttpPost("{idNaloga}/calculate-prosuto-for-each-beer")]
        public async Task<IActionResult> CalculateProsutoForEachBeer(int idNaloga)
        {
            var updated = await _prosutoService.CalculateAndUpdateProsutoForReportAsync(idNaloga);
            return Ok(updated);
        }
    }

}
