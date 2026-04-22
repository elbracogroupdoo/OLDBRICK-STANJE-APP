using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports
{
    public class RestockService : IRestockService
    {
        private readonly AppDbContext _context;

        public RestockService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RestockForReportDto>> GetRestocksForNalogAsync(int idNaloga)
        {
            var result = await _context.Restocks
          .Where(r => r.IdNaloga == idNaloga)
          .Join(
              _context.Beers,
              restock => restock.IdPiva,
              article => article.Id,
              (restock, article) => new RestockForReportDto
              {
                  Id = restock.Id,
                  CreatedAt = restock.CreatedAt,
                  IdNaloga = restock.IdNaloga,
                  IdPiva = restock.IdPiva,
                  NazivPiva = article.NazivPiva,
                  Quantity = restock.Quantity
              }
          )
          .OrderByDescending(x => x.CreatedAt)
          .ToListAsync();

            return result;

        }

        public async Task<bool> DeleteRestockByIdAsync(int id)
        {
            var restock = await _context.Restocks
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restock == null)
            {
                return false;
            }

            var beer = await _context.Beers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == restock.IdPiva);

            if (beer == null)
            {
                throw new KeyNotFoundException("Artikal nije pronađen.");
            }

            var snapshot = await _context.DailyRestockSnapshots
                .FirstOrDefaultAsync(x =>
                    x.IdNaloga == restock.IdNaloga &&
                    x.IdPiva == restock.IdPiva);

            if (snapshot == null)
            {
                throw new KeyNotFoundException("Daily restock snapshot nije pronađen.");
            }

            var tip = beer.TipMerenja?.Trim().ToLowerInvariant();
            var qty = restock.Quantity;

            using var tx = await _context.Database.BeginTransactionAsync();

            snapshot.AddedQuantity -= qty;

            if (tip == "bure" || tip == "kafa")
            {
                snapshot.IzmerenoSnapshot -= qty;
                snapshot.PosSnapshot -= qty;
            }
            else if (tip == "kesa")
            {
                snapshot.PosSnapshot -= qty;
            }
            else
            {
                throw new ArgumentException("Nepoznat tip merenja za ovaj artikal.");
            }

            snapshot.UpdatedAt = DateTime.UtcNow;

            _context.Restocks.Remove(restock);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return true;
        }
    }
}
