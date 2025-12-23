import { useEffect, useState } from "react";
import { getReportStatesById, getNalogByDate } from "../api/helpers";
import ReportDetails from "./ReportDetails";

function DailyReportPreview({ datum, onidNalogaResolved }) {
  const [data, setData] = useState(null);
  const [idNaloga, setIdNaloga] = useState(null);
  const [showDetails, setShowDetails] = useState(false);

  useEffect(() => {
    if (!datum) return;
    getNalogByDate(datum)
      .then((res) => {
        setIdNaloga(res.idNaloga);
        onidNalogaResolved?.(res.idNaloga);
      })

      .catch(console.error);

    console.log("DATUM:", datum);
  }, [datum]);

  useEffect(() => {
    if (!idNaloga) return;

    getReportStatesById(idNaloga).then(setData).catch(console.error);
  }, [idNaloga]);

  if (!data) {
    return <div>Loading...</div>;
  }

  return (
    <div className="mt-6">
      <h3 className="text-center text-lg text-gray-300 mb-3">
        Total prosuto:{" "}
        <span
          className={`font-semibold ${
            data.totalProsuto < 0 ? "text-green-400" : "text-red-400"
          }`}
        >
          {data.totalProsuto === 0
            ? data.totalProsuto
            : `-${data.totalProsuto}`}
        </span>
      </h3>
      <h3 className="text-center text-lg text-gray-300 mb-3">
        Izmereno prosuto:{" "}
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
            <ReportDetails items={data.items} />
          </div>
        </div>
      )}
    </div>
  );
}

export default DailyReportPreview;
