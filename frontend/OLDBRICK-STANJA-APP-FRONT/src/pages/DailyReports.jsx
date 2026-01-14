import { useState, useEffect } from "react";
import DailyReportPreview from "../components/DailyReportPreview";
import Calendar from "../components/Calendar";
import {
  createNalogByDate,
  getNalogByDate,
  createInventoryReset,
  deleteDailyReport,
  getKesaItemsForDate,
} from "../api/helpers";
import SaveDailyReportStates from "../components/SaveDailyReportStates";

function DailyReports() {
  const [datum, setDatum] = useState("");
  const [idNaloga, setIdNaloga] = useState(null);
  const [statusMessageForNalog, setStatusMessageForNalog] = useState("");
  const [isInvOpen, setIsInvOpen] = useState(false);
  const [invDate, setInvDate] = useState(""); // "YYYY-MM-DD"
  const [invNote, setInvNote] = useState("");
  const [invLoading, setInvLoading] = useState(false);
  const [invStatus, setInvStatus] = useState(""); // poruka u modalu
  const [showInvSuccess, setShowInvSuccess] = useState(false);
  const [calendarPatch, setCalendarPatch] = useState(null);
  const [kesaItems, setKesaItems] = useState([]);
  const [kesaPos, setKesaPos] = useState({});
  const [kesaLoading, setKesaLoading] = useState(false);

  async function handleGetOrCreateNalog(datum) {
    try {
      const existing = await getNalogByDate(datum);

      // Ako postoji — samo setujemo idNaloga i (opciono) patchujemo kalendar
      if (existing?.idNaloga) {
        setIdNaloga(existing.idNaloga);

        setCalendarPatch({ type: "add", datum, idNaloga: existing.idNaloga });

        return;
      }

      // Ako ne postoji — kreirajmo
      const res = await createNalogByDate(datum);
      setIdNaloga(res.idNaloga);

      //  odmah obojimo datum
      setCalendarPatch({ type: "add", datum, idNaloga: res.idNaloga });
    } catch (err) {
      console.error(err);
    }
  }
  async function handleDeleteNalog(idNaloga) {
    try {
      if (!idNaloga) return;

      await deleteDailyReport(idNaloga);

      setCalendarPatch({ type: "remove", idNaloga });
      setIdNaloga(null);
    } catch (err) {
      console.error(err);
      throw err;
    }
  }

  async function handleCalendarChange(datum) {
    setDatum(datum);
    handleGetOrCreateNalog(datum);
  }
  function handleKesaChange(idPiva, value) {
    setKesaPos((prev) => ({ ...prev, [idPiva]: value }));
  }

  console.log("URL", import.meta.env.VITE_API_BASE_URL);

  function openInventoryModal() {
    const d =
      typeof datum === "string"
        ? datum
        : datum?.toISOString?.().slice(0, 10) ?? "";

    setInvDate(d);
    setInvNote("");
    setInvStatus("");
    setIsInvOpen(true);
  }

  async function submitInventoryReset() {
    try {
      setInvLoading(true);
      setInvStatus("");

      if (!invDate) {
        setInvStatus("Izaberi datum popisa.");
        return;
      }
      const isKesaValid =
        kesaItems.length === 0 ||
        kesaItems.every((x) => {
          const raw = (kesaPos[x.idPiva] ?? "").toString().trim();
          const num = Number(raw);
          return raw !== "" && Number.isFinite(num) && num >= 0;
        });

      if (!isKesaValid) {
        setInvStatus("Moraš uneti POS vrednosti za sva KESA piva.");
        return;
      }

      const payload = {
        datumPopisa: invDate,
        napomena: (invNote || "").trim(),
        kesaPosOverrides: kesaItems.map((x) => ({
          idPiva: x.idPiva,
          posValue: Number((kesaPos[x.idPiva] ?? "").toString().trim()),
        })),
      };

      await createInventoryReset(payload);

      setIsInvOpen(false);
      setShowInvSuccess(true);
    } catch (e) {
      console.error(e);
      setInvStatus("Greška pri popisu. Pokušaj ponovo.");
    } finally {
      setInvLoading(false);
    }
  }
  useEffect(() => {
    if (!isInvOpen) return;
    if (!invDate) return;

    setKesaLoading(true);
    getKesaItemsForDate(invDate)
      .then((items) => {
        setKesaItems(items);

        // init input state
        const init = {};
        items.forEach((x) => (init[x.idPiva] = ""));
        setKesaPos(init);
      })
      .catch((e) => {
        console.error(e);
        setInvStatus("Ne mogu da učitam KESA piva za izabrani datum.");
        setKesaItems([]);
        setKesaPos({});
      })
      .finally(() => setKesaLoading(false));
  }, [isInvOpen, invDate]);

  return (
    <div className="pt-20 px-4 relative">
      <button
        type="button"
        onClick={openInventoryModal}
        className="absolute top-20 right-4 h-10 rounded-lg bg-blue-500 px-4 text-sm font-semibold text-white
               transition hover:bg-blue-600"
      >
        Popis
      </button>

      <h1 className="text-xl font-semibold text-white">KREIRAJ DNEVNO NALOG</h1>

      <div className="pt-20 px-4">
        <div className="mt-4">
          <Calendar
            value={datum}
            onChange={handleCalendarChange}
            calendarPatch={calendarPatch}
          />

          {statusMessageForNalog && (
            <div className="fixed inset-0 z-50 flex items-center justify-center">
              <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" />
              <div className="relative z-10 rounded-xl bg-[#1f2937] px-6 py-4 shadow-2xl text-center max-w-sm w-[90%]">
                <div className="text-white text-md">
                  {statusMessageForNalog}
                </div>
              </div>
            </div>
          )}
        </div>

        <SaveDailyReportStates
          idNaloga={idNaloga}
          onDelete={handleDeleteNalog}
        />
      </div>

      <DailyReportPreview datum={datum} onidNalogaResolved={setIdNaloga} />

      {/*ONCLICK SE POJAVI */}

      {isInvOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div
            className="absolute inset-0 bg-black/50 backdrop-blur-sm"
            onClick={() => setIsInvOpen(false)}
          />

          <div className="relative z-10 w-[92%] max-w-lg rounded-xl bg-[#1f2937] p-6 shadow-2xl">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-white font-semibold text-lg">Izvrši popis</h3>
              <button
                type="button"
                onClick={() => setIsInvOpen(false)}
                className="text-gray-400 hover:text-white text-xl"
              >
                ✕
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="block text-sm text-white/70 mb-1">
                  Datum
                </label>
                <input
                  type="date"
                  value={invDate}
                  onChange={(e) => setInvDate(e.target.value)}
                  className="h-12 w-full rounded-lg bg-white/10 px-4 text-white text-base"
                />
              </div>

              <div>
                <label className="block text-sm text-white/70 mb-1">
                  Napomena
                </label>
                <input
                  type="text"
                  value={invNote}
                  onChange={(e) => setInvNote(e.target.value)}
                  placeholder="Opcionalno…"
                  className="h-12 w-full rounded-lg bg-white/10 px-4 text-white text-base placeholder:text-white/40"
                />
              </div>
              {kesaItems.length > 0 && (
                <div className="rounded-lg border border-white/10 p-4">
                  <div className="text-white font-semibold mb-3">
                    KESA (obavezno)
                  </div>

                  {kesaLoading ? (
                    <div className="text-white/60 text-sm">Učitavam…</div>
                  ) : (
                    <div className="space-y-3">
                      {kesaItems.map((x) => (
                        <div key={x.idPiva} className="flex items-center gap-3">
                          <div className="w-28 text-white/80">
                            {x.nazivPiva}
                          </div>

                          <input
                            type="number"
                            placeholder="POS"
                            value={kesaPos[x.idPiva] ?? ""}
                            onChange={(e) =>
                              handleKesaChange(x.idPiva, e.target.value)
                            }
                            className="h-12 w-full rounded-lg bg-white/10 px-4 text-white"
                          />
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {invStatus && (
                <div
                  className={[
                    "rounded-lg px-3 py-2 text-sm border",
                    invStatus.includes("✅")
                      ? "bg-green-500/10 text-green-300 border-green-500/20"
                      : "bg-red-500/10 text-red-300 border-red-500/20",
                  ].join(" ")}
                >
                  {invStatus}
                </div>
              )}

              <button
                type="button"
                onClick={submitInventoryReset}
                disabled={invLoading}
                className="h-12 w-full rounded-lg bg-[#FACC15] text-black font-semibold
                     px-4 py-2 transition hover:bg-[#fde047]
                     disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {invLoading ? "Izvršavam…" : "Izvrši popis"}
              </button>
            </div>
          </div>
        </div>
      )}
      {showInvSuccess && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          {/* BACKDROP */}
          <div
            className="absolute inset-0 bg-black/50 backdrop-blur-sm"
            onClick={() => setShowInvSuccess(false)}
          />

          {/* POPUP */}
          <div className="relative z-10 max-w-sm w-[90%] rounded-xl bg-[#1f2937] px-6 py-5 shadow-2xl text-center">
            <div className="text-green-400 text-lg font-semibold mb-2">
              Popis uspešno izvršen
            </div>

            <div className="text-white/80 text-sm mb-4">
              Inventar je uspešno resetovan za izabrani datum.
            </div>

            <button
              type="button"
              onClick={() => setShowInvSuccess(false)}
              className="h-10 rounded-lg bg-[#FACC15] px-6 text-sm font-semibold text-black
                   transition hover:bg-[#fde047]"
            >
              OK
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export default DailyReports;
