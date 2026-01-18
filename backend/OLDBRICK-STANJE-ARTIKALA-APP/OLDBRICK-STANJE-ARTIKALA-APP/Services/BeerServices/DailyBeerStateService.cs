using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices
{
    public class DailyBeerStateService : IDailyBeerStateService
    {
        public readonly AppDbContext _context;
        public readonly IProsutoService _prosutoService;
        public readonly IDailyReportService _dailyReportService;

        public DailyBeerStateService(AppDbContext context, IProsutoService prosutoService,
            IDailyReportService dailyReportService)
        {
            _context = context;
            _prosutoService = prosutoService;
            _dailyReportService = dailyReportService;
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

        public async Task<DailyBeerState?> AddQuantityAsync(int idNaloga, int idPiva, float kolicina)
        {
            if(idNaloga <= 0) throw new ArgumentException("IdNaloga nije validan.");
            if (idPiva <= 0) throw new ArgumentException("IdPiva nije validan.");
            if (kolicina <= 0) throw new ArgumentException("Kolicina mora biti veca od 0.");

            var state = await _context.DailyBeerStates
                .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga && x.IdPiva == idPiva);

            if (state == null) return null;

            state.Izmereno += kolicina;
            state.StanjeUProgramu += kolicina;

            await _context.SaveChangesAsync();
            return state;
        }

        public async Task<List<DailyBeerState>> AddQuantityBatchAsync(
    int idNaloga,
    List<AddMoreBeerQuantityDto> items)
        {
            if (idNaloga <= 0) throw new ArgumentException("IdNaloga nije validan.");
            if (items == null) items = new List<AddMoreBeerQuantityDto>();

            // 0) Danasnji nalog
            var todayReport = await _context.DailyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdNaloga == idNaloga);

            if (todayReport == null)
                throw new ArgumentException("Dnevni nalog ne postoji.");

            // 1) Prethodni nalog (baza)
            var prevReport = await _context.DailyReports
                .AsNoTracking()
                .Where(r => r.Datum < todayReport.Datum)
                .OrderByDescending(r => r.Datum)
                .FirstOrDefaultAsync();

            if (prevReport == null)
                throw new ArgumentException("Ne postoji prethodni dan za dopunu.");

            var prevIdNaloga = prevReport.IdNaloga;
            var prevDate = prevReport.Datum; // DateOnly

            // 2) Grupisi body stavke -> addByBeerId (sum)
            var addByBeerId = items
                .Where(x => x != null)
                .GroupBy(x => x.IdPiva)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Kolicina));

            foreach (var kv in addByBeerId)
            {
                if (kv.Key <= 0) throw new ArgumentException("IdPiva nije validan.");
                if (kv.Value < 0) throw new ArgumentException("Kolicina ne sme biti negativna.");
            }

            // 3) UZMI SVA PIVA (da upises snapshot za svako, cak i kad nije u body)
            var allBeerIds = await _context.Beers
                .AsNoTracking()
                .Select(b => b.Id)
                .ToListAsync();

            if (allBeerIds.Count == 0)
                throw new ArgumentException("Nema artikala u tabeli Beers.");

            // 4) Tip merenja za sva piva
            var tipMer = await _context.Beers
                .AsNoTracking()
                .ToDictionaryAsync(b => b.Id, b => b.TipMerenja);

            // 5) Ucitaj juce TAB3 stanja (fallback) za sva piva
            var prevStates = await _context.DailyBeerStates
                .AsNoTracking()
                .Where(s => s.IdNaloga == prevIdNaloga && allBeerIds.Contains(s.IdPiva))
                .ToListAsync();

            var prevStateByBeerId = prevStates
                .GroupBy(s => s.IdPiva)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IdStanja).First()); // ako ima duplih, uzmi latest

            // 6) Ucitaj juce popis snapshot-e (ako postoje) preko CreatedAt range-a (UTC)
            var prevDayStartUtc = DateTime.SpecifyKind(prevDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var prevNextDayStartUtc = DateTime.SpecifyKind(prevDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

            var prevReset = await _context.InventoryResets
                .AsNoTracking()
                .Where(r => r.DatumPopisa >= prevDayStartUtc && r.DatumPopisa < prevNextDayStartUtc)
                .OrderByDescending(r => r.DatumPopisa)
                .ThenByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            List<InventoryResetItem> prevResetItems = new();

            if (prevReset != null)
            {
                prevResetItems = await _context.InventoryResetItems
                    .AsNoTracking()
                    .Where(i => i.InventoryResetId == prevReset.Id && allBeerIds.Contains(i.IdPiva))
                    .ToListAsync();
            }

            var prevResetByBeerId = prevResetItems
                .GroupBy(x => x.IdPiva)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Id).First());

            // 7) Postojeci snapshot-i za DANAS (ako ih vec ima) za sva piva
            var existingTodaySnapshots = await _context.DailyRestockSnapshots
                .Where(x => x.IdNaloga == idNaloga && allBeerIds.Contains(x.IdPiva))
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();

            var todaySnapByBeerId = existingTodaySnapshots
                .GroupBy(x => x.IdPiva)
                .ToDictionary(g => g.Key, g => g.First()); // najnoviji

            // 8) Validacija: za svako pivo mora postojati baza juce (reset ili tab3)
            var missingBase = allBeerIds
                .Where(id => !prevResetByBeerId.ContainsKey(id) && !prevStateByBeerId.ContainsKey(id))
                .ToList();

            if (missingBase.Count > 0)
            {
                throw new KeyNotFoundException(
                    $"Ne postoji baza ni u inventory_reset_items ni u TAB3 za prethodni dan (IdNaloga={prevIdNaloga}, Datum={prevDate}) za IdPiva: {string.Join(", ", missingBase)}"
                );
            }

            using var tx = await _context.Database.BeginTransactionAsync();

            var result = new List<DailyBeerState>();
            var nowUtcGlobal = DateTime.UtcNow;

            // 9) KLJUC: iteriramo kroz SVA piva, add je 0 ako nije poslato u body
            foreach (var idPiva in allBeerIds)
            {
                var add = addByBeerId.TryGetValue(idPiva, out var a) ? a : 0f;

                var tip = tipMer[idPiva]?.Trim().ToLowerInvariant();

                // Restock log samo ako je stvarno bilo dopune
                if (add > 0)
                {
                    _context.Restocks.Add(new Restock
                    {
                        IdNaloga = idNaloga,
                        IdPiva = idPiva,
                        Quantity = (float)add
                    });
                }

                // Baza od JUCE (prioritet: popis -> TAB3)
                float baseIzmereno;
                float basePos;

                if (prevResetByBeerId.TryGetValue(idPiva, out var reset))
                {
                    baseIzmereno = reset.IzmerenoSnapshot;
                    basePos = reset.PosSnapshot;
                }
                else
                {
                    var st = prevStateByBeerId[idPiva];
                    baseIzmereno = st.Izmereno;
                    basePos = st.StanjeUProgramu;
                }

                // Ako vec postoji snapshot za danas -> update (cak i kad je add=0, samo UpdatedAt pomeri)
                if (todaySnapByBeerId.TryGetValue(idPiva, out var snap))
                {
                    if (add != 0)
                    {
                        snap.AddedQuantity += add;

                        if (tip == "bure" || tip == "kafa")
                        {
                            snap.IzmerenoSnapshot += add;
                            snap.PosSnapshot += add;
                        }
                        else if (tip == "kesa")
                        {
                            snap.PosSnapshot += add;
                        }
                        else
                        {
                            throw new ArgumentException("Nepoznat tip merenja za ovaj artikal.");
                        }
                    }

                    snap.UpdatedAt = DateTime.UtcNow;

                    result.Add(new DailyBeerState
                    {
                        IdNaloga = idNaloga,
                        IdPiva = idPiva,
                        Izmereno = snap.IzmerenoSnapshot,
                        StanjeUProgramu = snap.PosSnapshot
                    });

                    continue;
                }

                // Nema snapshot-a za danas -> kreiraj novi (base + add, gde add moze biti 0)
                float newIzmereno = baseIzmereno;
                float newPos = basePos;

                if (tip == "bure" || tip == "kafa")
                {
                    newIzmereno += add;
                    newPos += add;
                }
                else if (tip == "kesa")
                {
                    newPos += add;
                }
                else
                {
                    throw new ArgumentException("Nepoznat tip merenja za ovaj artikal.");
                }

                var newSnap = new DailyRestockSnapshot
                {
                    IdNaloga = idNaloga,
                    IdPiva = idPiva,
                    AddedQuantity = add,               // 0 ako nije poslato u body
                    IzmerenoSnapshot = newIzmereno,
                    PosSnapshot = newPos,
                    SourceDate = prevDate,
                    SourceIdNaloga = prevIdNaloga,
                    CreatedAt = nowUtcGlobal,
                    UpdatedAt = nowUtcGlobal
                };

                _context.DailyRestockSnapshots.Add(newSnap);

                result.Add(new DailyBeerState
                {
                    IdNaloga = idNaloga,
                    IdPiva = idPiva,
                    Izmereno = newIzmereno,
                    StanjeUProgramu = newPos
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return result;
        }





        public async Task DeleteReportAsync(int idNaloga)
        {
            if (idNaloga <= 0) throw new ArgumentException("IdNaloga nije validan.");

            var report = await _context.DailyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);

            if (report == null)
                throw new KeyNotFoundException("Dnevni nalog ne postoji.");

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // TAB3 - stanja
                var states = await _context.DailyBeerStates
                    .Where(s => s.IdNaloga == idNaloga)
                    .ToListAsync();
                _context.DailyBeerStates.RemoveRange(states);

                // Restocks log
                var restocks = await _context.Restocks
                    .Where(r => r.IdNaloga == idNaloga)
                    .ToListAsync();
                _context.Restocks.RemoveRange(restocks);

                // Daily restock snapshots
                var snaps = await _context.DailyRestockSnapshots
                    .Where(s => s.IdNaloga == idNaloga)
                    .ToListAsync();
                _context.DailyRestockSnapshots.RemoveRange(snaps);

                // Inventory resets + items (po danu naloga)
                var dayStartUtc = DateTime.SpecifyKind(report.Datum.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                var nextDayStartUtc = DateTime.SpecifyKind(report.Datum.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

                var resets = await _context.InventoryResets
                    .Where(r => r.DatumPopisa >= dayStartUtc && r.DatumPopisa < nextDayStartUtc)
                    .ToListAsync();

                if (resets.Count > 0)
                {
                    var resetIds = resets.Select(r => r.Id).ToList();

                    var resetItems = await _context.InventoryResetItems
                        .Where(i => resetIds.Contains(i.InventoryResetId))
                        .ToListAsync();

                    _context.InventoryResetItems.RemoveRange(resetItems);
                    _context.InventoryResets.RemoveRange(resets);
                }

              
                var trackedReport = await _context.DailyReports
                    .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);

                _context.DailyReports.Remove(trackedReport!);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task<ProsutoResultDto> UpdateStatesAndRecalculateAsync(int idNaloga, List<UpdateDailyBeerStateDto> items)
        {
            if (idNaloga <= 0) throw new ArgumentException("IdNaloga nije validan.");
            if (items == null || items.Count == 0) throw new ArgumentException("Items lista je prazna.");


            foreach(var dto in items)
            {
                if (dto.IdPiva <= 0) throw new ArgumentException("IdPiva nije validan.");
                if (dto.Izmereno is < 0) throw new ArgumentException("Izmereno ne sme biti negativno.");
                if (dto.StanjeUProgramu is < 0) throw new ArgumentException("Stanje u programu ne sme biti negativno.");
                if (dto.Izmereno == null && dto.StanjeUProgramu == null)
                    throw new ArgumentException("Moraš poslati bar jedno polje za update.");
            }

            var idPivaList = items.Select(x => x.IdPiva).Distinct().ToList();

            using var tx = await _context.Database.BeginTransactionAsync();

            var states = await _context.DailyBeerStates.Where(s => s.IdNaloga == idNaloga && idPivaList.Contains(s.IdPiva))
                .ToListAsync();

            var found = states.Select(s => s.IdPiva).ToHashSet();
            var missing = idPivaList.Where(id => !found.Contains(id)).ToList();
            if(missing.Count > 0)
            {
                throw new KeyNotFoundException
                    ($"Ne postoji stanje u TAB3 za IdNaloga={idNaloga} za IdPiva: " +
                    $"{string.Join(", ", missing)}");
            }

            foreach (var s in states)
            {
                var dto = items.First(x => x.IdPiva == s.IdPiva);

                if (dto.Izmereno.HasValue) s.Izmereno = dto.Izmereno.Value;
                if (dto.StanjeUProgramu.HasValue) s.StanjeUProgramu = dto.StanjeUProgramu.Value;
            }

            await _context.SaveChangesAsync();

            await _dailyReportService.RecalculateProsutoJednogPivaAsync(idNaloga);

            var result = await _prosutoService.CalculateAndSaveAsync(idNaloga);

            await tx.CommitAsync();
            return result;
        }



    }
}
