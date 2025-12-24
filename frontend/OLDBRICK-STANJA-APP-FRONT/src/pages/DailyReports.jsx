import { useState } from "react";
import DailyReportPreview from "../components/DailyReportPreview";
import Calendar from "../components/Calendar";
import ProsutoKantaForm from "../components/ProsutoKantaForm";
import { createNalogByDate, getNalogByDate } from "../api/helpers";
import SaveDailyReportStates from "../components/SaveDailyReportStates";

function DailyReports() {
  const [datum, setDatum] = useState("");
  const [idNaloga, setIdNaloga] = useState(null);
  const [statusMessageForNalog, setStatusMessageForNalog] = useState("");

  async function handleGetOrCreateNalog(datum) {
    try {
      const existing = await getNalogByDate(datum);

      if (existing) {
        setStatusMessageForNalog("Nalog za izabrani datum već postoji.");
        setTimeout(() => setStatusMessageForNalog(""), 1000);
        return;
      }

      const res = await createNalogByDate(datum);
      setIdNaloga(res.idNaloga);

      setStatusMessageForNalog(`Nalog za datum ${datum} je uspešno kreiran.`);
      setTimeout(() => setStatusMessageForNalog(""), 1000);
    } catch (err) {
      console.error(err);
      setStatusMessageForNalog("Došlo je do greške prilikom kreiranja naloga.");
      setTimeout(() => setStatusMessageForNalog(""), 1000);
    }
  }
  async function handleCalendarChange(datum) {
    setDatum(datum);
    handleGetOrCreateNalog(datum);
  }

  return (
    <div className="pt-20 px-4">
      <h1 className="text-xl font-semibold text-white">Unesi stanje</h1>
      <div className="pt-20 px-4">
        <div className="mt-4">
          <Calendar value={datum} onChange={handleCalendarChange} />
          {/* <button
            type="button"
            disabled={!datum}
            onClick={async () => {
              try {
                const existing = await getNalogByDate(datum);
                if (existing) {
                  setStatusMessageForNalog(
                    "Nalog za izabrani datum već postoji."
                  );
                  setTimeout(() => setStatusMessageForNalog(""), 3000);
                  return;
                }
                const res = await createNalogByDate(datum);
                setIdNaloga(res.idNaloga);
                setStatusMessageForNalog(
                  `Nalog za datum ${datum} je uspešno kreiran.`
                );
                setTimeout(() => setStatusMessageForNalog(""), 3000);
              } catch (err) {
                console.error(err);
                setStatusMessageForNalog(
                  "Došlo je do greške prilikom kreiranja naloga."
                );
                setTimeout(() => setStatusMessageForNalog(""), 3000);
              }
            }}
            className={[
              "mt-4 w-full rounded-lg px-4 py-2 text-sm font-medium transition",
              !datum
                ? "bg-white/10 text-white/40 cursor-not-allowed"
                : "bg-[#FACC15] text-black hover:brightness-110",
            ].join(" ")}
          >
            Kreiraj nalog za izabrani datum
          </button> */}{" "}
          {/*We don't need button anymore, but for future it can stay in comment xD  */}
          {statusMessageForNalog && (
            <div className="fixed inset-0 z-50 flex items-center justify-center">
              {/* BACKDROP */}
              <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" />

              {/* MESSAGE WINDOW */}
              <div
                className="relative z-10 rounded-xl bg-[#1f2937]
                 px-6 py-4 shadow-2xl
                 text-center max-w-sm w-[90%]"
              >
                <div className="text-white text-md">
                  {statusMessageForNalog}
                </div>
              </div>
            </div>
          )}
        </div>
        {/* <ProsutoKantaForm idNaloga={idNaloga} /> */}
        <SaveDailyReportStates idNaloga={idNaloga} />

        {/* <p className="mt-3 text-center text-white/60 text-sm">
          Izabrani datum: <span className="text-white">{datum || "-"}</span>
        </p> */}
      </div>
      <DailyReportPreview datum={datum} onidNalogaResolved={setIdNaloga} />
    </div>
  );
}

export default DailyReports;
