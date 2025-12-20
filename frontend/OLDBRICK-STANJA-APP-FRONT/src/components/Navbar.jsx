import { useEffect, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useLocation } from "react-router-dom";

function Navbar() {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();

  const [hasToken, setHasToken] = useState(
    () => !!localStorage.getItem("token")
  );

  const location = useLocation();
  const isAuthActive = location.pathname === "/login";

  useEffect(() => {
    setHasToken(!!localStorage.getItem("token"));
  }, [location.pathname]);

  useEffect(() => {
    const syncAuth = () => setHasToken(!!localStorage.getItem("token"));
    window.addEventListener("auth-changed", syncAuth);
    return () => window.removeEventListener("auth-changed", syncAuth);
  }, []);

  const handleAuthClick = () => {
    const token = localStorage.getItem("token");

    if (!token) {
      navigate("/login");
      return;
    }

    localStorage.removeItem("token");
    window.dispatchEvent(new Event("auth-changed"));
    navigate("/login");
  };

  useEffect(() => {
    document.body.style.overflow = open ? "hidden" : "";
    return () => (document.body.style.overflow = "");
  }, [open]);

  return (
    <header className="fixed top-0 left-0 w-full z-50 bg-[#2A2F36] border-b border-white/10 text-white">
      <nav className="flex w-full items-center justify-between px-4 py-3">
        {/* Brand */}
        <NavLink to="/" className="font-semibold tracking-tight text-[#FACC15]">
          Oldbrick • Stanja
        </NavLink>

        {/* Desktop links */}
        <div className="hidden items-center gap-2 md:flex">
          <NavLink
            to="/"
            className={({ isActive }) =>
              [
                "rounded-lg px-3 py-2 text-sm transition hover:bg-white/10",
                isActive
                  ? "bg-white/10 text-[#FACC15] font-medium"
                  : "text-white/80",
              ].join(" ")
            }
          >
            Početna
          </NavLink>

          <NavLink
            to="/unesi"
            className={({ isActive }) =>
              [
                "rounded-lg px-3 py-2 text-sm transition hover:bg-white/10",
                isActive
                  ? "bg-white/10 text-[#FACC15] font-medium"
                  : "text-white/80",
              ].join(" ")
            }
          >
            Unesi stanje
          </NavLink>

          {/* AUTH */}
          <button
            type="button"
            onClick={handleAuthClick}
            className={[
              "rounded-lg px-3 py-2 text-sm transition hover:bg-white/10 cursor-pointer",
              isAuthActive
                ? "bg-white/10 text-[#FACC15] font-medium"
                : "text-white/80",
            ].join(" ")}
          >
            {hasToken ? "Odjavi se" : "Prijavljivanje"}
          </button>
        </div>

        {/* Mobile hamburger */}
        <button
          type="button"
          className="md:hidden rounded-lg p-2 hover:bg-white/10 cursor-pointer"
          aria-label="Open menu"
          aria-expanded={open}
          onClick={() => setOpen(true)}
        >
          <span className="block h-0.5 w-6 bg-white/90 mb-1.5"></span>
          <span className="block h-0.5 w-6 bg-white/90 mb-1.5"></span>
          <span className="block h-0.5 w-6 bg-white/90"></span>
        </button>
      </nav>

      {/* Mobile menu */}
      <div
        className={[
          "fixed inset-0  md:hidden",
          open ? "pointer-events-auto" : "pointer-events-none",
        ].join(" ")}
      >
        {/* Backdrop */}
        <div
          className={[
            "absolute inset-0 bg-black/40 backdrop-blur-sm transition-opacity duration-300",
            open ? "opacity-100" : "opacity-0",
          ].join(" ")}
          onClick={() => setOpen(false)}
        />

        {/* Slide-down panel */}
        <div
          className={[
            "absolute left-0 top-0 w-full bg-[#2A2F36] shadow-2xl border-b border-white/10",
            "transition-transform duration-300 ease-out",
            open ? "translate-y-0" : "-translate-y-full",
          ].join(" ")}
        >
          <div className="flex items-center justify-between border-b border-white/10 px-4 py-3 text-white">
            <span className="font-semibold text-[#FACC15]">Meni</span>
            <button
              onClick={() => setOpen(false)}
              className="rounded-lg p-2 hover:bg-white/10 cursor-pointer"
            >
              ✕
            </button>
          </div>

          {/* Links */}
          <div className="flex flex-col p-4 gap-2">
            <NavLink
              to="/"
              onClick={() => setOpen(false)}
              className={({ isActive }) =>
                [
                  "rounded-xl px-4 py-4 text-base transition hover:bg-white/10",
                  isActive
                    ? "bg-white/10 text-[#FACC15] font-medium"
                    : "text-white/80",
                ].join(" ")
              }
            >
              Početna
            </NavLink>

            <NavLink
              to="/daily-reports"
              onClick={() => setOpen(false)}
              className={({ isActive }) =>
                [
                  "rounded-xl px-4 py-4 text-base transition hover:bg-white/10",
                  isActive
                    ? "bg-white/10 text-[#FACC15] font-medium"
                    : "text-white/80",
                ].join(" ")
              }
            >
              Unesi stanje
            </NavLink>

            {/* AUTH */}
            <button
              type="button"
              onClick={() => {
                setOpen(false);
                handleAuthClick();
              }}
              className={[
                "rounded-xl px-4 py-4 text-base transition hover:bg-white/10 cursor-pointer",
                isAuthActive
                  ? "bg-white/10 text-[#FACC15] font-medium"
                  : "text-white/80",
              ].join(" ")}
            >
              {hasToken ? "Odjavi se" : "Prijavljivanje"}
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}

export default Navbar;
