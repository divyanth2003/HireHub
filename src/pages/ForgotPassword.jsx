import React, { useState } from "react";
import { Link } from "react-router-dom";
import api from "../api/api"; 
import "../styles/ForgotPassword.css";

export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    setMessage("");
    setError("");

    if (!email.trim()) {
      setError("Please enter your email address.");
      return;
    }

    setLoading(true);
    try {
    
      await api.post("/User/forgot-password", {
        email,
        originBaseUrl: window.location.origin,
      });

      setMessage(
        "If this email is registered, password reset instructions have been sent to your inbox."
      );
      setEmail("");
    } catch (err) {
      console.error("Forgot password error:", err);
      const msg =
        err.response?.data?.message ||
        err.response?.data?.error ||
        "Something went wrong. Please try again.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="forgot-container">
      <div className="forgot-card shadow-sm">
        <div className="text-center mb-3">
          <img src="hirehub-logo.png" alt="HireHub" className="forgot-logo" />
          <h2 className="fw-bold">Forgot Password</h2>
          <p className="text-muted">
            Enter your registered email to reset your password
          </p>
        </div>

        {/* Alerts */}
        {error && <div className="alert alert-danger">{error}</div>}
        {message && <div className="alert alert-success">{message}</div>}

        {/* Form */}
        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label">Email address</label>
            <input
              type="email"
              className="form-control"
              placeholder="you@company.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
            />
          </div>

          <button
            className="btn btn-primary w-100"
            type="submit"
            disabled={loading}
          >
            {loading ? "Sending..." : "Send Reset Link"}
          </button>
        </form>

        <div className="mt-3 text-center small">
          Remembered your password? <Link to="/login">Back to Login</Link>
        </div>
      </div>
    </div>
  );
}
