import React, { useEffect, useState } from "react";
import resumeService from "../services/resumeService";
import applicationService from "../services/applicationService";
import useAuth from "../auth/useAuth";
import { Link } from "react-router-dom";
import "../styles/jobseekerDashboard.css";

export default function JobSeekerDashboard() {
  const { user } = useAuth();
  const jobSeekerId = user?.jobSeekerId;

  const [loading, setLoading] = useState(true);
  const [resumes, setResumes] = useState([]);
  const [applications, setApplications] = useState([]);
  const [error, setError] = useState(null);


  const [previewResume, setPreviewResume] = useState(null);

  useEffect(() => {
    if (!jobSeekerId) {
      setLoading(false);
      return;
    }

    let mounted = true;
    const load = async () => {
      setLoading(true);
      try {
        const [resList, appList] = await Promise.all([
          resumeService.getByJobSeeker(jobSeekerId).catch(() => []),
          applicationService.getByJobSeeker(jobSeekerId).catch(() => []),
        ]);

        if (!mounted) return;
        setResumes(Array.isArray(resList) ? resList : []);
        setApplications(Array.isArray(appList) ? appList : []);
        setError(null);
      } catch (err) {
        console.error("JobSeekerDashboard load error:", err);
        if (mounted) setError("Failed to load dashboard data.");
      } finally {
        if (mounted) setLoading(false);
      }
    };

    load();
    return () => (mounted = false);
  }, [jobSeekerId]);

  if (!user) return <div className="container mt-4">Login required</div>;
  if (user.role !== "JobSeeker") return <div className="container mt-4">Only JobSeekers can view this page</div>;

  const resumesCount = resumes.length;
  const applicationsCount = applications.length;

  const completeness = Math.min(
    100,
    Math.round(
      (resumesCount > 0 ? 30 : 0) +
        (applicationsCount > 0 ? 20 : 0) +
        (user.userFullName ? 25 : 0) +
        (user.email ? 25 : 0)
    )
  );

  const recentResumes = resumes.slice(0, 3);
  const recentApplications = applications.slice(0, 5);

  const openPreview = (r) => {
    setPreviewResume(r);
    
    document.body.style.overflow = "hidden";
  };
  const closePreview = () => {
    setPreviewResume(null);
    document.body.style.overflow = "";
  };

  
  useEffect(() => {
    const onKey = (e) => {
      if (e.key === "Escape") closePreview();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  return (
    <div className="container dashboard-page">
    
      <div className="dashboard-inner">
        <div className="dashboard-header d-flex align-items-center justify-content-between">
          <div>
            <h2 className="mb-1">
              Welcome back, <span className="name">{user.userFullName ?? (user.email?.split("@")[0] ?? "User")}</span>
            </h2>
            <div className="text-muted">Quick overview of your account and recent activity</div>
          </div>

          <div className="header-actions">
            <Link to="/jobs" className="btn btn-primary me-2">Browse jobs</Link>
            <Link to="/resumes" className="btn btn-outline-secondary">Manage resumes</Link>
          </div>
        </div>

        {loading ? (
          <div className="dashboard-skeleton">
            <div className="sk-row"><div className="sk-card" /><div className="sk-card" /><div className="sk-card" /></div>
            <div className="sk-row mt-3"><div className="sk-list" /><div className="sk-list" /></div>
          </div>
        ) : error ? (
          <div className="alert alert-danger my-3">{error}</div>
        ) : (
          <>
            <div className="row stats-row g-3">
              <div className="col-md-4">
                <div className="stat-card">
                  <div className="stat-left"><div className="stat-icon resumes">📄</div></div>
                  <div className="stat-right">
                    <div className="stat-title">Resumes</div>
                    <div className="stat-value">{resumesCount}</div>
                    <div className="stat-action"><Link to="/resumes" className="link">Manage resumes</Link></div>
                  </div>
                </div>
              </div>

              <div className="col-md-4">
                <div className="stat-card">
                  <div className="stat-left"><div className="stat-icon apps">✉️</div></div>
                  <div className="stat-right">
                    <div className="stat-title">Applications</div>
                    <div className="stat-value">{applicationsCount}</div>
                    <div className="stat-action"><Link to="/applications" className="link">View applications</Link></div>
                  </div>
                </div>
              </div>

              <div className="col-md-4">
                <div className="profile-card">
                  <div className="profile-info">
                    <div className="profile-left">
                      <div className="avatar">{(user.userFullName || user.email || "U").slice(0,1).toUpperCase()}</div>
                    </div>

                    <div className="profile-right">
                      <div className="stat-title">Profile completeness</div>
                      <div className="completeness-value">{completeness}%</div>
                      <div className="progress-wrapper">
                        <div className="progress">
                          <div className="progress-bar" style={{ width: `${completeness}%` }} aria-valuenow={completeness} aria-valuemin="0" aria-valuemax="100" />
                        </div>
                      </div>
                      <div className="mt-2 small text-muted">Complete your profile to increase interview chances</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="row mt-4 g-4">
              <div className="col-lg-6">
                <div className="panel">
                  <div className="panel-header d-flex justify-content-between align-items-center">
                    <h6 className="mb-0">Recent resumes</h6>
                    <Link to="/resumes" className="small link">See all</Link>
                  </div>

                  <div className="panel-body">
                    {recentResumes.length === 0 ? (
                      <div className="text-muted small">You have no resumes yet. <Link to="/resumes">Create one now</Link>.</div>
                    ) : (
                      <ul className="item-list">
                        {recentResumes.map((r, idx) => (
                          <li key={r.resumeId ?? r.id ?? idx} className="item">
                            <div className="item-left"><div className="file-icon">📄</div></div>

                            <div className="item-mid">
                              <div className="item-title">{r.title ?? r.fileName ?? `Resume ${idx + 1}`}</div>
                              <div className="item-meta small text-muted">{r.isDefault ? "Default resume" : r.uploadedOn ? `Uploaded ${new Date(r.uploadedOn).toLocaleDateString()}` : "Uploaded"}</div>
                            </div>

                            <div className="item-right">
                              <button className="btn btn-sm btn-outline-primary me-2" onClick={() => openPreview(r)}>Preview</button>
                              <Link className="btn btn-sm btn-outline-secondary" to="/resumes">Manage</Link>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>
              </div>

              <div className="col-lg-6">
                <div className="panel">
                  <div className="panel-header d-flex justify-content-between align-items-center">
                    <h6 className="mb-0">Recent applications</h6>
                    <Link to="/applications" className="small link">See all</Link>
                  </div>

                  <div className="panel-body">
                    {recentApplications.length === 0 ? (
                      <div className="text-muted small">No recent applications. Browse jobs and apply to start tracking your applications.</div>
                    ) : (
                      <ul className="item-list">
                        {recentApplications.map((a, idx) => (
                          <li key={a.applicationId ?? a.id ?? idx} className="item">
                            <div className="item-left"><div className="job-icon">💼</div></div>
                            <div className="item-mid">
                              <div className="item-title">{a.jobTitle ?? a.title ?? "Job"}</div>
                              <div className="item-meta small text-muted">{a.employerName ?? ""} • {a.status ?? "Applied"} {a.appliedOn ? `• ${new Date(a.appliedOn).toLocaleDateString()}` : ""}</div>
                            </div>
                            <div className="item-right">
                              <Link className="btn btn-sm btn-outline-primary" to={`/jobs/${a.jobId ?? a.id}`}>View job</Link>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-4 d-flex gap-2">
              <Link className="btn btn-primary" to="/jobs">Browse jobs</Link>
              <Link className="btn btn-outline-secondary" to="/resumes">Manage resumes</Link>
              <Link className="btn btn-outline-secondary" to="/profile">Edit profile</Link>
            </div>
          </>
        )}
      </div>

      
      {previewResume && (
        <div className="preview-modal" role="dialog" aria-modal="true" aria-label="Resume preview">
          <div className="preview-panel">
            <div className="preview-header">
              <div>
                <h5 className="mb-0">{previewResume.title ?? previewResume.fileName ?? "Resume preview"}</h5>
                <div className="small text-muted">{previewResume.isDefault ? "Default resume" : previewResume.uploadedOn ? `Uploaded ${new Date(previewResume.uploadedOn).toLocaleDateString()}` : ""}</div>
              </div>
              <button className="btn-close" aria-label="Close preview" onClick={closePreview}>✕</button>
            </div>

            <div className="preview-body">
              {previewResume.downloadUrl ? (
                <iframe
                  src={previewResume.downloadUrl}
                  title="resume-preview"
                  style={{ width: "100%", height: 520, border: "none", borderRadius: 8 }}
                />
              ) : (
                <div className="p-3 text-muted">
                  No file available to preview for this resume. Use <Link to="/resumes">Manage resumes</Link> to upload a file or set a default resume.
                </div>
              )}
            </div>

            <div className="preview-footer d-flex justify-content-end gap-2">
              <button className="btn btn-outline-secondary" onClick={closePreview}>Close</button>
              <Link className="btn btn-primary" to="/resumes" onClick={closePreview}>Manage resumes</Link>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
