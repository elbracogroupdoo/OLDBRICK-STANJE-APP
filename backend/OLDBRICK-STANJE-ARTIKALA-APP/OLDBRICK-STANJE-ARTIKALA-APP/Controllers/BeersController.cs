using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BeersController : ControllerBase
    {
        private readonly IBeerService _beerService;
        

        public BeersController(IBeerService beerService)
        {
            _beerService = beerService;
        }

        [HttpPost("addNewBeer")]
        public async Task<IActionResult> CreateBeer([FromBody] CreateBeerRequestDto requestDto)
        {
            try
            {
                var beer = await _beerService.CreateAsync(requestDto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = beer.Id },
                    beer
                    );
            }catch(ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var beer = await _beerService.GetByIdAsync(id);
            if (beer == null) return NotFound();
            return Ok(beer);
        }

        [HttpGet("allArticles")]
        public async Task<IActionResult> GetAll()
        {
            var allBeers = await _beerService.GetAllBeersAsync();
            return Ok(allBeers);
        }

        [HttpPost("{idNaloga:int}/save-daily-beer-shortage")]
        public async Task<IActionResult> SaveDailyBeerShortage(int idNaloga)
        {
            await _beerService.SaveDailyBeerShortageAsync(idNaloga);
            return Ok(new { message = "Manjak po pivu je uspešno sačuvan." });
        }

        [HttpGet("{idNaloga:int}/beer-shortage-since-last-inventory")]
        public async Task<IActionResult> GetBeerShortageSinceLastInventory(int idNaloga)
        {
            var result = await _beerService
                .GetBeerShortageTotalsSinceLastInventoryAsync(idNaloga);

            return Ok(result);
        }

        [HttpPut("update-active-states")]
        public async Task<IActionResult> UpdateActiveStates([FromBody] UpdateBeerActiveStatesRequestDto requestDto)
        {
            try
            {
                await _beerService.UpdateBeerActiveStatesAsync(requestDto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
