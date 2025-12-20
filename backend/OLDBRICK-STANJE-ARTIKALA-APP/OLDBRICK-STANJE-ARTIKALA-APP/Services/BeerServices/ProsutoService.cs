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

            foreach (var s in states)
            {
                // 3) Nadji poslednji prethodni unos za isto pivo (TAB3) iz bilo kog starijeg naloga
                // Posto TAB3 nema datum, moramo preko TAB2 datuma.
                var prev = await _context.DailyBeerStates
                    .Join(_context.DailyReports,
                          st => st.IdNaloga,
                          dr => dr.IdNaloga,
                          (st, dr) => new { st, dr })
                    .Where(x => x.st.IdPiva == s.IdPiva && x.dr.Datum < report.Datum)
                    .OrderByDescending(x => x.dr.Datum)
                    .Select(x => x.st)
                    .FirstOrDefaultAsync();

                // Ako nema prethodnog, start = end (potrosnje 0) -> ne utice na prosuto
                var vagaStart = prev?.Izmereno ?? s.Izmereno;
                var posStart = prev?.StanjeUProgramu ?? s.StanjeUProgramu;

                var vagaEnd = s.Izmereno;
                var posEnd = s.StanjeUProgramu;

                var vagaPotrosnja = vagaStart - vagaEnd;
                var posPotrosnja = posStart - posEnd;

                var odstupanje = posPotrosnja - vagaPotrosnja;

                totalvagaPotrosnja += vagaPotrosnja;
                totalposPotrosnja += posPotrosnja;

                // prosuto = sabiramo samo negativna odstupanja (gubitak)
                if (odstupanje < 0)
                    prosutoSum += Math.Abs(odstupanje);
                

                result.Items.Add(new BeerCalcResultDto
                {
                    IdPiva = s.IdPiva,

                    VagaStart = vagaStart,
                    VagaEnd = vagaEnd,
                    VagaPotrosnja = vagaPotrosnja,

                    PosStart = posStart,
                    PosEnd = posEnd,
                    PosPotrosnja = posPotrosnja,

                    Odstupanje = odstupanje,
                });
            }

            // 4) Upis u TAB2
            report.TotalProsuto = MathF.Round(prosutoSum, 2);
            report.TotalPotrosenoVaga = MathF.Round(totalvagaPotrosnja, 2);
            report.TotalPotrosenoProgram = totalposPotrosnja;
            await _context.SaveChangesAsync();

            result.TotalProsuto = MathF.Round(prosutoSum, 2);
            return result;
        }

        public async Task<ProsutoResultDto> GetAllStatesByIdNaloga(int idNaloga)
        {
            var currentStates = await _context.DailyBeerStates
        .Where(s => s.IdNaloga == idNaloga)
        .ToListAsync();

            var prevReport = await _context.DailyReports
                .Where(r => r.IdNaloga < idNaloga)
                .OrderByDescending(r => r.IdNaloga)
                .FirstOrDefaultAsync();

            var prevStates = prevReport == null
                ? new List<DailyBeerState>()
                : await _context.DailyBeerStates
                    .Where(s => s.IdNaloga == prevReport.IdNaloga)
                    .ToListAsync();

            var items = new List<BeerCalcResultDto>();

            foreach (var current in currentStates)
            {
                // Prethodno stanje za pivo sa istim ID-em.
                var prev = prevStates.FirstOrDefault(p => p.IdPiva == current.IdPiva);

                float vagaStart;
                float posStart;

                if (prev != null)
                {
                    vagaStart = prev.Izmereno;
                    posStart = prev.StanjeUProgramu;
                }
                else
                {
                    // ako nema prethodnog, start = end
                    vagaStart = current.Izmereno;
                    posStart = current.StanjeUProgramu;
                }

                float vagaEnd = current.Izmereno;
                float posEnd = current.StanjeUProgramu;

                var vagaPotrosnja = vagaStart - vagaEnd;
                var posPotrosnja = posStart - posEnd;

                items.Add(new BeerCalcResultDto
                {
                    IdPiva = current.IdPiva,

                    VagaStart = vagaStart,
                    VagaEnd = vagaEnd,
                    VagaPotrosnja = vagaPotrosnja,

                    PosStart = posStart,
                    PosEnd = posEnd,
                    PosPotrosnja = posPotrosnja,

                    Odstupanje = vagaPotrosnja - posPotrosnja
                });
            }

            //TAB2 TRAZIMO PROSUTO ZA TAJ DAN(nalog)

            var report = await _context.DailyReports.AsNoTracking().FirstOrDefaultAsync(
                r => r.IdNaloga == idNaloga);

            var TotalProsuto = report?.TotalProsuto ?? 0;

            

            return new ProsutoResultDto
            {
                IdNaloga = idNaloga,
                TotalProsuto = TotalProsuto,
                Items = items
            };
        }

    }
}
