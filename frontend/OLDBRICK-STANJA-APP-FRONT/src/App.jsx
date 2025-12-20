import { useState } from "react";
import "./App.css";
import { Routes, Route } from "react-router-dom";
import ProtectedRoute from "./components/ProtectedRoute.jsx";
import Home from "./pages/Home.jsx";
import Login from "./pages/Login.jsx";
import Navbar from "./components/Navbar.jsx";
import DailyReportPreview from "./components/DailyReportPreview.jsx";
import DailyReports from "./pages/DailyReports.jsx";
import DayBeforeReports from "./pages/DayBeforeReports.jsx";

function App() {
  return (
    <div className="min-h-screen bg-[#20242B] text-slate-100">
      <Navbar />
      <Routes>
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <Home />
            </ProtectedRoute>
          }
        />
        <Route path="/login" element={<Login />} />
        <Route
          path="/daily-report/:idNaloga"
          element={<DailyReportPreview />}
        />
        <Route path="/daybefore-report" element={<DayBeforeReports />} />
        <Route path="/daily-reports" element={<DailyReports />} />
      </Routes>
    </div>
  );
}

export default App;
