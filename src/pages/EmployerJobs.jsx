
import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import jobService from "../services/jobService";
import useAuth from "../auth/useAuth";
import "../styles/employerjobs.css";

export default function EmployerJobs() {
  const { user } = useAuth();
  const employerId = user?.employerId;
  const navigate = useNavigate();

  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [savingId, setSavingId] = useState(null);
  const [deletingId, setDeletingId] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!employerId) {
      setLoading(false);
      return;
    }
    setLoading(true);
    jobService
      .getByEmployer(employerId)
      .then((data) => setJobs(Array.isArray(data) ? data : []))
      .catch((err) => {
        console.error("Failed to load employer jobs:", err);
        setError("Failed to load your jobs. See console for details.");
      })
      .finally(() => setLoading(false));
  }, [employerId]);

  const toggleStatus = async (job) => {
    const newStatus = job.status === "Open" ? "Closed" : "Open";

    
    const dto = {
      title: job.title,
      description: job.description,
      location: job.location,
      salary: job.salary,
      skillsRequired: job.skillsRequired,
      academicEligibility: job.academicEligibility,
      allowedBatches: job.allowedBatches,
      backlogs: job.backlogs,
      status: newStatus,
    };

    setSavingId(job.jobId);
    try {
      const updated = await jobService.update(job.jobId, dto);
      setJobs((prev) => prev.map((j) => (j.jobId === updated.jobId ? updated : j)));
    } catch (err) {
      console.error("Status update failed:", err);
      alert("Status update failed: " + (err?.response?.data?.message || err?.message));
    } finally {
      setSavingId(null);
    }
  };

  const handleDelete = async (job) => {
    if (!window.confirm("Delete this job and all related data? This cannot be undone.")) return;
    setDeletingId(job.jobId);
    try {
      await jobService.remove(job.jobId);
      setJobs((prev) => prev.filter((j) => j.jobId !== job.jobId));
      alert("Job deleted");
    } catch (err) {
      console.error("Delete failed:", err);
      alert("Delete failed: " + (err?.response?.data?.message || err?.message));
    } finally {
      setDeletingId(null);
    }
  };

  if (!user) return <div className="container mt-4">Login required</div>;
  if (user.role !== "Employer") return <div className="container mt-4">Only Employers can view this page</div>;

  return (
    <div className="container mt-4 employer-jobs-page">
      <div className="page-head d-flex justify-content-between align-items-center">
        <div>
          <h2 className="mb-0">My Jobs</h2>
          <div className="muted">Manage your posted jobs and review applicants</div>
        </div>

        <div>
          <button className="btn btn-primary" onClick={() => navigate("/jobs/create")}>Post new job</button>
        </div>
      </div>

      {loading ? (
        <div className="loading-card">Loading…</div>
      ) : error ? (
        <div className="alert alert-danger mt-3">{error}</div>
      ) : jobs.length === 0 ? (
        <div className="card empty-card mt-3 p-4">
          <div className="mb-2">You don't have any jobs yet.</div>
          <div>
            <button className="btn btn-primary" onClick={() => navigate("/jobs/create")}>Create your first job</button>
          </div>
        </div>
      ) : (
        <div className="jobs-grid mt-3">
          {jobs.map((job) => (
            <article key={job.jobId} className="job-card">
              <div className="job-left">
                <div className="job-title">{job.title}</div>
                <div className="job-sub muted">
                  {job.location || "—"} • {job.salary ? `₹${job.salary}` : "—"}
                </div>
                <div className="job-meta small muted">Posted: {job.postedAt ? new Date(job.postedAt).toLocaleDateString() : "—"}</div>
              </div>

              <div className="job-right">
                <div className="status-row">
                  <span className={`status-badge ${statusToClass(job.status)}`}>{job.status || "Unknown"}</span>
                </div>

                <div className="action-row">
                  <button className="btn btn-sm btn-outline-secondary" onClick={() => navigate(`/jobs/${job.jobId}`)}>View</button>

                  <button className="btn btn-sm btn-outline-primary ms-2" onClick={() => navigate(`/jobs/edit/${job.jobId}`)}>Edit</button>

                  <button
                    className={`btn btn-sm ms-2 ${job.status === "Open" ? "btn-warning" : "btn-success"}`}
                    onClick={() => toggleStatus(job)}
                    disabled={savingId === job.jobId}
                  >
                    {savingId === job.jobId ? "Saving..." : job.status === "Open" ? "Close" : "Open"}
                  </button>

                  <button className="btn btn-sm btn-outline-info ms-2" onClick={() => navigate(`/jobs/${job.jobId}/applications`)}>
                    Applicants
                  </button>

                  <button className="btn btn-sm btn-danger ms-2" onClick={() => handleDelete(job)} disabled={deletingId === job.jobId}>
                    {deletingId === job.jobId ? "Deleting..." : "Delete"}
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
  if (s.includes("open")) return "open";
  if (s.includes("close") || s.includes("closed")) return "closed";
  if (s.includes("draft")) return "draft";
  return "unknown";
}
