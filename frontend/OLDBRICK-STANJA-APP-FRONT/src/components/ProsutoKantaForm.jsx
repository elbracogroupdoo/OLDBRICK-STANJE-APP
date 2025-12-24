import { useState } from "react";
import { putMeasuredProsuto, calculateProsutoRazlika } from "../api/helpers";

function ProsutoKantaForm({ value, onChange }) {
  return (
    <div className="mt-4">
      <label className="block text-sm font-medium mb-1">PROSUTO (kanta)</label>

      <input
        type="number"
        step="0.05"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-lg border px-3 py-2"
      />
    </div>
  );
}

export default ProsutoKantaForm;
