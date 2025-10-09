// src/pages/JobDetails.jsx
import React, { useEffect, useState } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import jobService from "../services/jobService";
import applicationService from "../services/applicationService";
import resumeService from "../services/resumeService";
import jobSeekerService from "../services/jobSeekerService";
import useAuth from "../auth/useAuth";
import "../styles/jobdetails.css";

function formatSalary(s) {
  if (!s && s !== 0) return "—";
  const n = Number(s);
  if (Number.isNaN(n)) return s;
  return n >= 1000
    ? n.toLocaleString("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 })
    : `${n}`;
}

function getPostedDate(job) {
  if (!job) return null;
  return job.postedOn ?? job.postedDate ?? job.createdAt ?? job.createdOn ?? null;
}

export default function JobDetails() {
  const { id } = useParams();
  const jobId = Number(id);
  const [job, setJob] = useState(null);
  const [otherJobs, setOtherJobs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [applyLoading, setApplyLoading] = useState(false);
  const [applied, setApplied] = useState(false);
  const [coverLetter, setCoverLetter] = useState("");
  const [inlineApplyOpen, setInlineApplyOpen] = useState(false);
  const [defaultResume, setDefaultResume] = useState(null);
  const [loadingDefaultResume, setLoadingDefaultResume] = useState(false);
  const [applyError, setApplyError] = useState(null);
  const [notification, setNotification] = useState(null);
  const [saved, setSaved] = useState(false);
  const { user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!jobId || Number.isNaN(jobId)) return;
    setLoading(true);
    jobService.getById(jobId)
      .then(async (data) => {
        setJob(data);
        if (data?.isApplied) setApplied(true);
        if (Array.isArray(data?.otherJobs) && data.otherJobs.length > 0) {
          const filtered = data.otherJobs.filter(j => (j.jobId ?? j.id) !== (data.jobId ?? data.id));
          setOtherJobs(filtered.slice(0, 5));
          return;
        }
        setOtherJobs([]);
      })
      .catch((err) => {
        console.error("Failed to load job:", err);
        setNotification({ type: "error", text: "Failed to load job." });
      })
      .finally(() => setLoading(false));
  }, [jobId]);

  useEffect(() => {
    if (!notification) return;
    const t = setTimeout(() => setNotification(null), 4500);
    return () => clearTimeout(t);
  }, [notification]);

  const isEmployerOwner = user?.role === "Employer" && user?.employerId && user.employerId === job?.employerId;
  const skills = (job?.skillsRequired || "").split(",").map(s => s.trim()).filter(Boolean);

  const getStatusClass = (status) => {
    if (!status) return "open";
    const s = String(status).toLowerCase();
    if (s.includes("open")) return "open";
    if (s.includes("short")) return "shortlisted";
    if (s.includes("interview")) return "interview";
    if (s.includes("hired")) return "hired";
    if (s.includes("reject")) return "rejected";
    return "open";
  };

  const toggleSave = () => {
    setSaved((s) => !s);
    setNotification({ type: "success", text: !saved ? "Saved job" : "Removed saved job" });
  };
  const copyLink = async () => {
    try {
      await navigator.clipboard.writeText(window.location.href);
      setNotification({ type: "success", text: "Link copied to clipboard" });
    } catch {
      setNotification({ type: "error", text: "Failed to copy link" });
    }
  };

  const openInlineApply = async () => {
    if (!user) {
      setNotification({ type: "info", text: "Please login as JobSeeker to apply." });
      return;
    }
    if (user.role !== "JobSeeker") {
      setNotification({ type: "info", text: "Only JobSeeker accounts can apply." });
      return;
    }
    setApplyError(null);
    setCoverLetter("");
    setInlineApplyOpen((v) => !v);
    if (!inlineApplyOpen) {
      setLoadingDefaultResume(true);
      try {
        const jobSeekerId = user?.jobSeekerId ?? (user?.userId ? (await jobSeekerService.getByUser(user.userId)).jobSeekerId : null);
        if (!jobSeekerId) {
          setDefaultResume(null);
          setApplyError("Create your JobSeeker profile before applying.");
        } else {
          const def = await resumeService.getDefaultForJobSeeker(jobSeekerId).catch(() => null);
          if (!def) setApplyError("Please upload & set a default resume first (Manage resumes).");
          setDefaultResume(def);
        }
      } catch (err) {
        console.error("Failed to load default resume", err);
        setApplyError("Unable to load resume info. Try Manage resumes.");
      } finally {
        setLoadingDefaultResume(false);
      }
    }
  };

  const handleApply = async () => {
  setApplyError(null);
  setApplyLoading(true);
  try {
    const jobSeekerId = user?.jobSeekerId ?? (user?.userId ? (await jobSeekerService.getByUser(user.userId)).jobSeekerId : null);
    if (!jobSeekerId) {
      setApplyError("Missing JobSeeker profile.");
      setApplyLoading(false);
      return;
    }
    const def = defaultResume ?? (await resumeService.getDefaultForJobSeeker(jobSeekerId).catch(() => null));
    if (!def) {
      setApplyError("Default resume not found. Please set one in Manage resumes.");
      setApplyLoading(false);
      return;
    }
    const resumeId = def.resumeId ?? def.id;
    await applicationService.create({
      jobId,
      jobSeekerId,
      resumeId,
      coverLetter: coverLetter?.trim() || null
    });
    setApplied(true);
    setInlineApplyOpen(false);
    setNotification({ type: "success", text: "Applied successfully!" });
  } catch (err) {
    console.error("Apply failed:", err);
    const backendMsg = err?.response?.data?.message || err?.message || "";
    const status = err?.response?.status;

    if (status === 400 || backendMsg.toLowerCase().includes("already applied")) {
      setApplied(true);
      setNotification({ type: "info", text: "You have already applied for this job." });
    } else if (status === 500) {
      setApplyError("You have already applied for this job.");
    } else {
      setApplyError("Apply failed. Please try again later.");
    }
  } finally {
    setApplyLoading(false);
  }
};


  const handleDeleteJob = async () => {
    if (!window.confirm("Delete this job? This action cannot be undone.")) return;
    try {
      await jobService.remove(jobId);
      setNotification({ type: "success", text: "Job deleted" });
      navigate("/jobs");
    } catch (err) {
      console.error("Delete job failed:", err);
      const msg = err?.response?.data?.message || err?.message || "Delete failed";
      setNotification({ type: "error", text: "Delete failed: " + msg });
    }
  };

  if (loading) return <div className="container mt-4">Loading…</div>;
  if (!job) return <div className="container mt-4">Job not found.</div>;

  const posted = getPostedDate(job);
  const statusClass = getStatusClass(job?.status);

  return (
    <div className="container mt-4 job-details-page">
      {notification && (
        <div className={`alert job-notice alert-${notification.type === "error" ? "danger" : notification.type === "warning" ? "warning" : notification.type === "info" ? "info" : "success"}`}>
          {notification.text}
        </div>
      )}

      <div className="d-flex justify-content-between align-items-start mb-3">
        <div>
          <h3 className="job-title">{job.title}</h3>
          <div className="text-muted job-sub">
            <span>{job.location}</span>
            {job.location && " • "}
            <span>{job.employerName}</span>
          </div>
        </div>

        <div className="d-flex gap-2">
          <button className={`btn btn-outline-${saved ? "success" : "secondary"} btn-sm`} onClick={toggleSave}>
            {saved ? "Saved" : "Save"}
          </button>
          <button className="btn btn-outline-secondary btn-sm" onClick={copyLink}>Copy link</button>
        </div>
      </div>

      <div className="row">
        <div className="col-lg-8">
          <div className="card mb-3 job-main-card">
            <div className="card-body">
              <h5 className="mb-2">Job description</h5>
              <p className="job-desc">{job.description}</p>

              <div className="mt-3">
                <div className="d-flex flex-wrap gap-2 align-items-center">
                  <div className="badge bg-light text-dark border">Academic: {job.academicEligibility ?? "—"}</div>
                  <div className="badge bg-light text-dark border">Batches: {job.allowedBatches ?? "—"}</div>
                  <div className="badge bg-light text-dark border">Backlogs allowed: {job.backlogs ?? "—"}</div>
                  <div className="badge bg-light text-dark border">Salary: {formatSalary(job.salary)}</div>
                </div>
              </div>

              <div className="mt-4">
                <h6>Skills</h6>
                <div className="skill-chips">
                  {skills.length ? skills.map((s, i) => (
                    <span key={i} className="chip">{s}</span>
                  )) : <span className="text-muted">No skills specified</span>}
                </div>
              </div>
            </div>
          </div>

          <div className="card p-3 mb-4 apply-card">
            <div className="d-flex align-items-start justify-content-between">
              <div style={{flex:1}}>
                {applied ? (
                  <div className="applied-badge">✅ You applied to this job</div>
                ) : (
                  <>
                    <div className="h6 mb-1">Interested in this role?</div>
                    <div className="text-muted small">Make sure you have a default resume set before applying.</div>
                  </>
                )}
              </div>

              <div style={{marginLeft: 12}}>
                {user?.role === "JobSeeker" && !applied && (
                  <button className="btn btn-primary" onClick={openInlineApply}>
                    {inlineApplyOpen ? "Close" : "Apply now"}
                  </button>
                )}

                {isEmployerOwner && (
                  <div className="d-flex gap-2 mt-2">
                    <button className="btn btn-outline-secondary" onClick={() => navigate(`/jobs/edit/${jobId}`)}>Edit</button>
                    <button className="btn btn-danger" onClick={handleDeleteJob}>Delete</button>
                  </div>
                )}
              </div>
            </div>

            {inlineApplyOpen && !applied && (
              <div className="inline-apply-panel mt-3">
                {loadingDefaultResume ? (
                  <div className="small text-muted">Loading resume info…</div>
                ) : (
                  <>
                    {applyError && <div className="alert alert-danger">{applyError}</div>}

                    <div className="mb-2 small text-muted">
                      Default Resume:&nbsp;
                      {defaultResume ? (
                        <strong>{defaultResume.resumeName ?? defaultResume.name ?? "Your resume"}</strong>
                      ) : (
                        <span className="text-warning">No default resume set</span>
                      )}
                    </div>

                    <textarea
                      className="form-control mb-2"
                      rows={5}
                      placeholder="Write a short cover letter (optional)"
                      value={coverLetter}
                      onChange={(e) => setCoverLetter(e.target.value)}
                    />

                    <div className="d-flex gap-2">
                      <button className="btn btn-outline-secondary" onClick={() => setInlineApplyOpen(false)} disabled={applyLoading}>Cancel</button>
                      <button className="btn btn-primary" onClick={handleApply} disabled={applyLoading || !defaultResume}>
                        {applyLoading ? "Applying…" : "Confirm & Apply"}
                      </button>
                      <Link to="/resumes" className="btn btn-link ms-2" onClick={() => setInlineApplyOpen(false)}>Manage resumes</Link>
                    </div>
                  </>
                )}
              </div>
            )}
          </div>

          {otherJobs && otherJobs.length > 0 && (
            <div className="card mb-4">
              <div className="card-body">
                <h6>More jobs</h6>
                <ul className="list-unstyled mb-0">
                  {otherJobs.map(j => (
                    <li key={j.jobId ?? j.id} className="py-2 border-bottom">
                      <Link to={`/jobs/${j.jobId ?? j.id}`} className="fw-semibold">{j.title}</Link>
                      <div className="small text-muted">{j.location} • {formatSalary(j.salary)}</div>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          )}
        </div>

        <div className="col-lg-4">
          <div className="card sidebar-card mb-3">
            <div className="card-body text-center">
              <div className="company-avatar mb-2">{(job.employerName || "H").slice(0,1)}</div>
              <h6 className="mb-1">{job.employerName}</h6>
              <div className="small text-muted mb-2">{job.employerLocation ?? "—"}</div>

              <div className="company-meta">
                <div className="mb-1">
                  <span className={`status-badge ${statusClass}`}>
                    {job.status ?? "Open"}
                  </span>
                </div>
                {posted && <div className="small text-muted">Posted: {new Date(posted).toLocaleDateString()}</div>}
              </div>

              <div className="d-grid gap-2 mt-3">
                <div className="d-flex gap-2 justify-content-center">
                  <button className="btn btn-outline-secondary btn-sm" onClick={copyLink}>Share</button>
                </div>
              </div>
            </div>
          </div>

          <div className="card sidebar-card mb-3">
            <div className="card-body">
              <h6>Quick facts</h6>
              <div className="small text-muted">
                <div><strong>Salary:</strong> {formatSalary(job.salary)}</div>
                <div><strong>Location:</strong> {job.location}</div>
                <div><strong>Eligibility:</strong> {job.academicEligibility ?? "—"}</div>
                <div><strong>Backlogs allowed:</strong> {job.backlogs ?? "—"}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
