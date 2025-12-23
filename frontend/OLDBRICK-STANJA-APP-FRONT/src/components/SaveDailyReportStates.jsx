import { useEffect, useState } from "react";
import {
  postDailyReportStates,
  getReportStatesById,
  getAllArticles,
  calculateProsutoRazlika,
  calculateProsutoOnly,
} from "../api/helpers";

function SaveDailyReportStates({ idNaloga }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [values, setValues] = useState({});
  const [articles, setArticles] = useState([]);

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
      const dataToSend = Object.entries(values).map(([idPiva, v]) => ({
        beerId: Number(idPiva),
        izmereno: Number(v.izmereno),
        stanjeUProgramu: Number(v.stanjeUProgramu),
      }));

      await postDailyReportStates(idNaloga, dataToSend);
      await calculateProsutoOnly(idNaloga);
      await calculateProsutoRazlika(idNaloga);
    } catch (err) {
      console.error(err);
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
    <div className="space-y-4">
      {articles.map((b) => (
        <div key={b.id} className="rounded-lg border border-white/10 p-3">
          <div className="font-semibold text-white mb-2">{b.nazivPiva}</div>

          <div className="grid grid-cols-2 gap-3">
            <input
              type="number"
              placeholder="VAGA"
              value={values[b.id]?.izmereno ?? ""}
              onChange={(e) => handleChange(b.id, "izmereno", e.target.value)}
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
      <button
        type="button"
        disabled={Object.keys(values).length === 0}
        onClick={handleSave}
        className="
    mt-4 w-full rounded-lg
    bg-[#FACC15] text-black font-semibold
    px-4 py-2
    transition
    hover:bg-[#fde047]
    disabled:opacity-50
    disabled:cursor-not-allowed
  "
      >
        Sačuvaj stanje
      </button>
    </div>
  );
}

export default SaveDailyReportStates;
