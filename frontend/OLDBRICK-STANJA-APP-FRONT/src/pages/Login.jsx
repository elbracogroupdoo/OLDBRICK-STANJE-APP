import { useState } from "react";
import { login } from "../api/authApi";
import { useNavigate } from "react-router-dom";

function Login() {
  var [username, setUsername] = useState("");
  var [password, setPassword] = useState("");

  var [isLoading, setIsLoading] = useState(false);
  var [error, setError] = useState("");

  var navigate = useNavigate();

  async function handleSubmit(e) {
    e.preventDefault();

    setError("");
    setIsLoading(true);

    try {
      await login({ username: username, password: password });
      navigate("/");
    } catch (err) {
      var message =
        (err &&
          err.response &&
          err.response.data &&
          (err.response.data.errorMessage ||
            err.response.data.message ||
            err.response.data.title)) ||
        (err && err.message) ||
        "Login failed.";

      setError(message);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="mx-auto mt-16 max-w-md rounded-xl border border-gray-200 p-6 shadow-sm">
      <h2 className="mb-4 text-2xl font-semibold">Login</h2>

      {error ? (
        <div className="mb-3 rounded-lg border border-red-400 bg-red-50 p-2 text-sm text-red-700">
          {error}
        </div>
      ) : null}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700">
            Username
          </label>
          <input
            value={username}
            onChange={function (e) {
              setUsername(e.target.value);
            }}
            disabled={isLoading}
            autoComplete="username"
            className="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700">
            Password
          </label>
          <input
            type="password"
            value={password}
            onChange={function (e) {
              setPassword(e.target.value);
            }}
            disabled={isLoading}
            autoComplete="current-password"
            className="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
          />
        </div>

        <button
          type="submit"
          disabled={isLoading || !username || !password}
          className="w-full rounded-lg bg-blue-600 px-4 py-2 font-medium text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-gray-400"
        >
          {isLoading ? "Logging in..." : "Login"}
        </button>
      </form>
    </div>
  );
}

export default Login;
