using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Beers;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;

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

                    Odstupanje = odstupanje
                });
            }

            // 4) Upis u TAB2
            report.Prosuto = MathF.Round(prosutoSum, 2);
            await _context.SaveChangesAsync();

            result.Prosuto = MathF.Round(prosutoSum, 2);
            return result;
        }

    }
}
