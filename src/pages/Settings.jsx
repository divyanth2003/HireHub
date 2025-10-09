import React, { useEffect, useState } from "react";
import useAuth from "../auth/useAuth";
import userService from "../services/userService";
import { useNavigate } from "react-router-dom";

export default function Settings() {
  const { user, logout, setUserLocal } = useAuth();
  const [deactivating, setDeactivating] = useState(false);
  const [reactivating, setReactivating] = useState(false);
  const [permanentDeleting, setPermanentDeleting] = useState(false);
  const [message, setMessage] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!user) navigate("/login");
  }, [user, navigate]);

  const resolveEmail = (u) => {
    if (!u) return "—";
    if (typeof u.email === "string" && u.email.trim() !== "") return u.email;
    if (u.user && typeof u.user.email === "string" && u.user.email.trim() !== "") return u.user.email;
    if (typeof u.userEmail === "string" && u.userEmail.trim() !== "") return u.userEmail;
    if (typeof u.user_email === "string" && u.user_email.trim() !== "") return u.user_email;
    return "—";
  };

  const resolveName = (u) => {
    if (!u) return "—";
    if (typeof u.fullName === "string" && u.fullName.trim() !== "") return u.fullName;
    if (typeof u.userFullName === "string" && u.userFullName.trim() !== "") return u.userFullName;
    if (typeof u.name === "string" && u.name.trim() !== "") return u.name;
    return "—";
  };

  const isActive = (() => {
    if (!user) return false;
    if (typeof user.isActive === "boolean") return user.isActive;
    if (user.deactivatedAt !== undefined && user.deactivatedAt !== null) return false;
    if (user.deactivated_at !== undefined && user.deactivated_at !== null) return false;
    return true;
  })();

  const handleDeactivate = async () => {
    if (!window.confirm("Temporarily deactivate your account? You can reactivate later.")) return;
    setDeactivating(true);
    setMessage(null);
    try {
      const res = await userService.deactivate(user.userId);
      setMessage({ type: "success", text: res?.message || "Account deactivated temporarily." });
      try { setUserLocal({ ...user, isActive: false, deactivatedAt: new Date().toISOString() }); } catch {}
    } catch (err) {
      setMessage({ type: "error", text: err?.response?.data?.message || err?.message || "Deactivate failed" });
    } finally {
      setDeactivating(false);
    }
  };

  const handleReactivate = async () => {
    if (!window.confirm("Reactivate your account?")) return;
    setReactivating(true);
    setMessage(null);
    try {
      const res = await userService.reactivate(user.userId);
      setMessage({ type: "success", text: res?.message || "Account reactivated successfully." });
      try { setUserLocal({ ...user, isActive: true, deactivatedAt: null }); } catch {}
    } catch (err) {
      setMessage({ type: "error", text: err?.response?.data?.message || err?.message || "Reactivate failed" });
    } finally {
      setReactivating(false);
    }
  };

  const handlePermanentDelete = async () => {
    if (!window.confirm("Permanently delete your account? This cannot be undone.")) return;
    setPermanentDeleting(true);
    setMessage(null);
    try {
      const res = await userService.deletePermanently(user.userId);
      setMessage({ type: "success", text: res?.message || "Your account has been deleted successfully." });
      try { logout?.(); localStorage.removeItem("user"); } catch {}
      setTimeout(() => navigate("/"), 800);
    } catch (err) {
      setMessage({ type: "error", text: err?.response?.data?.message || err?.message || "Delete failed" });
    } finally {
      setPermanentDeleting(false);
    }
  };

  if (!user) return <div className="container mt-4">Please login to manage your account.</div>;

  return (
    <div className="container mt-4">
      <h3>Account Settings</h3>

      {message && (
        <div className={`alert ${message.type === "error" ? "alert-danger" : "alert-success"}`} role="alert">
          {message.text}
        </div>
      )}

      <div className="card mb-3">
        <div className="card-body">
          <h5 className="card-title">Account</h5>
          <p className="mb-1"><strong>Name:</strong> {resolveName(user)}</p>
          <p className="mb-1"><strong>Email:</strong> {resolveEmail(user)}</p>
          <p className="mb-0 text-muted">Manage account lifecycle below.</p>
        </div>
      </div>

      <div className="card mb-3">
        <div className="card-body">
          <h5>{isActive ? "Temporarily deactivate account" : "Reactivate account"}</h5>
          <p className="text-muted">{isActive ? "Deactivate your account temporarily. You can reactivate later." : "Your account is currently deactivated. Reactivate to access features again."}</p>
          <div className="d-flex gap-2">
            {isActive ? (
              <button className="btn btn-outline-warning" onClick={handleDeactivate} disabled={deactivating}>
                {deactivating ? "Deactivating..." : "Deactivate account"}
              </button>
            ) : (
              <button className="btn btn-outline-primary" onClick={handleReactivate} disabled={reactivating}>
                {reactivating ? "Reactivating..." : "Reactivate account"}
              </button>
            )}
          </div>
        </div>
      </div>

      <div className="card mb-5">
        <div className="card-body">
          <h5>Permanent deletion</h5>
          <p className="text-muted">Permanently delete your account immediately. This action cannot be undone and will remove all your data.</p>
          <button className="btn btn-danger" onClick={handlePermanentDelete} disabled={permanentDeleting}>
            {permanentDeleting ? "Deleting..." : "Delete account permanently"}
          </button>
        </div>
      </div>
    </div>
  );
}
