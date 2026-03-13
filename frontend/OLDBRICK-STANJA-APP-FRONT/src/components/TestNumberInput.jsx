import { useState } from "react";

export default function TestNumberInput() {
  const [value, setValue] = useState("");

  return (
    <div style={{ padding: 20 }}>
      <input
        type="number"
        value={value}
        onChange={(e) => {
          console.log("raw:", e.target.value);
          setValue(e.target.value);
        }}
        style={{ fontSize: 18, padding: 10 }}
      />

      <p>Value: {value}</p>
    </div>
  );
}
