using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
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
                .FirstOrDefaultAsync(x => x.IdNaloga == idNaloga);
            if (report == null)
            {
                throw new ArgumentException("Dnevni nalog ne postoji");
            }

            var dayBeforeReport = await _context.DailyReports
                .Where(r => r.Datum < report.Datum)
                .OrderByDescending(r => r.Datum)
                .FirstOrDefaultAsync();

            if (dayBeforeReport == null)
            {
                return new List<DayBeforeStateDto>();
            }

            var prevIdNaloga = dayBeforeReport.IdNaloga;

            var result = await _context.DailyBeerStates
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

            // 2) poslednji popis
            var lastReset = await _context.InventoryResets
                .OrderByDescending(x => x.DatumPopisa)
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

    }
}
