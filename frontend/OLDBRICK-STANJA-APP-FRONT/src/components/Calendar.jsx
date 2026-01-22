import { getAllReportDates, getInventoryResetDates } from "../api/helpers";
import { useMemo, useState, useEffect } from "react";
import { ClipboardCheck, Check } from "lucide-react";

function pad2(n) {
  return String(n).padStart(2, "0");
}

function toISODate(d) {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

function parseISO(value) {
  // value: "YYYY-MM-DD"
  if (!value) return null;
  const [y, m, d] = value.split("-").map(Number);
  if (!y || !m || !d) return null;
  return new Date(y, m - 1, d);
}

function startOfMonth(date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function daysInMonth(date) {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
}

// Monday-first index: Mon=0 ... Sun=6
function mondayIndex(jsDay) {
  return (jsDay + 6) % 7;
}

function Calendar({ value, onChange, label = "Datum", calendarPatch }) {
  const selected = useMemo(() => parseISO(value), [value]);

  const [viewDate, setViewDate] = useState(() => selected ?? new Date());
  const [markedMap, setMarkedMap] = useState(new Map());
  const [inventoryResetSet, setInventoryResetSet] = useState(new Set());

  useEffect(() => {
    console.log("CALENDAR PATCH:", calendarPatch);
  }, [calendarPatch]);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const dates = await getAllReportDates();

        const map = new Map();
        for (const d of dates) {
          map.set(d.datum, d.idNaloga);
        }

        if (!cancelled) setMarkedMap(map);
      } catch (e) {
        console.error("Failed to load report dates:", e);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const res = await getInventoryResetDates();

        const set = new Set(res.map((x) => x.datumPopisa.slice(0, 10)));

        if (!cancelled) setInventoryResetSet(set);
      } catch (e) {
        console.error("Failed to load inventory reset dates:", e);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (selected) setViewDate(selected);
  }, [value]);

  useEffect(() => {
    if (!calendarPatch?.type) return;

    setMarkedMap((prev) => {
      const next = new Map(prev);

      // ADD: imamo datum + idNaloga
      if (
        calendarPatch.type === "add" &&
        calendarPatch.datum &&
        calendarPatch.idNaloga
      ) {
        next.set(calendarPatch.datum, calendarPatch.idNaloga);
        return next;
      }

      // REMOVE: ako imamo datum, brisemo direktno
      if (calendarPatch.type === "remove" && calendarPatch.datum) {
        next.delete(calendarPatch.datum);
        return next;
      }

      // REMOVE: ako nemamo datum, ali imamo idNaloga -> nadjii datum i obrisi
      if (calendarPatch.type === "remove" && calendarPatch.idNaloga) {
        for (const [datumKey, nalogId] of next.entries()) {
          if (nalogId === calendarPatch.idNaloga) {
            next.delete(datumKey);
            break;
          }
        }
        return next;
      }

      return next;
    });
  }, [calendarPatch]);

  const monthTitle = useMemo(() => {
    return viewDate.toLocaleString("sr-RS", { month: "long", year: "numeric" });
  }, [viewDate]);

  const grid = useMemo(() => {
    const first = startOfMonth(viewDate);
    const total = daysInMonth(viewDate);
    const leading = mondayIndex(first.getDay());

    const days = [];

    // leading blanks from previous month
    for (let i = 0; i < leading; i++) days.push(null);

    // current month days
    for (let day = 1; day <= total; day++) {
      days.push(new Date(viewDate.getFullYear(), viewDate.getMonth(), day));
    }

    // trailing blanks to fill 6 rows (42 cells)
    while (days.length < 42) days.push(null);

    return days;
  }, [viewDate]);

  const weekdays = ["Pon", "Uto", "Sre", "Čet", "Pet", "Sub", "Ned"];

  const goPrevMonth = () =>
    setViewDate((d) => new Date(d.getFullYear(), d.getMonth() - 1, 1));

  const goNextMonth = () =>
    setViewDate((d) => new Date(d.getFullYear(), d.getMonth() + 1, 1));

  const goToday = () => {
    const t = new Date();
    setViewDate(new Date(t.getFullYear(), t.getMonth(), 1));
    onChange(toISODate(t));
  };

  const selectedISO = selected ? toISODate(selected) : null;

  return (
    <div className="w-full max-w-lg mx-auto">
      <div className="mb-2 text-sm text-white/70">{label}</div>

      <div className="rounded-2xl border border-white/10 bg-white/5 p-3 sm:p-4">
        {/* Header */}
        <div className="flex items-center justify-between gap-2">
          <button
            type="button"
            onClick={goPrevMonth}
            className="rounded-lg px-3 py-2 text-white/80 hover:bg-white/10 transition"
            aria-label="Prethodni mesec"
          >
            ←
          </button>

          <div className="flex flex-col items-center">
            <div className="text-white font-semibold capitalize">
              {monthTitle}
            </div>
            <button
              type="button"
              onClick={goToday}
              className="mt-1 text-xs text-[#FACC15] hover:underline"
            >
              Danas
            </button>
          </div>

          <button
            type="button"
            onClick={goNextMonth}
            className="rounded-lg px-3 py-2 text-white/80 hover:bg-white/10 transition"
            aria-label="Sledeći mesec"
          >
            →
          </button>
        </div>

        {/* Weekdays */}
        <div className="mt-3 grid grid-cols-7 gap-1 text-xs text-white/60">
          {weekdays.map((w) => (
            <div key={w} className="text-center py-1">
              {w}
            </div>
          ))}
        </div>

        {/* Grid */}
        <div className="mt-2 grid grid-cols-7 gap-1">
          {grid.map((d, idx) => {
            const isEmpty = !d;
            const iso = d ? toISODate(d) : null;
            const isSelected = iso && selectedISO === iso;
            const isToday = d && toISODate(d) === toISODate(new Date());
            const isMarked = iso && markedMap.has(iso);
            const isInventoryReset = iso && inventoryResetSet.has(iso);

            return (
              <button
                key={idx}
                type="button"
                disabled={isEmpty}
                onClick={() => iso && onChange(iso)}
                className={[
                  "relative w-full h-12 sm:h-14 rounded-xl text-base sm:text-lg transition",

                  isEmpty
                    ? "opacity-0 cursor-default"
                    : "hover:bg-white/15 text-white/90",
                  isSelected
                    ? "bg-[#FACC15] text-black font-semibold hover:bg-[#FACC15]"
                    : "",
                  !isSelected && isToday ? "ring-1 ring-[#FACC15]/50" : "",
                  isInventoryReset && !isSelected
                    ? "bg-blue-500/70 ring-1 ring-blue-400/50"
                    : "",
                  isMarked && !isSelected && !isInventoryReset
                    ? "bg-green-500/70 ring-1 ring-emerald-400/40"
                    : "",
                ].join(" ")}
              >
                <span className="inline-flex items-center justify-center w-full h-full">
                  {d ? d.getDate() : ""}
                </span>

                {isMarked && !isSelected && (
                  <Check
                    size={12}
                    className="absolute bottom-1 right-1 text-emerald-200 bg-emerald-500/20 rounded-full p-[1px]"
                  />
                )}
                {isInventoryReset && !isSelected && (
                  <ClipboardCheck
                    size={12}
                    className="absolute top-1 right-1 text-blue-200"
                  />
                )}
              </button>
            );
          })}
        </div>

        {/* Selected value */}
        <div className="mt-3 text-center text-xs text-white/60">
          Izabrani datum: <span className="text-white">{value || "-"}</span>
        </div>
      </div>
    </div>
  );
}

export default Calendar;
