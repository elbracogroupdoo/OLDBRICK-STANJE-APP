using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices
{
    public class ProsutoService : IProsutoService
    {
        private readonly AppDbContext _context;

        public ProsutoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProsutoResultDto> CalculateAndSaveAsync(int idNaloga)
        {
            Console.WriteLine("[OB_DEBUG] CalculateAndSaveAsync HIT");

            // 1) Ucitaj nalog (TAB2)
            var report = await _context.DailyReports
                .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);

            if (report == null)
                throw new ArgumentException("Dnevni nalog ne postoji.");

            // 2) Ucitaj stavke (TAB3) za taj nalog
            var states = await _context.DailyBeerStates
                .Where(x => x.IdNaloga == idNaloga)
                .ToListAsync();

            if (states.Count == 0)
            {
                // Nema unetih stavki -> nema sta da se racuna, preskoci bez pucanja
                return new ProsutoResultDto
                {
                    IdNaloga = idNaloga,
                    TotalProsuto = 0
                    // Items ostaje prazno (podrazumevano)
                };
            }

            var result = new ProsutoResultDto { IdNaloga = idNaloga };

            float prosutoSum = 0;
            float totalvagaPotrosnja = 0;
            float totalposPotrosnja = 0;

            var beerIds = states.Select(x => x.IdPiva).Distinct().ToList();

            var CountType = await _context.Beers
                .Where(b => beerIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.TipMerenja);

            // =========================
            // NEW 0) Dopuna snapshot-i za DANAS (ako postoje) -> najvisi prioritet za start
            // =========================
            var snapRows = await _context.DailyRestockSnapshots
                .AsNoTracking()
                .Where(x => x.IdNaloga == idNaloga && beerIds.Contains(x.IdPiva))
                .Select(x => new
                {
                    x.IdPiva,
                    x.IzmerenoSnapshot,
                    x.PosSnapshot,
                    x.UpdatedAt,
                    x.CreatedAt
                })
                .ToListAsync();

            var snapByBeerId = snapRows
                .GroupBy(x => x.IdPiva)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var last = g.OrderByDescending(z => z.UpdatedAt)
                                    .ThenByDescending(z => z.CreatedAt)
                                    .First();
                        return (Vaga: last.IzmerenoSnapshot, Pos: last.PosSnapshot);
                    });


            var cleaningRows = await _context.DailyCleaningSnapshots
                        .AsNoTracking()
                        .Where(x => x.IdNaloga == idNaloga && beerIds.Contains(x.IdPiva))
                        .Select(x => new
                        {
                            x.IdPiva,
                            x.BrojacStartAfterCleaning,
                            x.Id
                        })
                        .ToListAsync();

            var cleaningByBeerId = cleaningRows
                .GroupBy(x => x.IdPiva)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(z => z.Id).First().BrojacStartAfterCleaning
                );
            // =========================
            // 1) Poslednji popis PRE ovog dana (baseline)
            // =========================
            var lastReset = await _context.InventoryResets
                .AsNoTracking()
                .Where(r => DateOnly.FromDateTime(r.DatumPopisa.Date) < report.Datum)
                .OrderByDescending(r => r.DatumPopisa)
                .ThenByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            DateOnly? resetDate = lastReset != null
                ? DateOnly.FromDateTime(lastReset.DatumPopisa.Date)
                : null;

            Dictionary<int, (float izmereno, float pos)> resetMap = new();

            if (lastReset != null)
            {
                // Ako se desi vise itema po pivu, uzmi poslednji po Id
                resetMap = await _context.InventoryResetItems
                    .AsNoTracking()
                    .Where(i => i.InventoryResetId == lastReset.Id && beerIds.Contains(i.IdPiva))
                    .GroupBy(i => i.IdPiva)
                    .Select(g => new
                    {
                        IdPiva = g.Key,
                        Vaga = g.OrderByDescending(x => x.Id).Select(x => x.IzmerenoSnapshot).FirstOrDefault(),
                        Pos = g.OrderByDescending(x => x.Id).Select(x => x.PosSnapshot).FirstOrDefault()
                    })
                    .ToDictionaryAsync(x => x.IdPiva, x => (x.Vaga, x.Pos));
            }

            float sumNeg = 0;
            float sumPos = 0;

            foreach (var s in states)
            {
                float startVaga;
                float startPos;

                // =========================
                // NEW PRIORITET:
                // Ako postoji dopuna snapshot za DANAS -> start je iz snapshot-a
                // =========================
                if (snapByBeerId.TryGetValue(s.IdPiva, out var snapToday))
                {
                    startVaga = snapToday.Vaga;
                    startPos = snapToday.Pos;
                }
                else
                {
                    // Postojeca logika: prev iz TAB3 (posle popisa), pa resetMap, pa fallback
                    var prevQuery = _context.DailyBeerStates
                        .Join(_context.DailyReports,
                            st => st.IdNaloga,
                            dr => dr.IdNaloga,
                            (st, dr) => new { st, dr })
                        .Where(x => x.st.IdPiva == s.IdPiva && x.dr.Datum < report.Datum);

                    if (resetDate != null)
                    {
                        // sve pre (i ukljucujuci) datum popisa ignorisi
                        prevQuery = prevQuery.Where(x => x.dr.Datum > resetDate.Value);
                    }

                    var prev = await prevQuery
                        .OrderByDescending(x => x.dr.Datum)
                        .Select(x => x.st)
                        .FirstOrDefaultAsync();

                    Console.WriteLine("===== CALC DEBUG START =====");
                    Console.WriteLine($"[OB_DEBUG] Pivo {s.IdPiva}: prev={(prev != null ? "YES" : "NO")}," +
                        $" reset={(resetMap.ContainsKey(s.IdPiva) ? "YES" : "NO")} snap={(snapByBeerId.ContainsKey(s.IdPiva) ? "YES" : "NO")}");
                    Console.WriteLine("===== CALC DEBUG END =====");

                    if (prev != null)
                    {
                        startVaga = prev.Izmereno;
                        startPos = prev.StanjeUProgramu;
                    }
                    else if (resetMap.TryGetValue(s.IdPiva, out var snap))
                    {
                        startVaga = snap.izmereno;
                        startPos = snap.pos;
                    }
                    else
                    {
                        startVaga = s.Izmereno;
                        startPos = s.StanjeUProgramu;
                    }
                }
                if(cleaningByBeerId.TryGetValue(s.IdPiva, out var cleaningStart))
                {
                    startVaga = cleaningStart;
                }

                var endVaga = s.Izmereno;
                var endPos = s.StanjeUProgramu;

                var tipMerenja = CountType[s.IdPiva];

                float vagaPotrosnja;
                float posPotrosnja;

                if (tipMerenja.Equals("Bure", StringComparison.OrdinalIgnoreCase) ||
                    tipMerenja.Equals("Kafa", StringComparison.OrdinalIgnoreCase))
                {
                    vagaPotrosnja = startVaga - endVaga;
                    posPotrosnja = startPos - endPos;
                }
                else if (tipMerenja.Equals("Kesa", StringComparison.OrdinalIgnoreCase))
                {
                    vagaPotrosnja = endVaga - startVaga;
                    posPotrosnja = startPos - endPos;
                }
                else
                {
                    throw new ArgumentException($"Nepoznat tip merenja: '{tipMerenja}' za pivo ID {s.IdPiva}");
                }

                var odstupanje = posPotrosnja - vagaPotrosnja;

                var includeInProsuto = !string.Equals(tipMerenja, "Kafa", StringComparison.OrdinalIgnoreCase);

                if (includeInProsuto)
                {
                    totalvagaPotrosnja += vagaPotrosnja;
                    totalposPotrosnja += posPotrosnja;

                    if (odstupanje < 0) sumNeg += odstupanje;
                    else sumPos += odstupanje;

                    prosutoSum = sumNeg + sumPos;
                }

                result.Items.Add(new BeerCalcResultDto
                {
                    IdPiva = s.IdPiva,

                    VagaStart = startVaga,
                    VagaEnd = endVaga,
                    VagaPotrosnja = vagaPotrosnja,

                    PosStart = startPos,
                    PosEnd = endPos,
                    PosPotrosnja = posPotrosnja,

                    Odstupanje = odstupanje
                });
            }

            report.TotalProsuto = MathF.Round(prosutoSum, 2);
            report.TotalPotrosenoVaga = MathF.Round(totalvagaPotrosnja, 2);
            report.TotalPotrosenoProgram = totalposPotrosnja;

            await _context.SaveChangesAsync();

            result.TotalProsuto = MathF.Round(prosutoSum, 2);
            return result;
        }



        public async Task UpdateProsutoKantaAsync(int idNaloga, float prosutoKanta)
        {
            var report = await _context.DailyReports
                        .AsTracking()
                        .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);
            if (report == null)
            {
                throw new ArgumentException("Dnevni nalog ne postoji.");
            }
            if (prosutoKanta < 0)
            {
                throw new ArgumentException("Prosuto iz kante ne može biti negativno.");
            }
                

            report.IzmerenoProsuto = MathF.Round(prosutoKanta, 2);

            await _context.SaveChangesAsync();
        }

        public async Task<ProsutoResultDto> UpdateProsutoKantaAndRecalculateAsync(int idNaloga, float prosutoKanta)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // koristi tvoju postojecu metodu (validacije + upis)
                await UpdateProsutoKantaAsync(idNaloga, prosutoKanta);

                // ponovni proracun (koristi postojecu logiku)
                var result = await CalculateAndSaveAsync(idNaloga);

                await tx.CommitAsync();
                return result;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<ProsutoResultDto> GetAllStatesByIdNaloga(int idNaloga)
        {
            var report = await _context.DailyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdNaloga == idNaloga);

            if (report == null)
                throw new ArgumentException("Dnevni nalog ne postoji.");

            var reportDate = report.Datum; // DateOnly

            // Pocetak dana (UTC) => popis mora biti PRE ovog trenutka da bi vazilo kao baseline
            var reportStartUtc = new DateTime(
                reportDate.Year, reportDate.Month, reportDate.Day,
                0, 0, 0, DateTimeKind.Utc
            );

            var currentStates = await _context.DailyBeerStates
                .Where(s => s.IdNaloga == idNaloga)
                .ToListAsync();

            var beerIds = currentStates.Select(x => x.IdPiva).Distinct().ToList();
            var tipByBeerId = await _context.Beers
                .Where(b => beerIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.TipMerenja);

            var restockArticles = await _context.Restocks
                .Where(r => r.IdNaloga == idNaloga)
                .GroupBy(r => r.IdPiva)
                .Select(g => new { IdPiva = g.Key, Total = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.IdPiva, x => x.Total);

            // KLJUCNA PROMENA:
            // Popis vazi kao baseline samo ako je bio PRE tog dana (ne na isti dan).
            var yesterday = reportDate.AddDays(-1);

            var yStartUtc = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0, DateTimeKind.Utc);
            var yEndUtc = yStartUtc.AddDays(1);

            var cleaningRows = await _context.DailyCleaningSnapshots
                        .AsNoTracking()
                        .Where(x => x.IdNaloga == idNaloga && beerIds.Contains(x.IdPiva))
                        .Select(x => new
                        {
                            x.IdPiva,
                            x.BrojacStartAfterCleaning,
                            x.Id
                        })
                        .ToListAsync();

            var cleaningByBeerId = cleaningRows
                .GroupBy(x => x.IdPiva)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(z => z.Id).First().BrojacStartAfterCleaning
                );

            var lastRestart = await _context.InventoryResets
                .AsNoTracking()
                .Where(x => x.DatumPopisa >= yStartUtc && x.DatumPopisa < yEndUtc)
                .OrderByDescending(x => x.DatumPopisa)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            Dictionary<int, (float VagaStart, float PosStart)> resetMap = new();

            if (lastRestart != null)
            {
                resetMap = await _context.InventoryResetItems
                    .AsNoTracking()
                    .Where(x => x.InventoryResetId == lastRestart.Id)
                    .GroupBy(x => x.IdPiva)
                    .Select(g => new
                    {
                        IdPiva = g.Key,
                        Vaga = g.OrderByDescending(i => i.Id).Select(i => i.IzmerenoSnapshot).FirstOrDefault(),
                        Pos = g.OrderByDescending(i => i.Id).Select(i => i.PosSnapshot).FirstOrDefault()
                    })
                    .ToDictionaryAsync(
                        x => x.IdPiva,
                        x => (Convert.ToSingle(x.Vaga), Convert.ToSingle(x.Pos))
                    );
            }

            var items = new List<BeerCalcResultDto>();

            var todaySnapshots = await _context.DailyRestockSnapshots
                                        .AsNoTracking()
                                        .Where(x => x.IdNaloga == idNaloga)
                                        .GroupBy(x => x.IdPiva)
                                        .Select(g => g.OrderByDescending(x => x.UpdatedAt)
                                                      .ThenByDescending(x => x.CreatedAt)
                                                      .First())
                                        .ToListAsync();

            var snapByBeerId = todaySnapshots.ToDictionary(
                                    x => x.IdPiva,
                                    x => (x.IzmerenoSnapshot, x.PosSnapshot)
                                );

            foreach (var current in currentStates)
            {
               

                float vagaStart;
                float posStart;

                // 1) Ako postoji snapshot za DANAS -> to je start (i dopuna je vec unutra)
                if (snapByBeerId.TryGetValue(current.IdPiva, out var snapToday))
                {
                    vagaStart = snapToday.IzmerenoSnapshot;
                    posStart = snapToday.PosSnapshot;

                    // dopuna je vec u snapshot-u, ne dodajem je drugi put
                    
                }
                else if (resetMap.TryGetValue(current.IdPiva, out var snap))
                {
                    // 2) baseline iz popisa (pre dana)
                    vagaStart = snap.VagaStart;
                    posStart = snap.PosStart;
                }
                else
                {
                    // 3) baseline iz prethodnog TAB3
                    var prev = await _context.DailyBeerStates
                        .Join(_context.DailyReports,
                            st => st.IdNaloga,
                            dr => dr.IdNaloga,
                            (st, dr) => new { st, dr })
                        .Where(x => x.st.IdPiva == current.IdPiva && x.dr.Datum < reportDate)
                        .OrderByDescending(x => x.dr.Datum)
                        .Select(x => x.st)
                        .FirstOrDefaultAsync();

                    vagaStart = prev?.Izmereno ?? current.Izmereno;
                    posStart = prev?.StanjeUProgramu ?? current.StanjeUProgramu;
                }
                if (cleaningByBeerId.TryGetValue(current.IdPiva, out var cleaningStart))
                {
                    vagaStart = cleaningStart;
                }


                float vagaEnd = current.Izmereno;
                float posEnd = current.StanjeUProgramu;

                if (!tipByBeerId.TryGetValue(current.IdPiva, out var tipRaw))
                    throw new ArgumentException($"Pivo sa ID {current.IdPiva} ne postoji u TAB1.");

                var tip = (tipRaw ?? "").Trim().ToLower();

                float vagaPotrosnja;
                float posPotrosnja;

                if (tip == "bure" || tip == "kafa")
                {
                    vagaPotrosnja = vagaStart - vagaEnd;
                    posPotrosnja = posStart - posEnd;
                }
                else if (tip == "kesa")
                {
                    vagaPotrosnja = vagaEnd - vagaStart;
                    posPotrosnja = posStart - posEnd;
                }
                else
                {
                    throw new ArgumentException($"Nepoznat tip merenja: '{tipRaw}' za pivo ID {current.IdPiva}");
                }

                var odstupanje = posPotrosnja - vagaPotrosnja;

                items.Add(new BeerCalcResultDto
                {
                    IdPiva = current.IdPiva,
                    NazivPiva = current.NazivPiva,

                    VagaStart = vagaStart,
                    VagaEnd = vagaEnd,
                    VagaPotrosnja = vagaPotrosnja,

                    PosStart = posStart,
                    PosEnd = posEnd,
                    PosPotrosnja = posPotrosnja,

                    Odstupanje = odstupanje,
                    TipMerenja = tip
                });
            }

            return new ProsutoResultDto
            {
                IdNaloga = idNaloga,
                TotalProsuto = report.TotalProsuto ,
                ProsutoKanta = report.IzmerenoProsuto ,
                Items = items
            };
        }




        public async Task<float> CalculateAndSaveProsutoRazlikaAsync(int idNaloga)
        {
            var report = await _context.DailyReports.FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);

            if(report == null)
            {
                throw new ArgumentException("Dnevni nalog ne postoji.");
            }
            if(report.IzmerenoProsuto < 0)
            {
                throw new ArgumentException("Izmereno prosuto (kanta) nije uneto.");
            }
            if (report.TotalProsuto == null)
            {
                throw new ArgumentException("Pokrenite prvo calculate_prosuto!");
            }

            var totalProsuto = MathF.Round(report.TotalProsuto, 2);
            var izmerenoProsuto = MathF.Round(report.IzmerenoProsuto, 2);

            var razlika = MathF.Round(izmerenoProsuto -  totalProsuto, 2);

            report.ProsutoRazlika = razlika;
            await _context.SaveChangesAsync();

            return razlika;
        }

        public async Task<List<DailyBeerState>> CalculateAndUpdateProsutoForReportAsync(int idNaloga)
        {
            var currentStates = await _context.DailyBeerStates
                .Where(s => s.IdNaloga == idNaloga)
                .ToListAsync();

            if(currentStates.Count == 0)
            {
             throw new ArgumentException("Nema stanja za dati IdNaloga (TAB3 je prazna za taj nalog).");

            
            }
            var beerIds = currentStates.Select(x => x.IdPiva).Distinct().ToList();

            var tipByBeerId = await _context.Beers
                .Where(b => beerIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.TipMerenja);

            var prevReport = await _context.DailyReports
                .Where(r => r.IdNaloga < idNaloga)
                .OrderByDescending(r => r.IdNaloga)
                .FirstOrDefaultAsync();

            Dictionary<int, DailyBeerState> prevByBeerId = new();

            if(prevReport != null)
            {
                var prevStates = await _context.DailyBeerStates
                    .Where(s => s.IdNaloga == prevReport.IdNaloga)
                    .ToListAsync();

                prevByBeerId = prevStates.GroupBy(x => x.IdPiva)
                    .ToDictionary(g => g.Key, g => g.First());
                    
            }
            
            foreach (var cur in currentStates)
            {
                float endVaga = cur.Izmereno;
                float endPos = cur.StanjeUProgramu;

                float startVaga = prevByBeerId.TryGetValue(cur.IdPiva, out var prev) ? prev.Izmereno : endVaga;
                float startPos = prevByBeerId.TryGetValue(cur.IdPiva, out var prev2) ? prev2.StanjeUProgramu : endPos;

                if (!tipByBeerId.TryGetValue(cur.IdPiva, out var tipRaw))
                    throw new ArgumentException($"Pivo sa ID {cur.IdPiva} ne postoji u TAB1.");

                var tip = tipRaw?.Trim().ToLowerInvariant();

                float vagaPotrosnja;
                float posPotrosnja;
                // NAPOMENA:
                // - kod "bure": Izmereno predstavlja kolicinu koja OPADA (vaga)
                // - kod "kesa": Izmereno predstavlja BROJAC (kumulativno raste)
                //   zato se SAMO za kesa obrce proracun vagaPotrosnje,
                //   dok POS logika (startPos - endPos) ostaje ista
                // koristim vrednost za svako pivo a to je tipMerenja i na osnovu toga znam 
                // koja logika gde ide za proracun!
                if (tip == "bure")
                {
                    vagaPotrosnja = startVaga - endVaga;
                    posPotrosnja = startPos - endPos;
                }
                else if (tip == "kafa")
                {
                    vagaPotrosnja = startVaga - endVaga;
                    posPotrosnja = startPos - endPos;
                }
                else if (tip == "kesa")
                {
                    vagaPotrosnja = endVaga - startVaga;
                    posPotrosnja = startPos - endPos;
                }
                else
                {
                    throw new ArgumentException($"Nepoznat tip merenja: '{tipRaw}' za pivo ID {cur.IdPiva}");
                }

                var odstupanje = posPotrosnja - vagaPotrosnja;

                // Prosuto = samo kad je negativno odstupanje (izlaz > kucano)
                cur.ProsutoJednogPiva = odstupanje < 0 ? Math.Abs(odstupanje) : 0;
            }
            await _context.SaveChangesAsync();
            return currentStates;
        }

        //SLICNU METODU IMAM, ALI MI JE LAKSE OVO OVDE POSTAVITI UMESTO ISPRAVLJATI.. MORAM ONDA NA STO MESTA
        //A TO STVARNO SAD NE MOGU DA RADIM.. ZNAM DA OVO NIJE DOBRA PRAKSA..
        //AKO MENJAM CalculateAndSaveAsync(int idNaloga) MENJAM I OVDE! 

        public async Task<(ProsutoResultDto result, float totalVaga, float totalPos)> CalcProsutoForPotrosnjaVagaAndPos(int idNaloga)
        {
            var report = await _context.DailyReports
                .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);

            if (report == null)
                throw new ArgumentException("Dnevni nalog ne postoji.");

            var states = await _context.DailyBeerStates
                .Where(x => x.IdNaloga == idNaloga)
                .ToListAsync();

            if (states.Count == 0)
                throw new ArgumentException("Nema unetih stavki za ovaj nalog.");

            var result = new ProsutoResultDto { IdNaloga = idNaloga };

            float prosutoSum = 0;
            float totalvagaPotrosnja = 0;
            float totalposPotrosnja = 0;

            var beerIds = states.Select(x => x.IdPiva).Distinct().ToList();

            var countType = await _context.Beers
                .Where(b => beerIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.TipMerenja);

            // =========================
            // NEW 1) Dopuna snapshot-i za DANAS (ako postoje)
            // =========================
            var snapRows = await _context.DailyRestockSnapshots
                .AsNoTracking()
                .Where(x => x.IdNaloga == idNaloga && beerIds.Contains(x.IdPiva))
                .Select(x => new { x.IdPiva, x.IzmerenoSnapshot, x.PosSnapshot, x.UpdatedAt, x.CreatedAt })
                .ToListAsync();

            var snapByBeerId = snapRows
                .GroupBy(x => x.IdPiva)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var last = g.OrderByDescending(z => z.UpdatedAt).ThenByDescending(z => z.CreatedAt).First();
                        return (Vaga: last.IzmerenoSnapshot, Pos: last.PosSnapshot);
                    });

            // =========================
            // NEW 2) Poslednji popis PRE ovog dana kao baseline
            // =========================
            var reportDate = report.Datum; // DateOnly

            var yesterday = reportDate.AddDays(-1);

            var yStartUtc = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0, DateTimeKind.Utc);
            var yEndUtc = yStartUtc.AddDays(1);

            var cleaningRows = await _context.DailyCleaningSnapshots
                    .AsNoTracking()
                    .Where(x => x.IdNaloga == idNaloga && beerIds.Contains(x.IdPiva))
                    .Select(x => new
                    {
                        x.IdPiva,
                        x.BrojacStartAfterCleaning,
                        x.Id
                 })
                 .ToListAsync();

            var cleaningByBeerId = cleaningRows
                .GroupBy(x => x.IdPiva)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(z => z.Id).First().BrojacStartAfterCleaning
                );

            var lastRestart = await _context.InventoryResets
                .AsNoTracking()
                .Where(x => x.DatumPopisa >= yStartUtc && x.DatumPopisa < yEndUtc)
                .OrderByDescending(x => x.DatumPopisa)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            Dictionary<int, (float VagaStart, float PosStart)> resetMap = new();

            if (lastRestart != null)
            {
                resetMap = await _context.InventoryResetItems
                    .AsNoTracking()
                    .Where(x => x.InventoryResetId == lastRestart.Id && beerIds.Contains(x.IdPiva))
                    .GroupBy(x => x.IdPiva)
                    .Select(g => new
                    {
                        IdPiva = g.Key,
                        Vaga = g.OrderByDescending(i => i.Id).Select(i => i.IzmerenoSnapshot).FirstOrDefault(),
                        Pos = g.OrderByDescending(i => i.Id).Select(i => i.PosSnapshot).FirstOrDefault()
                    })
                    .ToDictionaryAsync(
                        x => x.IdPiva,
                        x => (Convert.ToSingle(x.Vaga), Convert.ToSingle(x.Pos))
                    );
            }

            float sumNeg = 0;
            float sumPos = 0;

            foreach (var s in states)
            {
               
                float startVaga;
                float startPos;

                if (snapByBeerId.TryGetValue(s.IdPiva, out var snap))
                {
                    startVaga = snap.Vaga;
                    startPos = snap.Pos;
                }
                else if (resetMap.TryGetValue(s.IdPiva, out var reset))
                {
                    startVaga = reset.VagaStart;
                    startPos = reset.PosStart;
                }
                else
                {
                    var prev = await _context.DailyBeerStates
                        .Join(_context.DailyReports,
                            st => st.IdNaloga,
                            dr => dr.IdNaloga,
                            (st, dr) => new { st, dr })
                        .Where(x => x.st.IdPiva == s.IdPiva && x.dr.Datum < report.Datum)
                        .OrderByDescending(x => x.dr.Datum)
                        .Select(x => x.st)
                        .FirstOrDefaultAsync();

                    startVaga = prev?.Izmereno ?? s.Izmereno;
                    startPos = prev?.StanjeUProgramu ?? s.StanjeUProgramu;
                }

                if (cleaningByBeerId.TryGetValue(s.IdPiva, out var cleaningStart))
                {
                    startVaga = cleaningStart;
                }

                var endVaga = s.Izmereno;
                var endPos = s.StanjeUProgramu;

                if (!countType.TryGetValue(s.IdPiva, out var tipMerenja))
                    throw new ArgumentException($"Nepoznat tip merenja za pivo ID {s.IdPiva}");

                float vagaPotrosnja;
                float posPotrosnja;

                if (string.Equals(tipMerenja, "Bure", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tipMerenja, "Kafa", StringComparison.OrdinalIgnoreCase))
                {
                    vagaPotrosnja = startVaga - endVaga;
                    posPotrosnja = startPos - endPos;
                }
                else if (string.Equals(tipMerenja, "Kesa", StringComparison.OrdinalIgnoreCase))
                {
                    vagaPotrosnja = endVaga - startVaga;
                    posPotrosnja = startPos - endPos;
                }
                else
                {
                    throw new ArgumentException($"Nepoznat tip merenja: '{tipMerenja}' za pivo ID {s.IdPiva}");
                }

                var odstupanje = posPotrosnja - vagaPotrosnja;

                var includeInProsuto = !string.Equals(tipMerenja, "Kafa", StringComparison.OrdinalIgnoreCase);
                if (includeInProsuto)
                {
                    totalvagaPotrosnja += vagaPotrosnja;
                    totalposPotrosnja += posPotrosnja;

                    if (odstupanje < 0) sumNeg += odstupanje;
                    else sumPos += odstupanje;

                    prosutoSum = sumNeg + sumPos;
                }

                result.Items.Add(new BeerCalcResultDto
                {
                    IdPiva = s.IdPiva,
                    VagaStart = startVaga,
                    VagaEnd = endVaga,
                    VagaPotrosnja = vagaPotrosnja,
                    PosStart = startPos,
                    PosEnd = endPos,
                    PosPotrosnja = posPotrosnja,
                    Odstupanje = odstupanje
                });
            }

            result.TotalProsuto = MathF.Round(prosutoSum, 2);

            return (result, totalvagaPotrosnja, totalposPotrosnja);
        }







    }
}
