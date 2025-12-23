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
    }

}
