using Microsoft.AspNetCore.Mvc;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestockController : Controller
    {
       private readonly IRestockService _restockService;

       public RestockController(IRestockService restockService)
        {
            _restockService = restockService;
        }

        [HttpGet("nalog/{idNaloga}/restocks")]
        public async Task<ActionResult<List<RestockForReportDto>>> GetRestocksForNalog(int idNaloga)
        {
            var result = await _restockService.GetRestocksForNalogAsync(idNaloga);
            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteRestock(int id)
        {
            var deleted = await _restockService.DeleteRestockByIdAsync(id);

            if (!deleted)
            {
                return NotFound($"Restock sa id {id} nije pronađen.");
            }

            return NoContent();
        }

    }

    
}
