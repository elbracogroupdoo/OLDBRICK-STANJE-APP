import { useEffect, useState } from "react";
import { getReportStatesById } from "../api/helpers";

function DailyReportPreview() {
  const [data, setData] = useState(null);

  useEffect(() => {
    getReportStatesById(8).then(setData).catch(console.error);
  }, []);

  if (!data) {
    return <div>Loading...</div>;
  }

  return (
    <div className="mt-6">
      <h3 className="text-center text-sm text-gray-300 mb-3">
        Total prosuto:{" "}
        <span className="font-semibold">{data.totalProsuto}</span>
      </h3>

      {/* ===== MOBILE (kartice) ===== */}
      <div className="md:hidden space-y-3">
        {data.items.map((x) => (
          <div
            key={x.idPiva}
            className="rounded-lg border border-white/10 bg-white/5 p-3"
          >
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm font-semibold">Pivo {x.idPiva}</div>
              <div
                className={`text-sm font-semibold ${
                  x.odstupanje === 0
                    ? "text-gray-400"
                    : x.odstupanje > 0
                    ? "text-yellow-400"
                    : "text-red-400"
                }`}
              >
                Odst: {x.odstupanje}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-2 text-sm">
              <div className="rounded-md bg-black/20 p-2">
                <div className="text-xs text-gray-400">Vaga start</div>
                <div className="font-medium">{x.vagaStart}</div>
              </div>

              <div className="rounded-md bg-black/20 p-2">
                <div className="text-xs text-gray-400">Vaga end</div>
                <div className="font-medium">{x.vagaEnd}</div>
              </div>

              <div className="rounded-md bg-black/20 p-2">
                <div className="text-xs text-gray-400">Vaga potrošnja</div>
                <div className="font-semibold">{x.vagaPotrosnja}</div>
              </div>

              <div className="rounded-md bg-black/20 p-2">
                <div className="text-xs text-gray-400">POS potrošnja</div>
                <div className="font-semibold">{x.posPotrosnja}</div>
              </div>

              <div className="rounded-md bg-black/20 p-2">
                <div className="text-xs text-gray-400">POS start</div>
                <div className="font-medium">{x.posStart}</div>
              </div>

              <div className="rounded-md bg-black/20 p-2">
                <div className="text-xs text-gray-400">POS end</div>
                <div className="font-medium">{x.posEnd}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* ===== DESKTOP / TABLET (tabela) ===== */}
      <div className="hidden md:block mt-4 rounded-lg border border-white/10">
        <table className="w-full border-collapse text-sm text-gray-200">
          <thead className="bg-white/5">
            <tr>
              <th className="px-3 py-2 text-left">Pivo</th>
              <th className="px-3 py-2 text-right">Vaga start</th>
              <th className="px-3 py-2 text-right">Vaga end</th>
              <th className="px-3 py-2 text-right">Vaga pot.</th>
              <th className="px-3 py-2 text-right">POS start</th>
              <th className="px-3 py-2 text-right">POS end</th>
              <th className="px-3 py-2 text-right">POS pot.</th>
              <th className="px-3 py-2 text-right">Odst.</th>
            </tr>
          </thead>

          <tbody>
            {data.items.map((x) => (
              <tr
                key={x.idPiva}
                className="border-t border-white/10 hover:bg-white/5 transition"
              >
                <td className="px-3 py-2 font-medium">{x.idPiva}</td>
                <td className="px-3 py-2 text-right">{x.vagaStart}</td>
                <td className="px-3 py-2 text-right">{x.vagaEnd}</td>
                <td className="px-3 py-2 text-right">{x.vagaPotrosnja}</td>
                <td className="px-3 py-2 text-right">{x.posStart}</td>
                <td className="px-3 py-2 text-right">{x.posEnd}</td>
                <td className="px-3 py-2 text-right">{x.posPotrosnja}</td>
                <td className="px-3 py-2 text-right font-semibold">
                  {x.odstupanje}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default DailyReportPreview;
