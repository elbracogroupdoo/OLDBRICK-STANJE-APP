function ReportDetails({ items, totals }) {
  if (!items || items.length === 0) return null;

  console.log("DTO STAVKE:", items);
  console.log("POTROSNJA VAGA I POS:", totals);

  return (
    <div className="mt-6">
      {/* ===== MOBILE (kartice) ===== */}
      <div className="md:hidden space-y-3">
        {items.map((x) => {
          const isKesa = x.tipMerenja === "kesa";

          return (
            <div
              key={x.idPiva}
              className="rounded-lg border border-white/10 bg-white/5 p-3"
            >
              <div className="flex items-center justify-between mb-2">
                <div className="text-sm font-semibold flex items-center gap-2">
                  <span>{x.nazivPiva}</span>
                  <span
                    className={`text-xs px-2 py-0.5 rounded ${
                      isKesa
                        ? "bg-orange-500/20 text-orange-300"
                        : "bg-blue-500/20 text-blue-300"
                    }`}
                  >
                    {isKesa ? "BROJAČ" : "VAGA"}
                  </span>
                </div>

                <div
                  className={`text-sm font-semibold ${
                    x.odstupanje === 0
                      ? "text-yellow-400"
                      : x.odstupanje < 0
                      ? "text-red-400"
                      : "text-green-400"
                  }`}
                >
                  Odst: {Number(x.odstupanje).toFixed(2)}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2 text-sm">
                <div className="rounded-md bg-black/20 p-2">
                  <div className="text-xs text-gray-400">
                    {isKesa ? "BROJAČ start" : "Vaga start"}
                  </div>
                  <div className="font-medium">
                    {Number(x.vagaStart).toFixed(2)}
                  </div>
                </div>

                <div className="rounded-md bg-black/20 p-2">
                  <div className="text-xs text-gray-400">
                    {isKesa ? "BROJAČ end" : "Vaga end"}
                  </div>
                  <div className="font-medium">
                    {Number(x.vagaEnd).toFixed(2)}
                  </div>
                </div>

                <div className="rounded-md bg-black/20 p-2">
                  <div className="text-xs text-gray-400">
                    {isKesa ? "BROJAČ potrošnja" : "Vaga potrošnja"}
                  </div>
                  <div className="font-semibold">
                    {Number(x.vagaPotrosnja).toFixed(2)}
                  </div>
                </div>

                <div className="rounded-md bg-black/20 p-2">
                  <div className="text-xs text-gray-400">POS potrošnja</div>
                  <div className="font-semibold">
                    {Number(x.posPotrosnja).toFixed(2)}
                  </div>
                </div>

                <div className="rounded-md bg-black/20 p-2">
                  <div className="text-xs text-gray-400">POS start</div>
                  <div className="font-medium">
                    {Number(x.posStart).toFixed(2)}
                  </div>
                </div>

                <div className="rounded-md bg-black/20 p-2">
                  <div className="text-xs text-gray-400">POS end</div>
                  <div className="font-medium">
                    {Number(x.posEnd).toFixed(2)}
                  </div>
                </div>
              </div>
            </div>
          );
        })}
        <div className="mt-4 rounded-lg border border-white/10 bg-white/5 p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold text-gray-300">
              UKUPNA POTROŠNJA
            </h3>
            <span className="text-xs text-gray-400">
              Nalog: {totals?.idNaloga ?? "—"}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div className="rounded-md bg-black/20 p-3">
              <div className="text-xs text-gray-400 mb-1">Ukupno VAGA</div>
              <div className="text-lg font-semibold text-blue-300">
                {totals
                  ? Number(totals.totals.totalVagaPotrosnja).toFixed(2)
                  : "—"}
              </div>
            </div>

            <div className="rounded-md bg-black/20 p-3">
              <div className="text-xs text-gray-400 mb-1">Ukupno POS</div>
              <div className="text-lg font-semibold text-green-300">
                {totals
                  ? Number(totals.totals.totalPosPotrosnja).toFixed(2)
                  : "—"}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* ===== DESKTOP / TABLET (tabela) ===== */}
      <div className="hidden md:block mt-4 rounded-lg border border-white/10">
        <table className="w-full border-collapse text-sm text-gray-200">
          <thead className="bg-white/5">
            <tr>
              <th className="px-3 py-2 text-left">Pivo</th>
              <th className="px-3 py-2 text-right">Start</th>
              <th className="px-3 py-2 text-right">End</th>
              <th className="px-3 py-2 text-right">Pot.</th>
              <th className="px-3 py-2 text-right">POS start</th>
              <th className="px-3 py-2 text-right">POS end</th>
              <th className="px-3 py-2 text-right">POS pot.</th>
              <th className="px-3 py-2 text-right">Odst.</th>
            </tr>
          </thead>

          <tbody>
            {items.map((x) => (
              <tr
                key={x.idPiva}
                className="border-t border-white/10 hover:bg-white/5 transition"
              >
                <td className="px-3 py-2 font-medium">{x.nazivPiva}</td>
                <td className="px-3 py-2 text-right">
                  <div className="text-xs text-gray-400">
                    {x.tipMerenja == "kesa" ? "BROJAČ start" : "Vaga start"}
                  </div>
                  <div className="font-medium">
                    {Number(x.vagaStart).toFixed(2)}
                  </div>
                </td>

                <td className="px-3 py-2 text-right">
                  <div className="text-xs text-gray-400">
                    {x.tipMerenja == "kesa" ? "BROJAČ end" : "Vaga end"}
                  </div>
                  <div className="font-medium">
                    {Number(x.vagaEnd).toFixed(2)}
                  </div>
                </td>

                <td className="px-3 py-2 text-right">
                  <div className="text-xs text-gray-400">
                    {x.tipMerenja === "kesa" ? "BROJAČ pot." : "Vaga pot."}
                  </div>
                  <div className="font-semibold">
                    {Number(x.vagaPotrosnja).toFixed(2)}
                  </div>
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="text-xs text-gray-400">Pos start</div>
                  <div className="font-semibold">
                    {Number(x.posStart).toFixed(2)}
                  </div>
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="text-xs text-gray-400">Pos end</div>
                  <div className="font-semibold">
                    {Number(x.posEnd).toFixed(2)}
                  </div>
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="text-xs text-gray-400">Pos pot.</div>
                  <div className="font-semibold">
                    {Number(x.posPotrosnja).toFixed(2)}
                  </div>
                </td>
                <td
                  className={`px-3 py-2 text-right font-semibold ${
                    x.odstupanje === 0
                      ? "text-yellow-400"
                      : x.odstupanje < 0
                      ? "text-red-400"
                      : "text-green-400"
                  }`}
                >
                  {Number(x.odstupanje).toFixed(2)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {/* ===== DESKTOP TOTALS ===== */}
        <div className="border-t border-white/10 bg-white/5 p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold text-gray-300">
              UKUPNA POTROŠNJA
            </h3>
            <span className="text-xs text-gray-400">
              Nalog: {totals?.idNaloga ?? "—"}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div className="rounded-md bg-black/20 p-3">
              <div className="text-xs text-gray-400 mb-1">Ukupno VAGA</div>
              <div className="text-lg font-semibold text-blue-300">
                {totals
                  ? Number(totals.totals.totalVagaPotrosnja).toFixed(2)
                  : "—"}
              </div>
            </div>

            <div className="rounded-md bg-black/20 p-3">
              <div className="text-xs text-gray-400 mb-1">Ukupno POS</div>
              <div className="text-lg font-semibold text-green-300">
                {totals
                  ? Number(totals.totals.totalPosPotrosnja).toFixed(2)
                  : "—"}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ReportDetails;
