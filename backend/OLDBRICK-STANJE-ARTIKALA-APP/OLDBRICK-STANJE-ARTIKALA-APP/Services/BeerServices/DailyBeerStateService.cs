using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices
{
    public class DailyBeerStateService : IDailyBeerStateService
    {
        public readonly AppDbContext _context;

        public DailyBeerStateService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DailyBeerState>>UpsertForReportAsync(int idNaloga, List<UpsertDailyBeerStateDto> items)
        {
            if(items == null || items.Count == 0) 
                throw new ArgumentException("Lista svaki je prazna.");

            var reportExists = await _context.DailyReports.AnyAsync(x => x.IdNaloga == idNaloga);
            if(!reportExists)
                throw new ArgumentException("Dnevni nalog ne postoji.");

            var beerIds = items.Select(x => x.BeerId).Distinct().ToList();

            var beers = await _context.Beers.Where(b => beerIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.NazivPiva);

            var result = new List<DailyBeerState>();

            foreach( var dto in items)
            {
                if (dto.BeerId <= 0) throw new ArgumentException("BeerId nije validan.");
                if (dto.Izmereno < 0) throw new ArgumentException("Izmereno ne može biti negativno.");
                if (dto.StanjeUProgramu < 0) throw new ArgumentException("Stanje u programu ne može biti negativno.");
                if (!beers.TryGetValue(dto.BeerId, out var beerName))
                    throw new ArgumentException($"Pivo sa ID {dto.BeerId} ne postoji.");


                var existing = await _context.DailyBeerStates
                    .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga && x.IdPiva == dto.BeerId);

                if(existing == null)
                {
                    var state = new DailyBeerState
                    {
                        IdNaloga = idNaloga,
                        IdPiva = dto.BeerId,
                        NazivPiva = beerName,
                        Izmereno = dto.Izmereno,
                        StanjeUProgramu = dto.StanjeUProgramu
                    };

                    _context.DailyBeerStates.Add(state);
                    result.Add(state);
                }
                else
                {
                    existing.NazivPiva = beerName;
                    existing.Izmereno = dto.Izmereno;
                    existing.StanjeUProgramu = dto.StanjeUProgramu;
                    result.Add(existing);
                }

               
            }
            await _context.SaveChangesAsync();
            return result;
        }
    }
}
