import React, { useState, useEffect, useMemo } from "react";
import {
  addMoreBeerQuantity,
  getRestockForNalog,
  deleteRestockById,
} from "../api/helpers";

function AddQuantityBatch({ idNaloga, articles = [], onUpdated }) {
  console.log(articles);

  const [beerItems, setBeerItems] = useState([]);
  const [restocks, setRestocks] = useState([]);
  const [restocksLoading, setRestocksLoading] = useState(false);
  const [deletingId, setDeletingId] = useState(null);

  console.log(beerItems);

  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState("");

  useEffect(() => {
    const activeItems = (articles || [])
      .filter((a) => a.isActive)
      .map((a) => {
        if (!a.id) {
          console.error("Ovo pivo nema idPiva!", a);
        }

        return {
          idPiva: a.id,
          kolicina: "",
          nazivPiva: a.nazivPiva,
        };
      });

    setBeerItems(activeItems);
  }, [articles]);

  useEffect(() => {
    if (!idNaloga) return;

    setRestocksLoading(true);

    getRestockForNalog(idNaloga)
      .then((res) => {
        setRestocks(res || []);
      })
      .catch((err) => {
        console.error("Greška pri učitavanju restock unosa:", err);
        setRestocks([]);
      })
      .finally(() => {
        setRestocksLoading(false);
      });
  }, [idNaloga]);

  function handleKolicinaChange(idPiva, value) {
    setBeerItems((prev) => {
      const newState = prev.map((item) => {
        if (item.idPiva === idPiva) {
          return { ...item, kolicina: Number(value) };
        } else {
          return item;
        }
      });
      console.log("beerItems after change:", newState);

      return newState;
    });
  }

  async function handleBatchAdd() {
    try {
      setMsg("");

      // filterujemo samo piva gde je input popunjen
      const payload = beerItems
        .filter((item) => {
          return item.kolicina !== "" && Number(item.kolicina) > 0;
        })
        .map((item) => {
          return { idPiva: item.idPiva, kolicina: Number(item.kolicina) };
        });

      if (payload.length === 0) {
        return setMsg("Nema unetih količina.");
      }
      const idNalogaDayBefore = idNaloga;

      setLoading(true);
      const updated = await addMoreBeerQuantity(idNalogaDayBefore, payload);
      const restocksRes = await getRestockForNalog(idNalogaDayBefore);
      setRestocks(restocksRes || []);

      // reset input polja
      setBeerItems(function (prev) {
        return prev.map((item) => {
          return { ...item, kolicina: "" };
        });
      });

      setMsg("Dodato ✅");
      onUpdated?.(updated);
    } catch (e) {
      console.error(e);
      setMsg("Greška pri dodavanju.");
    } finally {
      setLoading(false);
      setTimeout(() => {
        setMsg("");
      }, 2500);
    }
  }

  async function handleDeleteRestock(id) {
    try {
      setMsg("");
      setDeletingId(id);

      await deleteRestockById(id);

      const refreshed = await getRestockForNalog(idNaloga);
      setRestocks(refreshed || []);

      onUpdated?.();
      setMsg("Obrisano ✅");
    } catch (e) {
      console.error(e);
      setMsg("Greška pri brisanju.");
    } finally {
      setDeletingId(null);
      setTimeout(() => {
        setMsg("");
      }, 2500);
    }
  }

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
    "Kafa",
  ];
  const displayBeerItems = React.useMemo(() => {
    return beerItems.sort((a, b) => {
      const ia = articleOrder.indexOf(a.nazivPiva);
      const ib = articleOrder.indexOf(b.nazivPiva);

      if (ia === -1 && ib === -1) return 0;
      if (ia === -1) return 1;
      if (ib === -1) return -1;

      return ia - ib;
    });
  }, [beerItems]);

  return (
    <div className="flex flex-col gap-4 lg:grid lg:grid-cols-2">
      {/* LEVO - UNOS */}
      <section className="rounded-xl border border-white/10 bg-[#1f2937] p-4">
        <div className="mb-4">
          <h3 className="text-base font-semibold text-white">Dodaj količine</h3>
          <p className="mt-1 text-sm text-white/60">
            Unesi količine za aktivne artikle.
          </p>
        </div>

        <div className="space-y-3">
          {displayBeerItems.map((item) => (
            <div
              key={item.idPiva}
              className="rounded-lg border border-white/10 bg-white/5 p-3"
            >
              <div className="mb-2 text-sm font-medium text-white">
                {item.nazivPiva}
              </div>

              <input
                type="text"
                inputMode="decimal"
                value={item.kolicina}
                onChange={(e) => {
                  const val = e.target.value;

                  if (/^\d*\.?\d*$/.test(val)) {
                    handleKolicinaChange(item.idPiva, val);
                  }
                }}
                placeholder="0"
                className="h-11 w-full rounded-lg border border-white/10 bg-white/10 px-3 text-right text-white outline-none transition placeholder:text-white/30 focus:border-yellow-400/40 focus:ring-2 focus:ring-yellow-400"
              />
            </div>
          ))}
        </div>

        <button
          type="button"
          onClick={handleBatchAdd}
          disabled={loading}
          className="mt-4 h-12 w-full rounded-lg bg-[#FACC15] px-4 text-sm font-semibold text-black transition hover:bg-[#fde047] disabled:cursor-not-allowed disabled:opacity-50"
        >
          {loading ? "Dodajem..." : "Dodaj sve"}
        </button>
      </section>

      {/* DESNO - PRIKAZ DODATIH KOLICINA */}
      <section className="rounded-xl border border-white/10 bg-[#1f2937] p-4">
        <div className="mb-4">
          <h3 className="text-base font-semibold text-white">
            Već dodate količine
          </h3>
          <p className="mt-1 text-sm text-white/60">
            Pregled unosa za izabrani nalog.
          </p>
        </div>

        {restocksLoading ? (
          <div className="rounded-lg border border-white/10 bg-white/5 px-3 py-3 text-sm text-white/60">
            Učitavam...
          </div>
        ) : restocks.length === 0 ? (
          <div className="rounded-lg border border-white/10 bg-white/5 px-3 py-3 text-sm text-white/60">
            Nema dodatih količina.
          </div>
        ) : (
          <div className="space-y-2">
            {restocks.map((item) => (
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="text-sm font-semibold text-white">
                    {item.nazivPiva}
                  </div>
                  <div className="mt-1 text-xs text-white/50">
                    {item.createdAt
                      ? new Date(item.createdAt).toLocaleString("sr-RS")
                      : ""}
                  </div>
                </div>

                <div className="flex shrink-0 items-center gap-2">
                  <div className="rounded-md bg-yellow-400/10 px-2.5 py-1 text-sm font-semibold text-yellow-300">
                    {item.quantity}
                  </div>

                  <button
                    type="button"
                    onClick={() => handleDeleteRestock(item.id)}
                    disabled={deletingId === item.id}
                    className="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-1 text-xs font-semibold text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
                  >
                    {deletingId === item.id ? "Brišem..." : "Obriši"}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

export default AddQuantityBatch;
