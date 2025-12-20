import { useNavigate } from "react-router-dom";

function Home() {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen flex items-center justify-center px-4">
      <div className="w-full max-w-md space-y-3">
        <button
          type="button"
          onClick={() => navigate("/daily-reports")}
          className="w-full rounded-xl px-4 py-3 text-sm font-medium bg-[#2A2F3A] text-white hover:bg-[#343B49] transition"
        >
          UNESI IZVEŠTAJ
        </button>

        <button
          type="button"
          onClick={() => navigate("/daybefore-report")}
          className="w-full rounded-xl px-4 py-3 text-sm font-medium bg-white text-[#2A2F3A] border border-black/10 hover:bg-black/5 transition"
        >
          POGLEDAJ PRETHODNI DAN
        </button>

        <button
          type="button"
          onClick={() => navigate("/pregled/nedelja")}
          className="w-full rounded-xl px-4 py-3 text-sm font-medium bg-white text-[#2A2F3A] border border-black/10 hover:bg-black/5 transition"
        >
          POGLEDAJ PRETHODNU NEDELJU
        </button>
      </div>
    </div>
  );
}
export default Home;
