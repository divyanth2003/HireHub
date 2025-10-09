import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import useAuth from "../auth/useAuth";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState("");
  const { login } = useAuth();
  const nav = useNavigate();

  const submit = async (e) => {
    e.preventDefault();
    try {
      await login({ email, password });
      nav("/", { replace: true });
    } catch (error) {
      setErr(error.response?.data?.message || "Login failed");
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center vh-100 bg-light">
      <div className="card shadow-lg border-0 p-4" style={{ maxWidth: "420px", width: "100%" }}>
        <div className="text-center mb-4">
          <img
            src="hirehub-logo.png"
            alt="HireHub"
            style={{ width: "60px", height: "60px" }}
            className="mb-2"
          />
          <h3 className="fw-bold text-primary">Welcome Back</h3>
          <p className="text-muted">Login to continue your journey</p>
        </div>

        {err && <div className="alert alert-danger">{err}</div>}

        <form onSubmit={submit}>
          <div className="mb-3">
            <label className="form-label fw-semibold">Email</label>
            <input
              required
              type="email"
              className="form-control form-control-lg"
              placeholder="Enter your email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div className="mb-3">
            <label className="form-label fw-semibold">Password</label>
            <input
              required
              type="password"
              className="form-control form-control-lg"
              placeholder="Enter your password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>

          <button className="btn btn-primary btn-lg w-100 mt-2" type="submit">
            Login
          </button>
        </form>

        <div className="text-center mt-4">
          <p className="mb-1">
            Don’t have an account?{" "}
            <a href="/register" className="text-decoration-none fw-semibold">
              Register here
            </a>
          </p>
          <a href="/forgot-password" className="text-muted small">
            Forgot your password?
          </a>
        </div>
      </div>
    </div>
  );
}
