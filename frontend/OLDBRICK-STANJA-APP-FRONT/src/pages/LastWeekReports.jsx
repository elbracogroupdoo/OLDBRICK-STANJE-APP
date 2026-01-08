import { useMemo, useState } from "react";
import {
  getByRangeTotalProsuto,
  getProsutoByRangeForEachBeer,
} from "../api/helpers";

function formatDateIso(date) {
  return date.toISOString().slice(0, 10);
}

function LastWeekReports() {
  const todayIso = useMemo(() => formatDateIso(new Date()), []);
  const today = new Date();

  const firstDayOfMonth = `${today.getFullYear()}-${String(
    today.getMonth() + 1
  ).padStart(2, "0")}-01`;

  const [from, setFrom] = useState(firstDayOfMonth);
  const [to, setTo] = useState(todayIso);
  const [perBeer, setPerBeer] = useState([]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [result, setResult] = useState(null);

  async function handleFetch() {
    setError("");
    setResult(null);

    if (!from || !to) {
      setError("Popuni oba datuma.");
      return;
    }
    if (from > to) {
      setError('"From" ne sme biti posle "To".');
      return;
    }

    try {
      setLoading(true);

      const [summaryData, perBeerData] = await Promise.all([
        getByRangeTotalProsuto(from, to),
        getProsutoByRangeForEachBeer(from, to),
      ]);

      setResult(summaryData);
      setPerBeer(perBeerData);
    } catch (e) {
      setError("Greška pri učitavanju range izveštaja.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="pt-20 px-4">
      <h1 className="text-xl font-semibold text-white">PRETHODNI DANI</h1>
      <p className="text-white/70 mt-2">
        Izaberi opseg datuma, izvuci ukupno prosuto i prosuto za svaki artikal.
      </p>

      {/* RANGE UI */}
      <div className="mt-6 grid gap-4 max-w-xl">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <label className="grid gap-1">
            <span className="text-white/80 text-sm">Od:</span>
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="rounded-lg bg-white/10 text-white px-3 py-2 outline-none ring-1 ring-white/10 focus:ring-2 focus:ring-white/20"
            />
          </label>

          <label className="grid gap-1">
            <span className="text-white/80 text-sm">Do:</span>
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="rounded-lg bg-white/10 text-white px-3 py-2 outline-none ring-1 ring-white/10 focus:ring-2 focus:ring-white/20"
            />
          </label>
        </div>

        <button
          type="button"
          onClick={handleFetch}
          disabled={loading}
          className="rounded-lg px-4 py-2 bg-white/15 text-white hover:bg-white/20 transition disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {loading ? "Učitavam..." : "Prikaži"}
        </button>

        {/* ERROR */}
        {error && (
          <div className="rounded-lg bg-red-500/10 ring-1 ring-red-500/20 px-3 py-2 text-red-200 text-sm">
            {error}
          </div>
        )}

        {/* RESULT */}
        {result && (
          <div className="rounded-xl bg-white/5 ring-1 ring-white/10 p-4 text-white">
            <div className="text-white/70 text-sm">
              Opseg: <span className="text-white">{result.from}</span> →{" "}
              <span className="text-white">{result.to}</span>
            </div>

            <div className="mt-3 grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div className="rounded-lg bg-white/5 ring-1 ring-white/10 p-3">
                <div className="text-white/70 text-xs">Sa vage</div>
                <div className="text-lg font-semibold">
                  {result.totalMEasuredProsuto}L
                </div>
              </div>

              <div className="rounded-lg bg-white/5 ring-1 ring-white/10 p-3">
                <div className="text-white/70 text-xs">Iz app</div>
                <div className="text-lg font-semibold">
                  {result.totalAppProsuto}L
                </div>
              </div>

              <div className="rounded-lg bg-white/5 ring-1 ring-white/10 p-3">
                <div className="text-white/70 text-xs">Razlika</div>
                <div className="text-lg font-semibold">
                  {result.totalDifference}L
                </div>
              </div>
            </div>
          </div>
        )}
        {perBeer.length > 0 && (
          <div className="mt-6 max-w-xl">
            <h2 className="text-white font-semibold mb-3">Prosuto po pivu</h2>

            <div className="grid gap-2">
              {perBeer.map((item) => (
                <div
                  key={item.beerId}
                  className="flex justify-between items-center rounded-lg bg-white/5 ring-1 ring-white/10 px-4 py-2 text-white"
                >
                  <span className="text-white/80">{item.beerName}</span>

                  <span className="font-medium">{item.totalAppProsuto} L</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default LastWeekReports;
