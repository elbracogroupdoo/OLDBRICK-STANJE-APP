import { useEffect, useState } from "react";
import {
  getReportStatesById,
  getTotalPotrosnjaVagaAndPos,
  getDailyReportByDateWithBiggerOutput,
  getTotalsSinceLastInventoryReset,
  getBeerShortageTotalsForNalog,
} from "../api/helpers";
import ReportDetails from "./ReportDetails";

function DailyReportPreview({ datum, refreshKey, onidNalogaResolved }) {
  const [data, setData] = useState(null);
  const [idNaloga, setIdNaloga] = useState(null);
  const [showDetails, setShowDetails] = useState(false);
  const [totals, setTotals] = useState(null);
  const [sinceLastInventory, setSinceLastInventory] = useState(null);
  const [loading, setLoading] = useState(false);
  const [shortagePerBeer, setShortagePerBeer] = useState([]);

  useEffect(() => {
    if (!datum) return;

    setLoading(true);
    setData(null);
    setTotals(null);
    setSinceLastInventory(null);
    setShortagePerBeer([]);
    setIdNaloga(null);

    getDailyReportByDateWithBiggerOutput(datum)
      .then((res) => {
        setIdNaloga(res.idNaloga);
        onidNalogaResolved?.(res.idNaloga);
      })
      .catch(console.error);
  }, [datum]);
  useEffect(() => {
    if (!idNaloga) return;

    setLoading(true);
    setData(null);
    setTotals(null);
    setSinceLastInventory(null);
    setShortagePerBeer([]);
    setShowDetails(false);
  }, [idNaloga]);

  useEffect(() => {
    if (!idNaloga) return;

    let cancelled = false;

    (async () => {
      try {
        const [states, totalsRes, inv, shortage] = await Promise.all([
          getReportStatesById(idNaloga),
          getTotalPotrosnjaVagaAndPos(idNaloga),
          getTotalsSinceLastInventoryReset(idNaloga),
          getBeerShortageTotalsForNalog(idNaloga),
        ]);

        if (cancelled) return;

        setData(states);
        setTotals(totalsRes);
        setSinceLastInventory(inv);
        setShortagePerBeer(shortage || []);
      } catch (err) {
        if (cancelled) return;
        console.log("FETCH ERROR:", err);
        setShortagePerBeer([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [idNaloga, refreshKey]);

  console.log("TOTALS IN DailyReportPreview:", totals);
  console.log("SHORTAGE PER BEER:", shortagePerBeer);

  if (!data) {
    return <div>Loading...</div>;
  }

  return (
    <div className="mt-6">
      <h3 className="text-center text-lg text-gray-300 mb-3">
        Manjak vaga - potrošnja POS:{" "}
        <span
          className={`font-semibold ${
            data.totalProsuto < 0 ? "text-red-400" : "text-green-400"
          }`}
        >
          {data.totalProsuto === 0 ? data.totalProsuto : `${data.totalProsuto}`}
        </span>
      </h3>
      <h3 className="text-center text-lg text-gray-300 mb-3">
        OTPIS:{" "}
        <span
          className={`font-semibold ${
            data.prosutoKanta < 0 ? "text-green-400" : "text-red-400"
          }`}
        >
          {data.prosutoKanta === 0
            ? data.prosutoKanta
            : `-${data.prosutoKanta}`}
        </span>
      </h3>
      <button
        type="button"
        onClick={() => setShowDetails(true)}
        className="rounded-lg px-4 py-2 text-sm font-medium
             bg-yellow-400 text-black transition"
      >
        Detaljno dnevno stanje artikala
      </button>
      {showDetails && data && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          {/* BACKDROP */}
          <div
            className="absolute inset-0 bg-black/50 backdrop-blur-sm"
            onClick={() => setShowDetails(false)}
          />

          {/* MODAL WINDOW */}
          <div
            className="relative z-10 w-full max-w-5xl max-h-[85vh]
                    overflow-y-auto rounded-xl
                    bg-[#1f2937] p-6 shadow-2xl"
          >
            {/* HEADER */}
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-white">
                Detaljno dnevno stanje
              </h2>

              <button
                onClick={() => setShowDetails(false)}
                className="text-gray-400 hover:text-white text-xl"
              >
                ✕
              </button>
            </div>

            {/* CONTENT */}
            <ReportDetails
              items={data.items}
              totals={totals}
              sinceLastInventory={sinceLastInventory}
              shortagePerBeer={shortagePerBeer}
            />
          </div>
        </div>
      )}
    </div>
  );
}

export default DailyReportPreview;
