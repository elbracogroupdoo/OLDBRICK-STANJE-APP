import { useEffect, useState } from "react";
import { getAllArticles } from "../api/helpers"; // ispravi putanju
import CleaningSnapshotsForm from "./ClearingSnapshots";

function CleaningSnapshotsModal({
  open,
  onClose,
  idNaloga,
  datum,
  onSaved,
  isReportCalculated,
}) {
  const [beers, setBeers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState("");

  useEffect(() => {
    if (!open) return;

    setLoadError("");
    setLoading(true);

    getAllArticles()
      .then((all) => {
        const onlyKesa = (all || [])
          .filter(
            (x) => (x.tipMerenja || "").toLowerCase() === "kesa" && x.isActive,
          )
          .map((x) => ({
            idPiva: x.id,
            naziv: x.nazivPiva,
          }));

        setBeers(onlyKesa);
      })
      .catch((e) => {
        console.error(e);
        setLoadError("Ne mogu da učitam listu piva.");
        setBeers([]);
      })
      .finally(() => setLoading(false));
  }, [open]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        onClick={onClose}
      />

      <div className="relative z-10 w-[92%] max-w-lg rounded-xl bg-[#1f2937] p-6 shadow-2xl">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-white font-semibold text-lg">Pranje točilica</h3>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-white text-xl"
          >
            ✕
          </button>
        </div>

        {loading ? (
          <div className="text-white/60 text-sm">Učitavam piva…</div>
        ) : loadError ? (
          <div className="text-sm text-red-400">{loadError}</div>
        ) : (
          <CleaningSnapshotsForm
            idNaloga={idNaloga}
            datum={datum}
            beers={beers}
            isReportCalculated={isReportCalculated}
            onSaved={() => {
              onClose?.();
              onSaved?.();
            }}
          />
        )}
      </div>
    </div>
  );
}

export default CleaningSnapshotsModal;
