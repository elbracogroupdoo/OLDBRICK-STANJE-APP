import { useState } from "react";
import { createBeer } from "../api/helpers";

export default function AddBeerModal({ isOpen, onClose, onBeerCreated }) {
  const [nazivPiva, setNazivPiva] = useState("");
  const [tipMerenja, setTipMerenja] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  if (!isOpen) return null;

  const handleSubmit = async () => {
    try {
      setError("");

      if (!nazivPiva.trim()) {
        setError("Naziv piva je obavezan.");
        return;
      }

      if (!tipMerenja.trim()) {
        setError("Tip merenja je obavezan.");
        return;
      }

      setLoading(true);

      const createdBeer = await createBeer({
        nazivPiva: nazivPiva.trim(),
        tipMerenja: tipMerenja.trim(),
      });

      setNazivPiva("");
      setTipMerenja("");

      if (onBeerCreated) {
        onBeerCreated(createdBeer);
      }

      onClose();
    } catch (err) {
      setError(err?.response?.data || "Greška prilikom dodavanja piva.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* BACKDROP */}
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* MODAL */}
      <div className="relative z-10 w-full max-w-md rounded-xl bg-[#1f2937] p-6 shadow-2xl">
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-xl font-semibold text-white">Dodaj novo pivo</h2>

          <button
            onClick={onClose}
            className="text-gray-400 hover:text-white text-xl"
          >
            ✕
          </button>
        </div>

        <div className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-white/70">
              Naziv piva
            </label>
            <input
              type="text"
              value={nazivPiva}
              onChange={(e) => setNazivPiva(e.target.value)}
              placeholder="Unesi naziv piva"
              className="h-12 w-full rounded-lg bg-white/10 px-4 text-white text-base placeholder:text-white/40 outline-none transition focus:ring-2 focus:ring-yellow-400"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-white/70">
              Tip merenja
            </label>
            <select
              value={tipMerenja}
              onChange={(e) => setTipMerenja(e.target.value)}
              className="h-12 w-full rounded-lg bg-white/10 px-4 text-white text-base outline-none transition focus:ring-2 focus:ring-yellow-400"
            >
              <option value="" className="text-black">
                Izaberi tip merenja
              </option>
              <option value="Kesa" className="text-black">
                BROJAČ
              </option>
              <option value="Bure" className="text-black">
                VAGA
              </option>
            </select>
          </div>

          {error ? (
            <div className="rounded-lg border border-red-500/20 bg-red-500/10 px-3 py-2 text-sm text-red-300">
              {error}
            </div>
          ) : null}

          <div className="flex justify-end gap-3 pt-2">
            <button
              onClick={onClose}
              disabled={loading}
              className="rounded-lg border border-white/10 px-4 py-2 text-sm font-medium text-white/80 transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Otkaži
            </button>

            <button
              onClick={handleSubmit}
              disabled={loading}
              className="rounded-lg bg-[#FACC15] px-4 py-2 text-sm font-semibold text-black transition hover:bg-[#fde047] disabled:cursor-not-allowed disabled:opacity-50"
            >
              {loading ? "Dodavanje..." : "Dodaj pivo"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
