import React, { useEffect, useState, useMemo } from "react";
import {
  postDailyReportStates,
  getReportStatesById,
  getAllArticles,
  calculateProsutoRazlika,
  calculateProsutoOnly,
  putMeasuredProsuto,
  postCalculatedProsutoForEachBeer,
  deleteDailyReport,
  updateDailyReportStatusAndCalculate,
  updateProsutoKantaAndRecalculate,
  getDayBeforeStates,
  saveDailyBeerShortage,
  getBeerShortageTotalsForNalog,
} from "../api/helpers";
import ProsutoKantaForm from "./ProsutoKantaForm";
import AddQuantityBatch from "./AddQuantityBatch";

function SaveDailyReportStates({ idNaloga, onDelete, onSaved }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [values, setValues] = useState({});
  const [articles, setArticles] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [statusType, setStatusType] = useState("SUCCESS");
  const [prosutoKanta, setProsutoKanta] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [msg, setMsg] = useState("");
  const [mode, setMode] = useState("create");
  const [dayBeforeState, setDayBeforeState] = useState([]);
  const [prevMap, setPrevMap] = useState({});
  const [statusPopupOpen, setStatusPopupOpen] = useState(false);

  function handleChange(idPiva, field, value) {
    setValues((prev) => ({
      ...prev,
      [idPiva]: {
        ...prev[idPiva],
        [field]: value,
      },
    }));
  }

  function openEditModal() {
    const prefill = {};
    items.forEach((x) => {
      prefill[x.idPiva] = {
        izmereno: x.vagaEnd ?? "",
        stanjeUProgramu: x.posEnd ?? "",
      };
    });

    setValues(prefill);
    setMode("edit");
    setShowModal(true);
  }

  async function refreshStates() {
    if (!idNaloga) return;

    const res = await getReportStatesById(idNaloga);
    setItems(res.items);
    const arr = await getDayBeforeStates(idNaloga);
    setDayBeforeState(arr);
    setPrevMap(Object.fromEntries(arr.map((x) => [x.idPiva, x])));
  }

  async function handleSave() {
    setLoading(true);
    setStatusMessage("");

    try {
      // 1) Payload za piva (filter da ne šalješ NaN)
      const dataToSend = Object.entries(values)
        .map(([idPiva, v]) => ({
          beerId: Number(idPiva),
          izmereno: v?.izmereno === "" ? null : Number(v?.izmereno),
          stanjeUProgramu:
            v?.stanjeUProgramu === "" ? null : Number(v?.stanjeUProgramu),
        }))
        .filter(
          (x) =>
            Number.isFinite(x.beerId) &&
            (x.izmereno === null || Number.isFinite(x.izmereno)) &&
            (x.stanjeUProgramu === null || Number.isFinite(x.stanjeUProgramu)),
        );

      console.log("[SAVE] postDailyReportStates payload:", dataToSend);
      await postDailyReportStates(idNaloga, dataToSend);
      console.log("[SAVE] postDailyReportStates OK");

      // 2) Prosuto kanta (šalji samo ako je uneto)
      const rawProsuto = (prosutoKanta ?? "").toString().trim();
      if (rawProsuto !== "") {
        const proKanta = Number(rawProsuto);

        if (!Number.isFinite(proKanta) || proKanta < 0) {
          setStatusType("error");
          setStatusMessage("Prosuto (kanta) mora biti broj >= 0.");
          return;
        }

        console.log("[SAVE] putMeasuredProsuto:", proKanta);
        await putMeasuredProsuto(idNaloga, proKanta);
        console.log("[SAVE] putMeasuredProsuto OK");
      } else {
        console.log("[SAVE] prosutoKanta preskočeno (prazno)");
      }

      // 3) Calculate prosuto
      console.log("[SAVE] calculateProsutoOnly start");
      await calculateProsutoOnly(idNaloga);
      console.log("[SAVE] calculateProsutoOnly OK");

      console.log("[SAVE] calculateProsutoRazlika start");
      await calculateProsutoRazlika(idNaloga);
      console.log("[SAVE] calculateProsutoRazlika OK");

      console.log("[SAVE] saveDailyBeerShortage start");
      await saveDailyBeerShortage(idNaloga);
      console.log("[SAVE] saveDailyBeerShortage OK");

      onSaved?.();

      setStatusType("SUCCESS");
      setStatusMessage("SAČUVANO USPEŠNO");
      setStatusPopupOpen(true);
      setTimeout(() => setStatusMessage(""), 3000);
    } catch (err) {
      // najbitnije: loguj server response ako postoji (axios)
      console.error(
        "[SAVE] FAILED:",
        err?.response?.status,
        err?.response?.data || err,
      );

      setStatusType("error");
      setStatusMessage("Greška pri čuvanju. Pokušaj ponovo.");
      setTimeout(() => setStatusMessage(""), 3000);
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete() {
    const ok = window.confirm(
      "Da li ste sigurni da želite da obrišete ovaj nalog?",
    );
    if (!ok) return;

    try {
      await onDelete(idNaloga);
      setMsg("Nalog je obrisan uspešno!");
    } catch (error) {
      console.error(error);
      setMsg("Greška pri brisanju naloga.");
    }
  }
  async function handleUpdateSave() {
    console.log("EDIT MODE:", mode);
    console.log("VALUES STATE:", values);

    try {
      setLoading(true);
      setMsg("");

      const payload = Object.entries(values).map(([idPiva, v]) => ({
        idPiva: Number(idPiva),
        izmereno: v.izmereno === "" ? null : Number(v.izmereno),
        stanjeUProgramu:
          v.stanjeUProgramu === "" ? null : Number(v.stanjeUProgramu),
      }));

      const result = await updateDailyReportStatusAndCalculate(
        idNaloga,
        payload,
      );

      const proKanta = Number(prosutoKanta);
      console.log(
        "prosutoKanta raw:",
        prosutoKanta,
        "num:",
        Number(prosutoKanta),
      );

      const raw = (prosutoKanta ?? "").toString().trim();

      if (raw !== "") {
        const proKanta = Number(raw);

        if (!Number.isFinite(proKanta) || proKanta < 0) {
          setMsg("Prosuto mora biti broj >= 0 ");
          return;
        }

        const resultAfterKanta = await updateProsutoKantaAndRecalculate(
          idNaloga,
          proKanta,
        );
        setItems(resultAfterKanta.items);
      } else {
        setItems(result.items);
      }

      await saveDailyBeerShortage(idNaloga);
      await getBeerShortageTotalsForNalog(idNaloga);

      onSaved?.();
      setStatusType("SUCCESS");
      setStatusPopupOpen(true);
      setMsg("Izmenjeno i preračunato");
      setShowModal(false);
    } catch (e) {
      console.error(e);
      setMsg("Greška pri izmeni");
      setStatusType("error");
      setStatusPopupOpen(true);
    } finally {
      setLoading(false);
    }
  }

  function handleClearInputs() {
    setValues({});
    localStorage.removeItem(STORAGE_KEY);
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

  useEffect(() => {
    if (!idNaloga) return;

    getDayBeforeStates(idNaloga)
      .then((arr) => {
        setDayBeforeState(arr);
        const map = Object.fromEntries(arr.map((x) => [x.idPiva, x]));
        setPrevMap(map);
      })
      .catch(console.error);
  }, [idNaloga]);

  const STORAGE_KEY = `daily-values-${idNaloga}`;

  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) setValues(JSON.parse(raw));
    } catch {}
  }, [idNaloga]);

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(values));
    } catch {}
  }, [values, idNaloga]);

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
    "Lav",
    "Kafa",
  ];

  const displayArticles = React.useMemo(() => {
    return [...articles].sort((a, b) => {
      const ia = articleOrder.indexOf(a.nazivPiva);
      const ib = articleOrder.indexOf(b.nazivPiva);

      if (ia === -1 && ib === -1) return 0;
      if (ia === -1) return 1;
      if (ib === -1) return -1;

      return ia - ib;
    });
  }, [articles]);
  const isKesa = items.tipmerenja;

  if (!idNaloga) {
    return <div className="text-white/60">Učitavanje...</div>;
  }

  if (loading) {
    return;
  }

  return (
    <div className="mt-4">
      {/* Trigger button */}
      <div className="flex flex-col gap-3 items-center">
        <button
          type="button"
          onClick={() => setShowModal(true)}
          disabled={!idNaloga}
          className="w-full max-w-md rounded-lg px-4 py-2 text-lg font-medium bg-yellow-400 text-black transition disabled:opacity-50"
        >
          Unesi dnevno stanje
        </button>

        <button
          onClick={openEditModal}
          className="w-full max-w-md rounded-lg px-4 py-2 text-lg font-medium bg-yellow-400 text-black transition disabled:opacity-50"
        >
          Izmeni dnevno stanje
        </button>

        <button
          onClick={handleDelete}
          className="w-full max-w-md rounded-lg px-4 py-2 text-lg font-medium bg-red-500 text-white transition hover:bg-red-600"
        >
          Obriši nalog
        </button>
      </div>

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
                {mode === "edit"
                  ? "Izmena dnevnog stanja"
                  : "Unos dnevnog stanja"}
              </h2>
              <button
                onClick={() => setIsModalOpen(true)}
                className="rounded-md bg-[#2A2F36] px-4 py-2 text-white hover:opacity-90"
              >
                Dodaj količine
              </button>
              <button
                type="button"
                onClick={handleClearInputs}
                className="mt-4 rounded-lg border border-red-500/40
             px-4 py-2 text-sm text-red-400
             hover:bg-red-500/10 transition"
              >
                Obriši unose
              </button>

              {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center">
                  {/* overlay */}
                  <div
                    className="absolute inset-0 bg-black/40"
                    onClick={() => setIsModalOpen(false)}
                  />

                  {/* modal box */}
                  {/* <div className="relative z-10  rounded-xl bg-white p-6 shadow-xl">
                    <AddQuantityRow idNaloga={idNaloga} articles={articles} />
                  </div> */}
                  <div className="relative z-10  rounded-xl bg-black p-6 shadow-xl ">
                    <AddQuantityBatch
                      idNaloga={idNaloga}
                      articles={articles}
                      onUpdated={refreshStates}
                    />
                  </div>
                </div>
              )}

              <button
                type="button"
                onClick={() => setShowModal(false)}
                className="text-gray-400 hover:text-white text-xl"
              >
                ✕
              </button>
            </div>

            <div className="space-y-4">
              {displayArticles.map((b) => {
                const isKesa =
                  (b.tipMerenja || "").trim().toLowerCase() === "kesa";
                const prev = prevMap[b.id]; // JUCERASNJE VREDNOSTI ZA SVAKO PIVO
                const vagaDanas = Number(values[b.id]?.izmereno);
                const posDanas = Number(values[b.id]?.stanjeUProgramu);

                const razlikaVaga =
                  prev?.prevVaga != null && !Number.isNaN(vagaDanas)
                    ? Number(prev.prevVaga) - vagaDanas
                    : null;
                const razlikaPos =
                  prev?.prevPos != null && !Number.isNaN(posDanas)
                    ? Number(prev.prevPos) - posDanas
                    : null;

                return (
                  <div
                    key={b.id}
                    className="rounded-lg border border-white/10 p-3"
                  >
                    <div className="font-semibold text-white mb-2">
                      {b.nazivPiva}
                    </div>

                    <div className="mb-2 flex items-center justify-between text-xs text-gray-400">
                      <span>
                        Prethodni dan {isKesa ? "BROJAČ" : "VAGA"}:{" "}
                        <span className="text-gray-200 font-medium">
                          {prev?.prevVaga != null
                            ? Number(prev.prevVaga).toFixed(2)
                            : "—"}
                        </span>
                      </span>

                      <span>
                        Prethodni dan PROGRAM:{" "}
                        <span className="text-gray-200 font-medium">
                          {prev?.prevPos != null
                            ? Number(prev.prevPos).toFixed(2)
                            : "—"}
                        </span>
                      </span>
                    </div>

                    <div className="grid grid-cols-2 gap-3">
                      <input
                        type="number"
                        placeholder={isKesa ? "BROJAČ" : "VAGA"}
                        value={values[b.id]?.izmereno ?? ""}
                        onChange={(e) =>
                          handleChange(b.id, "izmereno", e.target.value)
                        }
                        onWheel={(e) => e.currentTarget.blur()}
                        className={`rounded bg-white/10 px-3 py-2 text-white
          ${
            isKesa ? "placeholder:text-blue-400" : "placeholder:text-gray-400"
          }`}
                      />

                      <input
                        type="number"
                        placeholder="PROGRAM"
                        value={values[b.id]?.stanjeUProgramu ?? ""}
                        onChange={(e) =>
                          handleChange(b.id, "stanjeUProgramu", e.target.value)
                        }
                        onWheel={(e) => e.currentTarget.blur()}
                        className="rounded bg-white/10 px-3 py-2 text-white"
                      />
                    </div>
                    <div className="mt-2 flex items-center justify-end text-xs text-gray-400">
                      {/* <span>
                        Potrošnja {isKesa ? "BROJAČ" : "VAGA"}:{" "}
                        <span className="text-gray-200 font-medium">
                          {razlikaVaga != null ? razlikaVaga.toFixed(2) : "—"}
                        </span>
                      </span> */}

                      <span>
                        Potrošnja POS:{" "}
                        <span className="text-gray-200 font-medium">
                          {razlikaPos != null ? razlikaPos.toFixed(2) : "—"}
                        </span>
                      </span>
                    </div>
                  </div>
                );
              })}
              <ProsutoKantaForm
                idNaloga={idNaloga}
                onChange={setProsutoKanta}
                value={prosutoKanta}
              />
              {statusMessage && (
                <div
                  className={[
                    "rounded-lg px-3 py-2 text-sm border",
                    statusType === "SUCCESS"
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
                onClick={mode === "edit" ? handleUpdateSave : handleSave}
                className="mt-4 w-full rounded-lg bg-[#FACC15] text-black font-semibold
             px-4 py-2 transition hover:bg-[#fde047]
             disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {mode === "edit" ? "Sačuvaj izmene" : "Sačuvaj stanje"}
              </button>
            </div>
          </div>
        </div>
      )}
      {statusPopupOpen && (
        <div className="fixed inset-0 z-50  flex items-center justify-center">
          {/* blur backdrop */}
          <div
            className="absolute inset-0 bg-black/40 backdrop-blur-sm"
            onClick={() => setStatusPopupOpen(false)}
          />
          {/* popup */}
          <div className="relative z-10 w-full max-w-sm rounded-xl bg-[#111827] p-5 shadow-2xl border border-white/10">
            <div className="text-white font-semibold text-base">
              {statusType === "SUCCESS" ? "Uspeh ✅" : "Greška ❌"}
            </div>

            <div className="mt-2 text-white/80 text-sm">
              {mode === "edit"
                ? statusType === "SUCCESS"
                  ? "Izmene su uspešno sačuvane i preračunate."
                  : "Greška pri izmeni dnevnog stanja."
                : statusType === "SUCCESS"
                  ? "Dnevno stanje je uspešno sačuvano."
                  : "Greška pri čuvanju dnevnog stanja."}
            </div>

            <div className="mt-4 flex justify-end gap-2">
              <button
                onClick={() => setStatusPopupOpen(false)}
                className="rounded-lg bg-white/10 px-4 py-2 text-sm text-white hover:bg-white/15"
              >
                OK
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default SaveDailyReportStates;
