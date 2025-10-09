import React, { useEffect, useState, useRef, useMemo } from "react";
import { useParams } from "react-router-dom";
import applicationService from "../services/applicationService";
import resumeService from "../services/resumeService";
import jobService from "../services/jobService";
import useAuth from "../auth/useAuth";
import "../styles/jobapplications.css";

export default function JobApplications() {
  const { id } = useParams();
  const jobId = Number(id);
  const { user } = useAuth();

  const [apps, setApps] = useState([]);
  const [loading, setLoading] = useState(true);
  const [pageError, setPageError] = useState(null);
  const [jobTitle, setJobTitle] = useState("");

  const [statusFilter, setStatusFilter] = useState("All");
  const [shortlistedOnly, setShortlistedOnly] = useState(false);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");

  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [selectedApp, setSelectedApp] = useState(null);
  const [datetimeLocal, setDatetimeLocal] = useState("");
  const [modalSaving, setModalSaving] = useState(false);

  const [showReviewModal, setShowReviewModal] = useState(false);
  const [reviewStatus, setReviewStatus] = useState("Shortlisted");
  const [reviewFeedback, setReviewFeedback] = useState("");
  const [reviewSaving, setReviewSaving] = useState(false);

  const resumeUrlCache = useRef({});

  useEffect(() => {
    if (!jobId) return;
    setLoading(true);
    setPageError(null);

    Promise.all([
      jobService.getById(jobId).catch(() => null),
      applicationService.getByJob(jobId).catch(() => null),
    ])
      .then(([job, appsData]) => {
        if (job) setJobTitle(job.title || "");
        setApps(Array.isArray(appsData) ? appsData : []);
      })
      .catch((err) => {
        console.error("Failed to load job applications:", err);
        setPageError("Failed to load applications. See console for details.");
      })
      .finally(() => setLoading(false));
  }, [jobId]);

  useEffect(() => {
    return () => {
      Object.values(resumeUrlCache.current).forEach((entry) => {
        if (entry?.isBlob && entry?.url) {
          try {
            URL.revokeObjectURL(entry.url);
          } catch {}
        }
      });
      resumeUrlCache.current = {};
    };
  }, []);

  const parseYmd = (s) => {
    if (!s) return null;
    const d = new Date(`${s}T00:00:00`);
    return Number.isNaN(d.getTime()) ? null : d;
  };

  const filteredApps = useMemo(() => {
    const from = parseYmd(dateFrom);
    const to = parseYmd(dateTo);
    return apps.filter((a) => {
      if (shortlistedOnly && !a.isShortlisted) return false;
      if (statusFilter !== "All") {
        const s = (a.status || "").toString();
        if (s.toLowerCase() !== statusFilter.toLowerCase()) return false;
      }
      if ((from || to) && a.appliedAt) {
        const ap = new Date(a.appliedAt);
        if (Number.isNaN(ap.getTime())) return false;
        const apDate = new Date(ap.getFullYear(), ap.getMonth(), ap.getDate());
        if (from && apDate < from) return false;
        if (to) {
          const toEnd = new Date(to.getFullYear(), to.getMonth(), to.getDate(), 23, 59, 59, 999);
          if (apDate > toEnd) return false;
        }
      } else if ((from || to) && !a.appliedAt) return false;
      return true;
    });
  }, [apps, statusFilter, shortlistedOnly, dateFrom, dateTo]);

  const updateAppInState = (updated) => {
    setApps((prev) => prev.map((a) => (a.applicationId === updated.applicationId ? updated : a)));
  };

  const toggleShortlist = async (app) => {
    try {
      const dto = {
        isShortlisted: !app.isShortlisted,
        status: !app.isShortlisted ? "Shortlisted" : "Applied",
      };
      const updated = await applicationService.update(app.applicationId, dto);
      updateAppInState(updated);
    } catch (err) {
      console.error("Shortlist failed:", err);
      alert(err?.response?.data?.message || err.message || "Shortlist failed");
    }
  };

  const openScheduleModal = (app) => {
    setSelectedApp(app);
    const iso = app.interviewDate || "";
    if (iso) {
      const dt = new Date(iso);
      const pad = (n) => String(n).padStart(2, "0");
      setDatetimeLocal(
        `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}T${pad(dt.getHours())}:${pad(dt.getMinutes())}`
      );
    } else setDatetimeLocal("");
    setShowScheduleModal(true);
  };
  const closeScheduleModal = () => {
    setShowScheduleModal(false);
    setSelectedApp(null);
    setDatetimeLocal("");
  };

  const saveSchedule = async () => {
    if (!selectedApp) return;
    if (!datetimeLocal) return alert("Please pick a date and time.");

    const selectedTime = new Date(datetimeLocal);
    const now = new Date();
    if (selectedTime <= now) {
      alert("Interview date and time must be in the future!");
      return;
    }

    setModalSaving(true);
    try {
      const iso = selectedTime.toISOString();
      const dto = { interviewDate: iso, status: "Interview", isShortlisted: true };
      const updated = await applicationService.update(selectedApp.applicationId, dto);
      updateAppInState(updated);
      closeScheduleModal();
    } catch (err) {
      console.error("Schedule failed:", err);
      alert("Failed to schedule interview");
    } finally {
      setModalSaving(false);
    }
  };

  const openReviewModal = (app) => {
    setSelectedApp(app);
    setReviewStatus(app.status || "Shortlisted");
    setReviewFeedback(app.employerFeedback || "");
    setShowReviewModal(true);
  };
  const closeReviewModal = () => {
    setShowReviewModal(false);
    setSelectedApp(null);
    setReviewStatus("Shortlisted");
    setReviewFeedback("");
  };

  const saveReview = async () => {
    if (!selectedApp) return;
    setReviewSaving(true);
    try {
      const dto = {
        status: reviewStatus,
        employerFeedback: reviewFeedback,
        isShortlisted: reviewStatus === "Shortlisted",
      };
      const updated = await applicationService.update(selectedApp.applicationId, dto);
      updateAppInState(updated);
      closeReviewModal();
    } catch (err) {
      console.error("Review failed:", err);
      alert("Failed to save review");
    } finally {
      setReviewSaving(false);
    }
  };

  const handleDelete = async (app) => {
    if (!window.confirm("Delete this application?")) return;
    try {
      await applicationService.remove(app.applicationId);
      setApps((prev) => prev.filter((a) => a.applicationId !== app.applicationId));
    } catch (err) {
      console.error("Delete failed:", err);
      alert("Failed to delete");
    }
  };

  const handleView = async (resumeId) => {
    try {
      const cached = resumeUrlCache.current[resumeId];
      if (cached) return window.open(cached.url, "_blank");
      const entry = await resumeService.getViewUrl(resumeId);
      resumeUrlCache.current[resumeId] = entry;
      window.open(entry.url, "_blank");
    } catch {
      alert("Resume not available");
    }
  };

  if (!user) return <div className="container mt-4">Login required</div>;
  if (user.role !== "Employer") return <div className="container mt-4">Only Employers can view this page</div>;

  return (
    <div className="container mt-4 job-apps-page">
      <div className="page-header">
        <h3 className="mb-0">
          Applications for {jobTitle ? <span className="text-primary">“{jobTitle}”</span> : `Job #${jobId}`}
        </h3>
        <div className="muted small">Review applicants, schedule interviews and leave feedback.</div>
      </div>

      <div className="filter-bar card p-2 mt-3">
        <div className="filter-row">
          <div className="filter-item">
            <label className="filter-label">Status</label>
            <select className="form-select" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="All">All</option>
              <option>Applied</option>
              <option>Shortlisted</option>
              <option>Interview</option>
              <option>Hired</option>
              <option>Rejected</option>
            </select>
          </div>

          <div className="filter-item compact">
            <label className="filter-label">Shortlisted</label>
            <div className="form-check form-switch">
              <input
                className="form-check-input"
                type="checkbox"
                id="shortlistedOnly"
                checked={shortlistedOnly}
                onChange={(e) => setShortlistedOnly(e.target.checked)}
              />
              <label className="form-check-label" htmlFor="shortlistedOnly">
                {shortlistedOnly ? "On" : "Off"}
              </label>
            </div>
          </div>

          <div className="filter-item">
            <label className="filter-label">From</label>
            <input type="date" className="form-control" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
          </div>

          <div className="filter-item">
            <label className="filter-label">To</label>
            <input type="date" className="form-control" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
          </div>

          <div className="filter-item actions-right">
            <label className="filter-label">&nbsp;</label>
            <div>
              <button
                className="btn btn-outline-secondary btn-sm me-2"
                onClick={() => {
                  setStatusFilter("All");
                  setShortlistedOnly(false);
                  setDateFrom("");
                  setDateTo("");
                }}
              >
                Reset
              </button>
              <div className="small muted">Showing {filteredApps.length} / {apps.length}</div>
            </div>
          </div>
        </div>
      </div>

      {loading ? (
        <div className="loading mt-3">Loading applications…</div>
      ) : filteredApps.length === 0 ? (
        <div className="card empty p-4 mt-3">No applications found.</div>
      ) : (
        <div className="applications-list mt-3">
          {filteredApps.map((a) => (
            <div key={a.applicationId} className="app-card card" data-role="app-card">
              <div className="app-left">
                <div className="app-title">
                  <span className="app-name">{a.jobSeekerName || "Candidate"}</span>
                  <span className="resume-id muted">
                    — Resume (
                    <button className="btn btn-link p-0 small" style={{ textDecoration: "underline" }} onClick={() => handleView(a.resumeId)}>
                      View
                    </button>
                    )
                  </span>
                </div>

                <div className="app-sub muted">Applied {a.appliedAt ? new Date(a.appliedAt).toLocaleDateString() : "—"}</div>

                {a.employerFeedback && <div className="app-feedback">Feedback: {a.employerFeedback}</div>}

                {a.interviewDate && <div className="app-interview">Interview: {new Date(a.interviewDate).toLocaleString()}</div>}
              </div>

              <div className="app-right">
                <div className="badges">
                  <span className={`badge status ${a.status ? a.status.toLowerCase() : ""}`}>{a.status || "—"}</span>
                  {a.isShortlisted && a.status !== "Shortlisted" && <span className="badge shortlisted">Shortlisted</span>}
                </div>

                <div className="actions">
                  <button className={`btn btn-sm ${a.isShortlisted ? "btn-primary" : "btn-outline-primary"}`} onClick={() => toggleShortlist(a)}>
                    {a.isShortlisted ? "Unshortlist" : "Shortlist"}
                  </button>

                  <button className="btn btn-sm btn-warning" onClick={() => openScheduleModal(a)}>
                    {a.interviewDate ? "Reschedule" : "Schedule"}
                  </button>

                  <button className="btn btn-sm btn-secondary" onClick={() => openReviewModal(a)}>
                    Review
                  </button>

                  <button className="btn btn-sm btn-danger" onClick={() => handleDelete(a)}>
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {showScheduleModal && (
        <div className="modal-backdrop">
          <div className="modal-card">
            <h5>Schedule Interview — {selectedApp?.jobSeekerName}</h5>
            <label className="form-label">Date & Time (Local)</label>
            <input type="datetime-local" className="form-control" value={datetimeLocal} onChange={(e) => setDatetimeLocal(e.target.value)} />
            <div className="modal-actions">
              <button className="btn btn-outline-secondary" onClick={closeScheduleModal} disabled={modalSaving}>
                Cancel
              </button>
              <button className="btn btn-primary" onClick={saveSchedule} disabled={modalSaving}>
                {modalSaving ? "Saving…" : "Save"}
              </button>
            </div>
          </div>
        </div>
      )}

      {showReviewModal && (
        <div className="modal-backdrop">
          <div className="modal-card">
            <h5>Review Application — {selectedApp?.jobSeekerName}</h5>
            <label className="form-label">Status</label>
            <select className="form-select" value={reviewStatus} onChange={(e) => setReviewStatus(e.target.value)}>
              <option>Shortlisted</option>
              <option>Interview</option>
              <option>Hired</option>
              <option>Rejected</option>
            </select>

            <label className="form-label mt-3">Feedback</label>
            <textarea className="form-control" rows={4} value={reviewFeedback} onChange={(e) => setReviewFeedback(e.target.value)} />

            <div className="modal-actions">
              <button className="btn btn-outline-secondary" onClick={closeReviewModal} disabled={reviewSaving}>
                Cancel
              </button>
              <button className="btn btn-primary" onClick={saveReview} disabled={reviewSaving}>
                {reviewSaving ? "Saving…" : "Save"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
