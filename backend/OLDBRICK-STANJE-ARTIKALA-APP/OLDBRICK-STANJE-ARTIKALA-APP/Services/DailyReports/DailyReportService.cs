using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.RangeReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports
{
    public class DailyReportService : IDailyReportService
    {
        private readonly AppDbContext _context;
        private readonly IProsutoService _prosutoService;

        public DailyReportService(AppDbContext context, IProsutoService prosutoService)
        {
            _context = context;
            _prosutoService = prosutoService;
        }

        public async Task<DailyReportResponseDto> CreateByDateAsync(DateOnly datum)
        {
            var existing = await _context.DailyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Datum == datum);

            if (existing != null)
            {
                return new DailyReportResponseDto
                {
                    IdNaloga = existing.IdNaloga,
                    Datum = existing.Datum,
                    TotalProsuto = existing.TotalProsuto,
                    IzracunataRazlikaProsutog = existing.ProsutoRazlika,
                    IzmerenoProsutoVaga = existing.IzmerenoProsuto,

                };
            }
            var dailyReport = new DailyReport
            {
                Datum = datum,
                TotalProsuto = 0,
                ProsutoRazlika = 0,
                IzmerenoProsuto = 0,
            };

            _context.DailyReports.Add(dailyReport);
            await _context.SaveChangesAsync();

            //var hasStates = await _context.DailyBeerStates.AnyAsync(s => s.IdNaloga == dailyReport.IdNaloga);

            //if (!hasStates)
            //{
            //    var lastReset = await _context.InventoryResets.Where(r => DateOnly.FromDateTime(r.DatumPopisa.Date) < datum)
            //        .OrderByDescending(r => r.DatumPopisa).FirstOrDefaultAsync();

            //    if(lastReset != null)
            //    {
            //        var items = await _context.InventoryResetItems
            //                        .Where(i => i.InventoryResetId == lastReset.Id)
            //                        .ToListAsync();
                    
            //        if(items.Count > 0)
            //        {
            //            var newStates = items.Select(i => new DailyBeerState
            //            {
            //                IdNaloga = dailyReport.IdNaloga,
            //                IdPiva = i.IdPiva,
            //                NazivPiva = i.NazivPiva,
            //                Izmereno = i.IzmerenoSnapshot,
            //                StanjeUProgramu = i.PosSnapshot
            //            }).ToList();
            //            _context.DailyBeerStates.AddRange(newStates);
            //            await _context.SaveChangesAsync();
            //        }
            //    }
            //}

            return new DailyReportResponseDto
            {
                IdNaloga = dailyReport.IdNaloga,
                Datum = dailyReport.Datum,
                TotalProsuto = dailyReport.TotalProsuto,
                IzracunataRazlikaProsutog = dailyReport.ProsutoRazlika,
                IzmerenoProsutoVaga = dailyReport.IzmerenoProsuto
            };
        }
        public async Task<DailyReportResponseDto> GetByDateAsync(DateOnly datum)
        {
            var report = await _context.DailyReports.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Datum == datum);

            if (report == null)
            {
                throw new KeyNotFoundException($"Daily report for date {datum} not found.");
            }
            return new DailyReportResponseDto
            {
                IdNaloga = report.IdNaloga,
                Datum = report.Datum,
                TotalProsuto = report.TotalProsuto,
                IzracunataRazlikaProsutog = report.ProsutoRazlika,
                IzmerenoProsutoVaga = report.IzmerenoProsuto
            };
        }
        public async Task<List<DailyReportDateDto>> GetAllDatesNalogaAsync()
        {
            var result = await _context.DailyReports.Select(x => new DailyReportDateDto
            {
                IdNaloga = x.IdNaloga,
                Datum = x.Datum
            }).ToListAsync();

            return result;
        }

        public async Task<DailyReportDateDto?> GetTodayAsync(DateOnly date)
        {
            return await _context.DailyReports
                .Where(x => x.Datum == date)
                .Select(x => new DailyReportDateDto
                {
                    IdNaloga = x.IdNaloga,
                    Datum = x.Datum
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<int>> GetReportIdsForRangeAsync(DateOnly from, DateOnly to)
        {
            if (from > to) throw new ArgumentException("From date must be before to date");

            var ids = await _context.DailyReports.Where(r => r.Datum >= from && r.Datum <= to)
                .Select(r => r.IdNaloga)
                .ToListAsync();

            return ids;
        }

        public async Task<(float totalMeasured, float totalApp)> GetTotalsForRangeAsync(List<int> reportIds)
        {
            if (reportIds == null || reportIds.Count == 0)
                return (0f, 0f);

            var totalMeasured = await _context.DailyReports
                .Where(r => reportIds.Contains(r.IdNaloga))
                .SumAsync(s => (float?)s.IzmerenoProsuto) ?? 0f;

            var totalApp = await _context.DailyReports
                .Where(s => reportIds.Contains(s.IdNaloga))
                .SumAsync(s => (float?)(s.TotalProsuto)) ?? 0f;

            return (totalMeasured, totalApp);
        }

        public async Task<List<BeerProsutoByBeerDto>> GetAppProsutoByBeerForRangeAsync(List<int> reportIds)
        {
            if (reportIds == null || reportIds.Count == 0)
                return new List<BeerProsutoByBeerDto>();

            var result = await _context.DailyBeerStates
                .Where(s => reportIds.Contains(s.IdNaloga))
                .GroupBy(s => new { s.IdPiva, s.NazivPiva })
                .Select(g => new BeerProsutoByBeerDto
                {
                    BeerId = g.Key.IdPiva,
                    BeerName = g.Key.NazivPiva,
                    TotalAppProsuto = g.Sum(x => x.ProsutoJednogPiva)

                })
                .OrderByDescending(x => x.TotalAppProsuto)
                .ToListAsync();
            return result;
        }
        public async Task RecalculateProsutoJednogPivaAsync(int idNaloga)
        {
            var currStates = await _context.DailyBeerStates
                   .Where(s => s.IdNaloga == idNaloga)
                   .ToListAsync();

            if (currStates.Count == 0) return;

            var beerIds = currStates.Select(s => s.IdPiva).Distinct().ToList();

            var tipMer = await _context.Beers
                .Where(b => beerIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => (b.TipMerenja ?? "").Trim().ToLowerInvariant());

            var prevReportId = await _context.DailyReports
                .Where(r => r.IdNaloga < idNaloga)
                .OrderByDescending(r => r.IdNaloga)
                .Select(r => (int?)r.IdNaloga)
                .FirstOrDefaultAsync();

            if (prevReportId is null)
            {
                foreach (var s in currStates) s.ProsutoJednogPiva = 0;
                await _context.SaveChangesAsync();
                return;
            }

            var prevStates = await _context.DailyBeerStates
                .Where(s => s.IdNaloga == prevReportId.Value && beerIds.Contains(s.IdPiva))
                .ToDictionaryAsync(s => s.IdPiva, s => s);

            foreach (var curr in currStates)
            {
                prevStates.TryGetValue(curr.IdPiva, out var prev);

                if (prev == null)
                {
                    curr.ProsutoJednogPiva = 0;
                    continue;
                }
                var tip = tipMer.TryGetValue(curr.IdPiva, out var t) ? t : "";

                var deltaProgram = prev.StanjeUProgramu - curr.StanjeUProgramu;

                float deltaMeasured;

                if (tip == "kesa")
                {

                    if (curr.Izmereno <= 0 || prev.Izmereno < 0)
                    {
                        curr.ProsutoJednogPiva = 0;
                        continue;
                    }

                    deltaMeasured = curr.Izmereno - prev.Izmereno;
                }
                else
                {

                    deltaMeasured = prev.Izmereno - curr.Izmereno;
                }


                if (deltaProgram < 0) deltaProgram = 0;
                if (deltaMeasured < 0) deltaMeasured = 0;


                var prosuto = deltaMeasured - deltaProgram;
                curr.ProsutoJednogPiva = prosuto > 0 ? prosuto : 0;
            }
            await _context.SaveChangesAsync();
        }

        //UPIS (MOZDA ZATREBA!)
        public async Task<TotalPotrosnjaDto> PostTotalPotrosnjaVagaAndPOS(int idNaloga)
        {
            var (result, totalVaga, totalPos) =
                await _prosutoService.CalcProsutoForPotrosnjaVagaAndPos(idNaloga);

            var report = await _context.DailyReports
                .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);

            report.TotalProsuto = result.TotalProsuto;
            report.TotalPotrosenoVaga = MathF.Round(totalVaga, 2);
            report.TotalPotrosenoProgram = MathF.Round(totalPos, 2);

            await _context.SaveChangesAsync();

            //  OVO JE RESPONSE
            return new TotalPotrosnjaDto
            {
                IdNaloga = idNaloga,
                TotalVagaPotrosnja = MathF.Round(totalVaga, 2),
                TotalPosPotrosnja = MathF.Round(totalPos, 2)
            };
        }


        //CITANJE
        public async Task<ProsutoWithTotalPotrosnjaDto> GetTotalPotrosnjaVagaAndPOS(int idNaloga)
        {
            var (result, totalVaga, totalPos) =
        await _prosutoService.CalcProsutoForPotrosnjaVagaAndPos(idNaloga);

            return new ProsutoWithTotalPotrosnjaDto
            {
                Prosuto = result, // u sebi ima Items : List<BeerCalcResultDto>
                Totals = new TotalPotrosnjaDto
                {
                    IdNaloga = idNaloga,
                    TotalVagaPotrosnja = MathF.Round(totalVaga, 2),
                    TotalPosPotrosnja = MathF.Round(totalPos, 2)
                }
            };
        }

        public async Task<List<DayBeforeStateDto>> GetDayBeforeStates(int idNaloga)
        {
            var report = await _context.DailyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);

            if (report == null)
                throw new ArgumentException("Dnevni nalog ne postoji");

            var dayBeforeReport = await _context.DailyReports
                .AsNoTracking()
                .Where(r => r.Datum < report.Datum)
                .OrderByDescending(r => r.Datum)
                .FirstOrDefaultAsync();

            if (dayBeforeReport == null)
                return new List<DayBeforeStateDto>();

            var prevDate = dayBeforeReport.Datum;     // DateOnly
            var prevIdNaloga = dayBeforeReport.IdNaloga;

            // 0) NOVO: snapshot dopune za prethodni nalog -> to je "juce stanje"
            var snapRows = await _context.DailyRestockSnapshots
                        .AsNoTracking()
                        .Where(x => x.IdNaloga == idNaloga && x.SourceIdNaloga == prevIdNaloga)
                        .Select(x => new
                        {
                            x.IdPiva,
                            x.IzmerenoSnapshot,
                         x.PosSnapshot,
                            x.UpdatedAt,
                            x.CreatedAt
                        })
                        .ToListAsync();

            // Ako ima snapshot-a, uzmi najnoviji po pivu (u memoriji)
            if (snapRows.Count > 0)
            {
                var latestByBeer = snapRows
                    .GroupBy(x => x.IdPiva)
                    .Select(g => g
                        .OrderByDescending(x => x.UpdatedAt)
                        .ThenByDescending(x => x.CreatedAt)
                        .First())
                    .ToList();

                var beerIds = latestByBeer.Select(x => x.IdPiva).Distinct().ToList();

                var beers = await _context.Beers
                    .AsNoTracking()
                    .Where(b => beerIds.Contains(b.Id))
                    .ToDictionaryAsync(b => b.Id, b => new { b.NazivPiva, b.TipMerenja });

                var snapshotStates = latestByBeer
                    .Select(x => new DayBeforeStateDto
                    {
                        IdPiva = x.IdPiva,
                        NazivPiva = beers.TryGetValue(x.IdPiva, out var b) ? b.NazivPiva : "",
                        TipMerenja = beers.TryGetValue(x.IdPiva, out var b2) ? b2.TipMerenja : "",
                        PrevVaga = x.IzmerenoSnapshot,
                        PrevPos = x.PosSnapshot
                    })
                    .ToList();

                return snapshotStates;
            }


            // 1) Provera: da li je BIO POPIS na taj prethodni datum?
            // DatumPopisa je timestamptz => pravimo UTC range za ceo dan
            var prevStartUtc = new DateTime(prevDate.Year, prevDate.Month, prevDate.Day, 0, 0, 0, DateTimeKind.Utc);
            var prevEndUtc = new DateTime(prevDate.Year, prevDate.Month, prevDate.Day, 23, 59, 59, 999, DateTimeKind.Utc);

            var restartThatDay = await _context.InventoryResets
                .AsNoTracking()
                .Where(x => x.DatumPopisa >= prevStartUtc && x.DatumPopisa <= prevEndUtc)
                .OrderByDescending(x => x.DatumPopisa)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            // Ako je prethodni dan bio POPIS, vracamo snapshot iz inventory_reset_items
            if (restartThatDay != null)
            {
                var snapshot = await _context.InventoryResetItems
                    .AsNoTracking()
                    .Where(i => i.InventoryResetId == restartThatDay.Id)
                    .Join(_context.Beers,
                        i => i.IdPiva,
                        b => b.Id,
                        (i, b) => new DayBeforeStateDto
                        {
                            IdPiva = i.IdPiva,
                            NazivPiva = b.NazivPiva,
                            TipMerenja = b.TipMerenja,
                            PrevVaga = Convert.ToSingle(i.IzmerenoSnapshot),
                            PrevPos = Convert.ToSingle(i.PosSnapshot)
                        })
                    .ToListAsync();

                return snapshot;
            }

            // 2) Inace: klasika TAB3 za prethodni nalog
            var result = await _context.DailyBeerStates
                .AsNoTracking()
                .Where(s => s.IdNaloga == prevIdNaloga)
                .Join(_context.Beers,
                    s => s.IdPiva,
                    b => b.Id,
                    (s, b) => new DayBeforeStateDto
                    {
                        IdPiva = s.IdPiva,
                        NazivPiva = b.NazivPiva,
                        TipMerenja = b.TipMerenja,
                        PrevVaga = s.Izmereno,
                        PrevPos = s.StanjeUProgramu
                    })
                .ToListAsync();

            return result;
        }



        public async Task<PotrosnjaSinceLastInventoryDto> GetTotalsSinceLastInventoryAsync(int idNaloga)
        {
            if (idNaloga <= 0) throw new ArgumentException("IdNaloga nije validan.");

            // 1) "to" = datum naloga (front ne treba da zna datum)
            var to = await _context.DailyReports
                .Where(r => r.IdNaloga == idNaloga)
                .Select(r => (DateOnly?)r.Datum)
                .FirstOrDefaultAsync();

            if (to == null)
                throw new ArgumentException("Nalog ne postoji.");

            var toDate = to.Value;
            var toStartUtc = DateTime.SpecifyKind(
                                 toDate.ToDateTime(TimeOnly.MinValue),
                                 DateTimeKind.Utc
                                );

            // 2) poslednji popis
            var lastReset = await _context.InventoryResets
                                  .AsNoTracking()
                                  .Where(x => x.DatumPopisa < toStartUtc)
                                  .OrderByDescending(x => x.DatumPopisa)
                                  .ThenByDescending(x => x.Id)
                                  .FirstOrDefaultAsync();

            // 3) "from" = sutradan posle popisa (popis uvece vecinski)
            DateOnly from;
            if (lastReset == null)
            {
                var first = await _context.DailyReports
                    .OrderBy(r => r.Datum)
                    .Select(r => (DateOnly?)r.Datum)
                    .FirstOrDefaultAsync();

                from = first ?? to.Value;
            }
            else
            {
                var resetDate = DateOnly.FromDateTime(lastReset.DatumPopisa.Date);
                from = resetDate.AddDays(1);
            }

            // 4) ako popis presece tako da nema opsega
            if (from > to.Value)
            {
                return new PotrosnjaSinceLastInventoryDto
                {
                    From = from,
                    To = to.Value,
                    TotalVagaFromInventoryPotrosnja = 0f,
                    TotalPosFromInventoryPotrosnja = 0f,
                    TotalFromInventoryProsuto = 0f
                };
            }

            // 5) ids u periodu od do
            var ids = await _context.DailyReports
                .Where(r => r.Datum >= from && r.Datum <= to.Value)
                .Select(r => r.IdNaloga)
                .ToListAsync();

            // 6) Otpis (rucno prosuto) u periodu
            var totalOtpis = await _context.DailyReports
                .Where(r => r.Datum >= from && r.Datum <= to.Value)
                .SumAsync(r => (float?)r.IzmerenoProsuto) ?? 0f;

            // 7) saberi vaga/pos potrošnju po danima, imam tu metodu
            float totalVaga = 0f;
            float totalPos = 0f;

            foreach (var dayIdNaloga in ids)
            {
                var (_, dayVaga, dayPos) = await _prosutoService.CalcProsutoForPotrosnjaVagaAndPos(dayIdNaloga);
                totalVaga += dayVaga;
                totalPos += dayPos;
            }

            return new PotrosnjaSinceLastInventoryDto
            {
                From = from,
                To = to.Value,
                TotalVagaFromInventoryPotrosnja = totalVaga,
                TotalPosFromInventoryPotrosnja = totalPos,
                TotalFromInventoryProsuto = totalOtpis
                // TotalFromInventoryProsutoPoApp computed u DTO (POS - VAGA) razlika ->  manjak
            };
        }

        public async Task<object> CreateInventoryDate([FromBody] CreateInventoryResetDto dto)
        {
            if (dto == null) throw new ArgumentException("Body je prazan.");
            if (dto.DatumPopisa == default) throw new ArgumentException("DatumPopisa nije validan.");

            // 1) Provera da popis za taj datum vec ne postoji
            var exists = await _context.InventoryResets
                .AnyAsync(x => DateOnly.FromDateTime(x.DatumPopisa.Date) == dto.DatumPopisa);

            if (exists) throw new ArgumentException("Popis za taj datum već postoji.");

            // 2) Nadji dnevni nalog (TAB2) za datum popisa
            var report = await _context.DailyReports
                .FirstOrDefaultAsync(x => x.Datum == dto.DatumPopisa);

            if (report == null)
                throw new ArgumentException("Ne postoji dnevni nalog za izabrani datum");

            // 3) Ucitaj stanja (TAB3) za taj nalog (popis dan)
            var states = await _context.DailyBeerStates
                .Where(s => s.IdNaloga == report.IdNaloga)
                .ToListAsync();

            if (states.Count == 0)
                throw new ArgumentException("Nema unetih stanja za taj nalog.");

            // 4) Ucitaj piva i tip merenja (normalizacija posle query)
            var beerIds = states.Select(s => s.IdPiva).Distinct().ToList();

            var beersRaw = await _context.Beers
                .Where(b => beerIds.Contains(b.Id))
                .Select(b => new { b.Id, b.NazivPiva, b.TipMerenja })
                .ToListAsync();

            var beers = beersRaw.Select(b => new
            {
                b.Id,
                b.NazivPiva,
                Tip = ((b.TipMerenja ?? "").Trim().ToLower()) // <-- posle query, bez ToLowerInvariant u SQL-u
            }).ToList();

            var nazivMap = beers.ToDictionary(x => x.Id, x => x.NazivPiva);

            var tipMap = beers.ToDictionary(x => x.Id, x => x.Tip);

            // 5) KESA items (kao do sad)
            var kesaItems = beers
                .Where(b => b.Tip == "kesa")
                .Select(b => new KesaItemDto
                {
                    IdPiva = b.Id,
                    NazivPiva = b.NazivPiva
                })
                .ToList();

            var kesaIds = kesaItems.Select(x => x.IdPiva).ToHashSet();

            // 6) Ako ima KESA artikala, validiraj overrides (ali NE menjaj states za datum popisa)
            Dictionary<int, float> kesaOverrideMap = new();

            if (kesaIds.Count > 0)
            {
                if (dto.KesaPosOverrides == null || dto.KesaPosOverrides.Count == 0)
                    throw new ArgumentException("Morate uneti POS vrednosti za KESA artikle");

                var providedIds = new HashSet<int>();

                foreach (var o in dto.KesaPosOverrides)
                {
                    if (o.IdPiva <= 0) throw new ArgumentException("IdPiva za KESA nije validan.");
                    if (o.PosValue < 0) throw new ArgumentException("POS vrednost za KESA ne sme biti negativna.");

                    providedIds.Add(o.IdPiva);
                }

                if (!kesaIds.SetEquals(providedIds))
                    throw new ArgumentException("Morate uneti POS vrednosti za sva KESA piva tj. artikle");

                kesaOverrideMap = dto.KesaPosOverrides.ToDictionary(x => x.IdPiva, x => x.PosValue);
            }

            // 7) Snimi INVENTORY RESET zapis (kao do sad)
            var datumPopisaUtc = new DateTime(
                dto.DatumPopisa.Year,
                dto.DatumPopisa.Month,
                dto.DatumPopisa.Day,
                0, 0, 0,
                DateTimeKind.Utc
            );

            var entity = new InventoryReset
            {
                DatumPopisa = datumPopisaUtc,
                Napomena = dto.Napomena,
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryResets.Add(entity);
            await _context.SaveChangesAsync();

            var resetItems = new List<InventoryResetItem>();

            foreach(var s in states)
            {
                var tip = tipMap.TryGetValue(s.IdPiva, out var t) ? t : "";
                var naziv = nazivMap.TryGetValue(s.IdPiva, out var n) ? n : null;

                var izmerenoSnapshot = s.Izmereno;
                float posSnapshot;

                if(tip == "kesa")
                {
                    if(!kesaOverrideMap.TryGetValue(s.IdPiva, out var posVal))
                    {
                        throw new ArgumentException("Morate uneti POS vrednost za sva piva koja se mere iz kese.");

                        
                    }
                    posSnapshot = posVal;
                }
                else
                {
                    posSnapshot = izmerenoSnapshot;
                }

                resetItems.Add(new InventoryResetItem
                {
                    InventoryResetId = entity.Id,
                    IdPiva = s.IdPiva,
                    NazivPiva = naziv,
                    IzmerenoSnapshot = izmerenoSnapshot,
                    PosSnapshot = posSnapshot
                });
            }

            _context.InventoryResetItems.AddRange(resetItems);
            await _context.SaveChangesAsync();

            return new
            {
                id = entity.Id,
                kesaItems
            };
        }


        public async Task<List<KesaItemDto>> GetKesaitemsForDateAsync(DateOnly datum)
        {
            var report = await _context.DailyReports.FirstOrDefaultAsync(x => x.Datum == datum);

            if (report == null)
                throw new ArgumentException("Ne postoji dnevni nalog za izabrani datum.");

            var beerIds = await _context.DailyBeerStates
                .Where(s => s.IdNaloga == report.IdNaloga)
                .Select(s => s.IdPiva)
                .Distinct()
                .ToListAsync();
            var kesaItems = await _context.Beers
                .Where(b => beerIds.Contains(b.Id))
                .Select(b => new
                {
                    b.Id,
                    b.NazivPiva,
                    Tip = (b.TipMerenja ?? "").Trim().ToLower()
                 })
                .Where(x => x.Tip == "kesa")
                 .Select(x => new KesaItemDto
                 {
                     IdPiva = x.Id,
                     NazivPiva = x.NazivPiva
                 })
                 .ToListAsync();
            
                       return kesaItems;

        }

  

    }
}
