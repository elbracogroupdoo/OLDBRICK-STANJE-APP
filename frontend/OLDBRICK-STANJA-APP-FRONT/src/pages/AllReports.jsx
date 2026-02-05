import { useEffect, useState, useRef } from "react";
import {
  getReportStatesById,
  getAllReportDates,
  getTotalsSinceLastInventoryReset,
  getBeerShortageTotalsForNalog,
} from "../api/helpers";

function AllReports() {
  const [idNaloga, setIdNaloga] = useState(189);
  const [loading, setLoading] = useState(false);

  const [timeline, setTimeline] = useState([]);
  const [currentIndex, setCurrentIndex] = useState(0);

  const [data, setData] = useState(null);
  const [displayData, setDisplayData] = useState(null);

  const [loadingTimeline, setLoadingTimeline] = useState(false);
  const [loadingData, setLoadingData] = useState(false);
  const [error, setError] = useState(null);

  const [totals, setTotals] = useState(null);
  const [displayTotals, setDisplayTotals] = useState(null);
  const [loadingTotals, setLoadingTotals] = useState(false);

  const [beerShortageMap, setBeerShortageMap] = useState({});

  const requestIdRef = useRef(0);

  useEffect(() => {
    async function loadTimeline() {
      try {
        setLoadingTimeline(true);
        setError(null);

        const dates = await getAllReportDates();

        const sorted = (dates ?? [])
          .filter((x) => x?.idNaloga && x?.datum)
          .sort((a, b) => new Date(a.datum) - new Date(b.datum));

        setTimeline(sorted);
        setCurrentIndex(0);
      } catch (err) {
        setError("Greška pri učitavanju liste naloga.");
        console.error(err);
      } finally {
        setLoadingTimeline(false);
      }
    }

    loadTimeline();
  }, []);

  const current = timeline[currentIndex];
  const currentIdNaloga = current?.idNaloga;

  useEffect(() => {
    if (!currentIdNaloga) return;

    const requestId = ++requestIdRef.current;

    async function loadAll() {
      try {
        setError(null);
        setLoadingData(true);
        setLoadingTotals(true);

        const [statesRes, totalsRes, shortageRes] = await Promise.allSettled([
          getReportStatesById(currentIdNaloga),
          getTotalsSinceLastInventoryReset(currentIdNaloga),
          getBeerShortageTotalsForNalog(currentIdNaloga),
        ]);

        if (requestId !== requestIdRef.current) return;

        if (statesRes.status === "fulfilled") {
          setData(statesRes.value);
          setDisplayData(statesRes.value);
        } else {
          const status = statesRes.reason?.response?.status;
          if (status === 404 || status === 204) {
            const empty = { items: [] };
            setData(empty);
            setDisplayData(empty);
          } else {
            setError("Greška pri učitavanju podataka.");
            console.error(statesRes.reason);

            setDisplayData({ items: [] });
          }
        }

        if (totalsRes.status === "fulfilled") {
          setTotals(totalsRes.value);
          setDisplayTotals(totalsRes.value);
        } else {
          console.error(totalsRes.reason);
          setTotals(null);
          setDisplayTotals(null);
        }

        if (shortageRes.status === "fulfilled") {
          const list = shortageRes.value ?? [];
          const map = list.reduce((acc, x) => {
            acc[x.idPiva] = x.totalManjak;
            return acc;
          }, {});
          setBeerShortageMap(map);
        } else {
          console.error(shortageRes.reason);
          setBeerShortageMap({});
        }
      } finally {
        if (requestId !== requestIdRef.current) return;
        setLoadingData(false);
        setLoadingTotals(false);
      }
    }

    loadAll();
  }, [currentIdNaloga]);

  const handlePrev = () => setCurrentIndex((i) => Math.max(0, i - 1));
  const handleNext = () =>
    setCurrentIndex((i) => Math.min(timeline.length - 1, i + 1));

  const articleOrder = [
    "Stara cigla svetla",
    "Stara cigla IPA",
    "Nektar",
    "Haineken",
    "Paulaner svetli",
    "Paulaner psenica",
    "Kozel tamno",
    "Blank",
    "Tuborg",
    "Lav",
    "Kafa",
  ];

  const orderMap = articleOrder.reduce((acc, name, index) => {
    acc[name] = index;
    return acc;
  }, {});

  const items = displayData?.items ?? [];

  const sortedItems = [...items].sort((a, b) => {
    const aIndex = orderMap[a.nazivPiva] ?? Number.MAX_SAFE_INTEGER;
    const bIndex = orderMap[b.nazivPiva] ?? Number.MAX_SAFE_INTEGER;
    return aIndex - bIndex;
  });

  const showOverlay = loadingData || loadingTotals;

  function fmt(value) {
    if (value === null || value === undefined || isNaN(value)) return "-";
    return Number(value).toFixed(2);
  }

  return (
    <div className="mx-auto w-full max-w-5xl p-4">
      <div className="flex flex-col gap-4 rounded-2xl border border-slate-700 bg-white/5 p-4 shadow-lg">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <button
              onClick={handlePrev}
              disabled={currentIndex === 0 || loadingTimeline}
              className="rounded-xl bg-slate-800 px-4 py-2 text-sm font-medium text-slate-200 hover:bg-slate-700 disabled:opacity-40"
            >
              ⬅️ Prethodni
            </button>

            <button
              onClick={handleNext}
              disabled={loadingTimeline || currentIndex === timeline.length - 1}
              className="rounded-xl bg-slate-800 px-4 py-2 text-sm font-medium text-slate-200 hover:bg-slate-700 disabled:opacity-40"
            >
              Sledeći ➡️
            </button>
          </div>

          <div className="flex items-center gap-3">
            <span className="text-sm text-slate-300">Skok na dan:</span>
            <select
              value={currentIndex}
              onChange={(e) => setCurrentIndex(Number(e.target.value))}
              className="rounded-xl bg-slate-800 px-3 py-2 text-sm text-slate-200 outline-none ring-1 ring-slate-700 focus:ring-amber-500"
            >
              {timeline.map((t, index) => (
                <option key={t.idNaloga} value={index}>
                  {t.datum} (#{t.idNaloga})
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="flex flex-col gap-1">
          <div className="text-lg font-semibold text-amber-400">
            Nalog #{currentIdNaloga ?? "-"}
          </div>
          <div className="text-sm text-slate-300">
            Datum: {current?.datum ?? "-"}
          </div>
        </div>

        {loadingTimeline && (
          <p className="text-sm text-slate-300">Učitavam listu naloga…</p>
        )}
        {error && <p className="text-sm text-red-400">{error}</p>}

        <div className="relative">
          {showOverlay && (
            <div className="absolute inset-0 z-10 rounded-2xl bg-slate-900/50 backdrop-blur-[2px] flex items-center justify-center">
              <div className="flex items-center gap-3 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 shadow">
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-slate-400 border-t-transparent" />
                <span className="text-sm text-slate-200">
                  Učitavam novi nalog…
                </span>
              </div>
            </div>
          )}

          {sortedItems.length > 0 ? (
            <div className="overflow-x-auto rounded-xl border border-slate-700 ">
              <table className="w-full text-left text-sm text-slate-200">
                <thead className="bg-white/7">
                  <tr>
                    <th className="px-3 py-2 text-left">Pivo</th>
                    <th className="px-3 py-2 text-right">Start</th>
                    <th className="px-3 py-2 text-right">End</th>
                    <th className="px-3 py-2 text-right">Pot.</th>
                    <th className="px-3 py-2 text-right">POS start</th>
                    <th className="px-3 py-2 text-right">POS end</th>
                    <th className="px-3 py-2 text-right">POS pot.</th>
                    <th className="px-3 py-2 text-right">Odst.</th>
                    <th className="px-3 py-2 text-right">Od popisa</th>
                  </tr>
                </thead>

                <tbody>
                  {sortedItems.map((item, index) => (
                    <tr
                      key={item.idPiva ?? index}
                      className="border-t border-slate-700 hover:bg-slate-800/60"
                    >
                      <td className="px-3 py-2 text-left font-medium">
                        {item.nazivPiva}
                      </td>

                      <td className="px-3 py-2 text-right">
                        {fmt(item.vagaStart)}
                      </td>
                      <td className="px-3 py-2 text-right">
                        {fmt(item.vagaEnd)}
                      </td>

                      <td className="px-3 py-2 text-right font-semibold">
                        {item.vagaPotrosnja}
                      </td>

                      <td className="px-3 py-2 text-right">
                        {fmt(item.posStart)}
                      </td>
                      <td className="px-3 py-2 text-right">
                        {fmt(item.posEnd)}
                      </td>

                      <td className="px-3 py-2 text-right font-semibold">
                        {fmt(item.posPotrosnja)}
                      </td>

                      <td
                        className={`px-3 py-2 text-right font-bold ${
                          item.odstupanje === 0
                            ? "text-slate-300"
                            : item.odstupanje < 0
                              ? "text-red-400"
                              : "text-green-400"
                        }`}
                      >
                        {fmt(item.odstupanje)}
                      </td>

                      <td
                        className={`px-3 py-2 text-right
                        ${
                          beerShortageMap[item.idPiva] === 0
                            ? "text-yellow-400"
                            : beerShortageMap[item.idPiva] < 0
                              ? "text-red-400"
                              : "text-green-400"
                        }`}
                      >
                        {fmt(beerShortageMap[item.idPiva])}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="text-sm text-slate-300">Nema unosa za ovaj dan.</p>
          )}

          <div className="mt-4 rounded-2xl border border-slate-700 bg-white/10 p-4">
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-sm font-semibold text-slate-200">
                Totali od popisa
              </h3>

              {loadingTotals && (
                <span className="text-xs text-slate-400">Učitavam…</span>
              )}
            </div>

            {!loadingTotals && displayTotals && (
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-2">
                <div className="rounded-xl bg-white/5 p-3">
                  <div className="text-lg text-slate-400">
                    Total VAGA potrošnja
                  </div>
                  <div className="text-lg font-semibold text-slate-100">
                    {fmt(displayTotals.totalVagaFromInventoryPotrosnja)}
                  </div>
                </div>

                <div className="rounded-xl bg-white/5 p-3">
                  <div className="text-lg text-slate-400">
                    Total POS potrošnja
                  </div>
                  <div className="text-lg font-semibold text-slate-100">
                    {fmt(displayTotals.totalPosFromInventoryPotrosnja)}
                  </div>
                </div>

                <div className="rounded-xl bg-white/5 p-3">
                  <div className="text-lg text-slate-400">Total otpis</div>
                  <div className="text-lg font-semibold text-slate-100">
                    {fmt(displayTotals.totalFromInventoryProsuto)}
                  </div>
                </div>

                <div className="rounded-xl bg-white/5 p-3">
                  <div className="text-lg text-slate-400">Total Manjak</div>
                  <div
                    className={`text-lg font-bold ${
                      displayTotals.totalFromInventoryProsutoPoApp === 0
                        ? "text-slate-100"
                        : displayTotals.totalFromInventoryProsutoPoApp < 0
                          ? "text-red-400"
                          : "text-green-400"
                    }`}
                  >
                    {fmt(displayTotals.totalFromInventoryProsutoPoApp)}
                  </div>
                </div>
              </div>
            )}

            {!loadingTotals && !displayTotals && (
              <p className="text-xs text-slate-400">
                Nema totals podataka za ovaj nalog.
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default AllReports;
