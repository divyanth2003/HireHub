import React, { useEffect, useState } from "react";
import jobService from "../services/jobService";
import applicationService from "../services/applicationService";
import useAuth from "../auth/useAuth";
import { Link } from "react-router-dom";
import "../styles/employerdashboard.css";

export default function EmployerDashboard() {
  const { user } = useAuth();
  const employerId = user?.employerId;
  const [loading, setLoading] = useState(true);
  const [jobsCount, setJobsCount] = useState(0);
  const [applicationsCount, setApplicationsCount] = useState(0);
  const [jobs, setJobs] = useState([]);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!employerId) {
      setLoading(false);
      return;
    }

    const load = async () => {
      setLoading(true);
      try {
        const myJobs = await jobService.getByEmployer(employerId);
        const arrJobs = Array.isArray(myJobs) ? myJobs : [];
        setJobs(arrJobs);
        setJobsCount(arrJobs.length);

        const promises = arrJobs.map((j) =>
          applicationService.getByJob(j.jobId).catch((e) => {
            console.warn("Failed loading applications for job", j.jobId, e);
            return [];
          })
        );

        const results = await Promise.all(promises);
        const totalApps = results.reduce((acc, r) => acc + (Array.isArray(r) ? r.length : 0), 0);
        setApplicationsCount(totalApps);
      } catch (err) {
        console.error("EmployerDashboard load error:", err);
        setError("Failed to load dashboard data.");
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [employerId]);

  if (!user) return <div className="container mt-4">Login required</div>;
  if (user.role !== "Employer") return <div className="container mt-4">Only Employers can view this page</div>;

  return (
    <div className="container mt-4 employer-dashboard">
      <div className="dashboard-header">
        <div>
          <h1 className="page-title">Employer Dashboard</h1>
          <p className="muted">Quick overview of your jobs and recent activity</p>
        </div>

        <div className="header-actions">
          <Link to="/jobs/create" className="btn btn-primary">Post job</Link>
          <Link to="/employer/jobs" className="btn btn-outline-secondary ms-2">Manage jobs</Link>
        </div>
      </div>

      {loading ? (
        <div className="loading-block">Loading…</div>
      ) : error ? (
        <div className="alert alert-danger">{error}</div>
      ) : (
        <>
          <div className="stat-grid">
            <div className="stat-card">
              <div className="stat-title">My Jobs</div>
              <div className="stat-value">{jobsCount}</div>
              <div className="stat-sub"><Link to="/employer/jobs">View & manage your jobs</Link></div>
            </div>

            <div className="stat-card">
              <div className="stat-title">Total Applications</div>
              <div className="stat-value">{applicationsCount}</div>
              <div className="stat-sub muted">Across all your jobs</div>
            </div>

            <div className="stat-card">
              <div className="stat-title">Active Openings</div>
              <div className="stat-value">{jobs.filter(j => j.status === "Open").length}</div>
              <div className="stat-sub muted">Currently accepting applicants</div>
            </div>
          </div>

          <section className="recent-jobs">
            <div className="section-header">
              <h4 className="section-title">Recent Jobs</h4>
              <div className="section-actions">
                <Link to="/jobs/create" className="link-primary">New job</Link>
                <Link to="/employer/jobs" className="link-muted ms-3">See all</Link>
              </div>
            </div>

            {jobs.length === 0 ? (
              <div className="card empty-card p-3">
                <div>No jobs posted yet. <Link to="/jobs/create">Create your first job</Link></div>
              </div>
            ) : (
              <div className="jobs-list">
                {jobs.slice(0, 6).map((j) => (
                  <div key={j.jobId} className="job-card">
                    <div className="job-left">
                      <div className="job-title">{j.title}</div>
                      <div className="job-meta muted">{j.location || "—"} • {j.status || "—"}</div>
                    </div>

                    <div className="job-actions">
                      <Link to={`/jobs/${j.jobId}`} className="btn btn-sm btn-outline-secondary">View</Link>
                      <Link to={`/jobs/edit/${j.jobId}`} className="btn btn-sm btn-outline-primary ms-2">Edit</Link>
                      <Link to={`/jobs/${j.jobId}/applications`} className="btn btn-sm btn-outline-success ms-2">Applications</Link>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
