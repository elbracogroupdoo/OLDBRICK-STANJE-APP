import { useEffect, useState } from "react";
import {
  postDailyReportStates,
  getReportStatesById,
  getAllArticles,
  calculateProsutoRazlika,
  calculateProsutoOnly,
  putMeasuredProsuto,
  postCalculatedProsutoForEachBeer,
} from "../api/helpers";
import ProsutoKantaForm from "./ProsutoKantaForm";

function SaveDailyReportStates({ idNaloga }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [values, setValues] = useState({});
  const [articles, setArticles] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [statusType, setStatusType] = useState("SUCCESS");
  const [prosutoKanta, setProsutoKanta] = useState("");

  function handleChange(idPiva, field, value) {
    setValues((prev) => ({
      ...prev,
      [idPiva]: {
        ...prev[idPiva],
        [field]: value,
      },
    }));
  }

  async function handleSave() {
    try {
      setLoading(true);
      setStatusMessage("");

      const dataToSend = Object.entries(values).map(([idPiva, v]) => ({
        beerId: Number(idPiva),
        izmereno: Number(v.izmereno),
        stanjeUProgramu: Number(v.stanjeUProgramu),
      }));

      await postDailyReportStates(idNaloga, dataToSend);

      const proKanta = Number(prosutoKanta);

      if (!Number.isNaN(proKanta)) {
        await putMeasuredProsuto(idNaloga, proKanta);
      }

      await calculateProsutoOnly(idNaloga);
      await calculateProsutoRazlika(idNaloga);
      await postCalculatedProsutoForEachBeer(idNaloga);

      setStatusType("SUCCESS");
      setStatusMessage("SAČUVANO USPEŠNO");
      setTimeout(() => setStatusMessage(""), 3000);
    } catch (err) {
      console.error(err);
      setStatusType("error");
      setStatusMessage("Greška pri čuvanju. Pokušaj ponovo.");
      setTimeout(() => setStatusMessage(""), 3000);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    getAllArticles().then(setArticles).catch(console.error);
  }, []);

  useEffect(() => {
    if (!idNaloga) return;

    setLoading(true);
    getReportStatesById(idNaloga)
      .then((res) => {
        setItems(res.items);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [idNaloga]);

  if (!idNaloga) {
    return <div className="text-white/60">Učitavanje...</div>;
  }

  if (loading) {
    return;
  }

  return (
    <div className="mt-4">
      {/* Trigger button */}
      <button
        type="button"
        onClick={() => setShowModal(true)}
        disabled={!idNaloga}
        className="rounded-lg px-4 py-2 text-sm font-medium bg-yellow-400 text-black transition disabled:opacity-50"
      >
        Unesi dnevno stanje
      </button>

      {/* MODAL */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          {/* BACKDROP */}
          <div
            className="absolute inset-0 bg-black/50 backdrop-blur-sm"
            onClick={() => setShowModal(false)}
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
                Unos dnevnog stanja
              </h2>

              <button
                type="button"
                onClick={() => setShowModal(false)}
                className="text-gray-400 hover:text-white text-xl"
              >
                ✕
              </button>
            </div>

            {/* CONTENT (tvoja postojeća forma) */}
            <div className="space-y-4">
              {articles.map((b) => (
                <div
                  key={b.id}
                  className="rounded-lg border border-white/10 p-3"
                >
                  <div className="font-semibold text-white mb-2">
                    {b.nazivPiva}
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <input
                      type="number"
                      placeholder="VAGA"
                      value={values[b.id]?.izmereno ?? ""}
                      onChange={(e) =>
                        handleChange(b.id, "izmereno", e.target.value)
                      }
                      className="rounded bg-white/10 px-3 py-2 text-white"
                    />

                    <input
                      type="number"
                      placeholder="PROGRAM"
                      value={values[b.id]?.stanjeUProgramu ?? ""}
                      onChange={(e) =>
                        handleChange(b.id, "stanjeUProgramu", e.target.value)
                      }
                      className="rounded bg-white/10 px-3 py-2 text-white"
                    />
                  </div>
                </div>
              ))}
              <ProsutoKantaForm
                idNaloga={idNaloga}
                onChange={setProsutoKanta}
                value={prosutoKanta}
              />
              {statusMessage && (
                <div
                  className={[
                    "rounded-lg px-3 py-2 text-sm border",
                    statusType === "success"
                      ? "bg-green-500/10 text-green-300 border-green-500/20"
                      : "bg-red-500/10 text-red-300 border-red-500/20",
                  ].join(" ")}
                >
                  {statusMessage}
                </div>
              )}

              <button
                type="button"
                disabled={Object.keys(values).length === 0}
                onClick={handleSave}
                className="mt-4 w-full rounded-lg bg-[#FACC15] text-black font-semibold
                         px-4 py-2 transition hover:bg-[#fde047]
                         disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Sačuvaj stanje
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default SaveDailyReportStates;
