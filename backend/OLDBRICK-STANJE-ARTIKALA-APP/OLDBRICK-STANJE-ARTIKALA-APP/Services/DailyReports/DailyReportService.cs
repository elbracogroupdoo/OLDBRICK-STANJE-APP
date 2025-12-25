using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.DTOs.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports
{
    public class DailyReportService : IDailyReportService
    {
        private readonly AppDbContext _context;

        public DailyReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DailyReportResponseDto> CreateByDateAsync(DateOnly datum)
        {
            var existing = await _context.DailyReports
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Datum == datum);

            if(existing != null)
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

            if(report == null)
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
    }
}
