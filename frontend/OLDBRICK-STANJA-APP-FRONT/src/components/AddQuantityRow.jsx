import { useState } from "react";
import { addBeerQuantity, addMoreBeerQuantity } from "../api/helpers";

function AddQuantityRow({ idNaloga, articles = [], onUpdated }) {
  const [selectedBeerId, setSelectedBeerId] = useState("");
  const [kolicina, setKolicina] = useState("");
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState("");
  const idNalogaDayBefore = idNaloga - 1;
  const [items, setItems] = useState(
    articles.map((article) => ({
      idPiva: article.idPiva,
      kolicina: 0,
      selected: true,
    }))
  );

  async function handleAdd() {
    try {
      setMsg("");

      const idPiva = Number(selectedBeerId);
      const value = Number(kolicina);

      if (!idNalogaDayBefore) return setMsg("Nema idNaloga.");
      if (!idPiva) return setMsg("Izaberi pivo.");
      if (!value || value <= 0) return setMsg("Unesi pozitivnu količinu.");

      setLoading(true);
      const updated = await addBeerQuantity(idNalogaDayBefore, idPiva, value);

      setKolicina("");
      setMsg("Dodato ✅");
      onUpdated?.(updated);
    } catch (e) {
      console.error(e);
      setMsg("Greška pri dodavanju.");
    } finally {
      setLoading(false);
      setTimeout(() => setMsg(""), 2500);
    }
  }

  return (
    <div className="rounded-lg border border-white/10 bg-white/5 p-4">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_140px_120px] sm:items-end">
        {/* PIVO */}
        <div>
          <label className="mb-1 block text-xs font-medium text-white/70">
            Pivo
          </label>
          <select
            value={selectedBeerId}
            onChange={(e) => setSelectedBeerId(e.target.value)}
            className="w-full rounded-lg border border-white/10 bg-white/10 px-3 py-2 text-black
                     outline-none focus:border-white/20"
          >
            <option value="" className="bg-[#1f2937]">
              -- Izaberi pivo --
            </option>
            {articles.map((b) => (
              <option key={b.id} value={b.id} className="bg-[#1f2937]">
                {b.nazivPiva}
              </option>
            ))}
          </select>
        </div>

        {/* KOLIČINA */}
        <div>
          <label className="mb-1 block text-xs font-medium text-black">
            Količina
          </label>
          <input
            className="w-full rounded-lg border border-white/10 bg-amber-300 px-3 py-2 text-black
                     placeholder:text-white/40 outline-none focus:border-white/20"
            value={kolicina}
            onChange={(e) => setKolicina(e.target.value)}
            placeholder="npr. 20"
            type="number"
            step="0.01"
            min="0"
          />
        </div>

        {/* BUTTON */}
        <button
          type="button"
          onClick={handleAdd}
          disabled={loading}
          className="rounded-lg bg-[#FACC15] px-4 font-semibold text-black
                   transition hover:bg-[#fde047] disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "Dodajem..." : "Dodaj"}
        </button>
      </div>

      {msg && (
        <div className="mt-3 rounded-lg border border-white/10 bg-black/20 px-3 py-2 text-sm text-white/80">
          {msg}
        </div>
      )}
    </div>
  );
}

export default AddQuantityRow;
