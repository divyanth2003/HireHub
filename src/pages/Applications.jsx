import React, { useEffect, useState, useRef } from "react";
import applicationService from "../services/applicationService";
import useAuth from "../auth/useAuth";
import "../styles/applications.css";

export default function Applications() {
  const { user } = useAuth();
  const [apps, setApps] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const pollRef = useRef(null);

 
  const [topMessage, setTopMessage] = useState(null);
  const showToast = (text, type = "success", ttl = 3500) => {
    setTopMessage({ text, type });
    window.setTimeout(() => setTopMessage(null), ttl);
  };

  const load = async () => {
    if (!user?.jobSeekerId) return;
    try {
      const data = await applicationService.getByJobSeeker(user.jobSeekerId);
      setApps(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error("Failed to load applications:", err);
      showToast("Failed to load applications.", "error", 4000);
    }
  };

  useEffect(() => {
    if (!user?.jobSeekerId) {
      setLoading(false);
      return;
    }
    setLoading(true);
    load().finally(() => setLoading(false));

    pollRef.current = setInterval(() => {
      load();
    }, 10000);

    return () => {
      if (pollRef.current) clearInterval(pollRef.current);
    };

  }, [user?.jobSeekerId]);

  const handleWithdraw = async (applicationId) => {
  
    if (!window.confirm("Withdraw this application?")) return;

    setBusy(true);
    try {
      await applicationService.remove(applicationId);
      setApps((prev) => prev.filter((a) => a.applicationId !== applicationId));
      showToast("Application withdrawn.", "success");
    } catch (err) {
      console.error("Withdraw failed:", err);
      const msg = err?.response?.data?.message || err?.message || "Withdraw failed";
      showToast(String(msg), "error", 5000);
    } finally {
      setBusy(false);
    }
  };

  if (!user) return <div className="container mt-4">Login required</div>;
  if (user.role !== "JobSeeker") return <div className="container mt-4">Only JobSeekers can view this page.</div>;
  if (loading) return <div className="container mt-4">Loading…</div>;

  return (
    <div className="container mt-4 applications-page">
     
      <div style={{ position: "fixed", top: 12, right: 12, zIndex: 1400 }}>
        {topMessage && (
          <div
            className={`app-toast ${topMessage.type === "success" ? "app-toast-success" : topMessage.type === "error" ? "app-toast-error" : "app-toast-info"}`}
            role="status"
          >
            {topMessage.text}
          </div>
        )}
      </div>

      <div className="apps-header d-flex justify-content-between align-items-center">
        <h3 className="mb-0">My Applications</h3>
        <div className="muted small">Auto-refreshing every 10s</div>
      </div>

      {apps.length === 0 ? (
        <div className="card empty-card mt-3">
          <div className="card-body">
            <p className="mb-0">You haven’t applied to any jobs yet. Browse jobs and apply to start tracking your applications.</p>
          </div>
        </div>
      ) : (
        <div className="list-grid mt-3">
          {apps.map((a) => (
            <article key={a.applicationId} className="app-card" aria-live="polite">
              <div className="app-left">
                <div className="job-avatar" aria-hidden>
                  {(a.employerName || a.jobTitle || "H").slice(0, 1).toUpperCase()}
                </div>
                <div className="job-meta">
                  <div className="job-title">{a.jobTitle}</div>
                  <div className="job-sub small muted">
                    Applied on {a.appliedAt ? new Date(a.appliedAt).toLocaleDateString() : "—"}
                    {a.employerName ? ` • ${a.employerName}` : ""}
                  </div>

                  {a.coverLetter && <div className="cover small">“{a.coverLetter}”</div>}
                  <div className="meta-row">
                    {a.isShortlisted && <span className="pill info">Shortlisted</span>}
                    {a.interviewDate && <span className="pill warn">Interview</span>}
                    {a.employerFeedback && <span className="pill success">Feedback</span>}
                  </div>
                </div>
              </div>

              <div className="app-right">
                <div className="status-wrap">
                  <span className={`status-badge ${statusToClass(a.status)}`}>{a.status || "Unknown"}</span>
                  {a.interviewDate && (
                    <div className="mt-1 small text-warning">
                      Interview: {new Date(a.interviewDate).toLocaleString()}
                    </div>
                  )}
                </div>

                <div className="actions">
                  <button
                    type="button"
                    className="btn btn-sm btn-danger"
                    onClick={() => handleWithdraw(a.applicationId)}
                    disabled={busy}
                  >
                    Withdraw
                  </button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}

function statusToClass(status) {
  if (!status) return "unknown";
  const s = String(status).toLowerCase();
  if (s.includes("applied")) return "applied";
  if (s.includes("shortlist")) return "shortlisted";
  if (s.includes("interview")) return "interview";
  if (s.includes("hired")) return "hired";
  if (s.includes("reject")) return "rejected";
  return "unknown";
}
