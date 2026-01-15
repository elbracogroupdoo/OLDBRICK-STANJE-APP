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
                throw new ArgumentException("Nema unetih stavki za ovaj nalog.");

            var result = new ProsutoResultDto { IdNaloga = idNaloga };

            float prosutoSum = 0;
            float totalvagaPotrosnja = 0;
            float totalposPotrosnja = 0;

            var beerIds = states.Select(x => x.IdPiva).Distinct().ToList();

            var CountType = await _context.Beers.Where(b => beerIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.TipMerenja);


            var lastReset = await _context.InventoryResets
                            .Where(r => DateOnly.FromDateTime(r.DatumPopisa.Date) < report.Datum)
                            .OrderByDescending(r => r.DatumPopisa)
                            .FirstOrDefaultAsync();

            DateOnly? resetDate = lastReset != null
                                    ? DateOnly.FromDateTime(lastReset.DatumPopisa.Date)
                                    : null;

            Dictionary<int, (float izmereno, float pos)> resetMap = new();

            if (lastReset != null)
            {
                resetMap = await _context.InventoryResetItems
                    .Where(i => i.InventoryResetId == lastReset.Id)
                    .ToDictionaryAsync(i => i.IdPiva, i => (i.IzmerenoSnapshot, i.PosSnapshot));
            }

            float sumNeg = 0;
            float sumPos = 0;

            foreach (var s in states)
            {
                var prevQuery = _context.DailyBeerStates
                            .Join(_context.DailyReports,
                                st => st.IdNaloga,
                                dr => dr.IdNaloga,
                                (st, dr) => new { st, dr })
                            .Where(x => x.st.IdPiva == s.IdPiva && x.dr.Datum < report.Datum);

                if (resetDate != null)
                {
                    prevQuery = prevQuery.Where(x => x.dr.Datum > resetDate.Value);
                }

                var prev = await prevQuery
                    .OrderByDescending(x => x.dr.Datum)
                    .Select(x => x.st)
                    .FirstOrDefaultAsync();

                Console.WriteLine("===== CALC DEBUG START =====");
                Console.WriteLine($"[OB_DEBUG] Pivo {s.IdPiva}: prev={(prev != null ? "YES" : "NO")}," +
                    $" reset={(resetMap.ContainsKey(s.IdPiva) ? "YES" : "NO")}");

                Console.WriteLine("===== CALC DEBUG END =====");

                float startVaga;
                float startPos;

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

                    //  NETO SUMA: prvo saberi negativne i pozitivne posebno
                    if (odstupanje < 0) sumNeg += odstupanje;   // ostaje negativno
                    else sumPos += odstupanje;                  // pozitivno


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
            var lastRestart = await _context.InventoryResets
                .AsNoTracking()
                .Where(x => x.DatumPopisa < reportStartUtc)
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

            foreach (var current in currentStates)
            {
                restockArticles.TryGetValue(current.IdPiva, out var addedDec);
                var added = Convert.ToSingle(addedDec);

                float vagaStart;
                float posStart;

                if (resetMap.TryGetValue(current.IdPiva, out var snap))
                {
                    vagaStart = snap.VagaStart;
                    posStart = snap.PosStart;
                }
                else
                {
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

                float vagaEnd = current.Izmereno;
                float posEnd = current.StanjeUProgramu;

                if (!tipByBeerId.TryGetValue(current.IdPiva, out var tipRaw))
                    throw new ArgumentException($"Pivo sa ID {current.IdPiva} ne postoji u TAB1.");

                var tip = (tipRaw ?? "").Trim().ToLower();

                float vagaPotrosnja;
                float posPotrosnja;

                if (tip == "bure" || tip == "kafa")
                {
                    vagaPotrosnja = (vagaStart + added) - vagaEnd;
                    posPotrosnja = (posStart + added) - posEnd;
                }
                else if (tip == "kesa")
                {
                    vagaPotrosnja = vagaEnd - vagaStart;
                    posPotrosnja = (posStart + added) - posEnd;
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
            if(report.IzmerenoProsuto <= 0)
            {
                throw new ArgumentException("Izmereno prosuto (kanta) nije uneto.");
            }
            if(report.TotalProsuto < 0)
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

            float sumNeg = 0;
            float sumPos = 0;

            foreach (var s in states)
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

                var startVaga = prev?.Izmereno ?? s.Izmereno;
                var startPos = prev?.StanjeUProgramu ?? s.StanjeUProgramu;

                var endVaga = s.Izmereno;
                var endPos = s.StanjeUProgramu;

                var tipMerenja = countType[s.IdPiva];

                float vagaPotrosnja;
                float posPotrosnja;

                if (tipMerenja == "Bure" || tipMerenja == "Kafa")
                {
                    vagaPotrosnja = startVaga - endVaga;
                    posPotrosnja = startPos - endPos;
                }
                else if (tipMerenja == "Kesa")
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
