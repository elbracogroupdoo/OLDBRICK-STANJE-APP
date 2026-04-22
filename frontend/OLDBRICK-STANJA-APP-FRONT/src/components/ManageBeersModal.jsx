import { useEffect, useState } from "react";
import { getAllArticles, updateBeerActiveStates } from "../api/helpers";

export default function ManageBeersModal({ isOpen, onClose, onSaved }) {
  const [beers, setBeers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (!isOpen) return;

    setLoading(true);

    getAllArticles()
      .then((res) => {
        setBeers(res);
      })
      .catch((err) => {
        console.error(err);
      })
      .finally(() => {
        setLoading(false);
      });
  }, [isOpen]);

  function handleToggleBeer(id) {
    setBeers((prev) =>
      prev.map((beer) =>
        beer.id === id ? { ...beer, isActive: !beer.isActive } : beer,
      ),
    );
  }

  async function handleSave() {
    try {
      setSaving(true);
      setError("");

      const payload = {
        beers: beers.map((beer) => ({
          id: beer.id,
          isActive: beer.isActive,
        })),
      };

      await updateBeerActiveStates(payload);

      if (onSaved) {
        onSaved();
      }

      setSuccess(true);
      onClose();
    } catch (err) {
      console.error(err);
      setError("Greška pri čuvanju statusa piva.");
    } finally {
      setSaving(false);
    }
  }

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* BACKDROP */}
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* MODAL */}
      <div className="relative z-10 w-full max-w-lg max-h-[80vh] overflow-y-auto rounded-xl bg-[#1f2937] p-6 shadow-2xl">
        {/* HEADER */}
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-white">
            Upravljanje pivima
          </h2>

          <button
            onClick={onClose}
            className="text-gray-400 hover:text-white text-xl"
          >
            ✕
          </button>
        </div>

        {/* CONTENT */}
        {loading ? (
          <div className="text-white/60 text-sm">Učitavam piva…</div>
        ) : (
          <div className="space-y-3">
            {beers.map((beer) => (
              <div
                key={beer.id}
                className="flex items-center justify-between rounded-lg border border-white/10 px-4 py-3"
              >
                {/* LEVA STRANA */}
                <div className="flex flex-col items-start">
                  <div className="text-white font-medium">{beer.nazivPiva}</div>
                  <div className="text-sm text-white/50">
                    {beer.tipMerenja === "Kesa"
                      ? "BROJAČ"
                      : beer.tipMerenja === "Bure"
                        ? "VAGA"
                        : beer.tipMerenja === "Kafa"
                          ? "KAFA"
                          : beer.tipMerenja}
                  </div>
                </div>

                {/* DESNA STRANA */}
                <label className="flex items-center gap-2 text-sm text-white/80">
                  <input
                    type="checkbox"
                    checked={beer.isActive}
                    onChange={() => handleToggleBeer(beer.id)}
                    className="h-4 w-4 accent-yellow-400"
                  />
                  Aktivno
                </label>
              </div>
            ))}
          </div>
        )}
        {error ? (
          <div className="mt-4 rounded-lg border border-red-500/20 bg-red-500/10 px-3 py-2 text-sm text-red-300">
            {error}
          </div>
        ) : null}

        <div className="mt-5 flex justify-end gap-3">
          <button
            onClick={onClose}
            disabled={saving}
            className="rounded-lg border border-white/10 px-4 py-2 text-sm font-medium text-white/80 transition hover:bg-white/10 disabled:opacity-50"
          >
            Otkaži
          </button>

          <button
            onClick={handleSave}
            disabled={saving}
            className="rounded-lg bg-[#FACC15] px-4 py-2 text-sm font-semibold text-black transition hover:bg-[#fde047] disabled:opacity-50"
          >
            {saving ? "Čuvanje..." : "Sačuvaj"}
          </button>
        </div>
        {success && (
          <div className="fixed inset-0 z-50 flex items-center justify-center">
            <div className="absolute inset-0 bg-black/50" />
            <div className="relative z-10 rounded-xl bg-[#1f2937] px-6 py-4 text-white">
              Uspešno sačuvano ✅
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
