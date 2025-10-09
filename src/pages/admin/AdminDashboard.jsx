import React, { useEffect, useState, useRef } from "react";
import adminService from "../../services/adminService";
import { toast } from "react-toastify";
import "../../styles/admin.css";
import { Link } from "react-router-dom";

function useAnimatedCount(value, duration = 700) {
  const [count, setCount] = useState(0);
  const startRef = useRef(null);

  useEffect(() => {
    let start = performance.now();
    const initial = Number(count);
    const target = Number(value);

    function step(now) {
      if (!startRef.current) startRef.current = now;
      const elapsed = now - startRef.current;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3); 
      const current = Math.round(initial + (target - initial) * eased);
      setCount(current);
      if (progress < 1) requestAnimationFrame(step);
      else startRef.current = null;
    }

    
    if (target === 0) setCount(0);
    requestAnimationFrame(step);

    return () => {
      startRef.current = null;
    };
 
  }, [value]);

  return count;
}

export default function AdminDashboard() {
  const [stats, setStats] = useState({ users: 0, jobs: 0, applications: 0 });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    async function load() {
      try {
        const data = await adminService.getStats();
        if (!mounted) return;
        setStats({
          users: data?.totalUsers ?? data?.users ?? 0,
          jobs: data?.totalJobs ?? data?.jobs ?? 0,
          applications:
            data?.totalApplications ?? data?.applications ?? 0
        });
      } catch (err) {
        toast.error("Failed to load stats");
      } finally {
        if (mounted) setLoading(false);
      }
    }
    load();
    return () => (mounted = false);
  }, []);

  const usersCount = useAnimatedCount(stats.users);
  const jobsCount = useAnimatedCount(stats.jobs);
  const appsCount = useAnimatedCount(stats.applications);

  if (loading)
    return (
      <div className="container mt-4">
        <div className="loading-dot" aria-hidden />
        <div className="sr-only">Loading stats…</div>
      </div>
    );

  return (
    <div className="container mt-4 admin-dashboard">
      <div className="dash-header">
        <h1>Admin Dashboard</h1>
        <p className="muted">Overview of platform activity</p>
      </div>

      <div className="stats-grid" role="list">
        <article className="stat-card stat-users" role="listitem" aria-label="Total users">
          <div className="card-top">
            <div className="icon" aria-hidden>
              <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="12" cy="7" r="4" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </div>
            <small className="label">Total Users</small>
          </div>

          <div className="card-body">
            <div className="big-number">{usersCount}</div>
            <div className="meta">Active users on the platform</div>
          </div>

          <div className="card-footer">
            <Link to="/admin/users" className="btn-link">View users →</Link>
          </div>
        </article>

        <article className="stat-card stat-jobs" role="listitem" aria-label="Total jobs">
          <div className="card-top">
            <div className="icon" aria-hidden>
              <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <rect x="2" y="7" width="20" height="14" rx="2" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                <path d="M16 3v4M8 3v4M3 11h18" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </div>
            <small className="label">Total Jobs</small>
          </div>

          <div className="card-body">
            <div className="big-number">{jobsCount}</div>
            <div className="meta">Positions posted by employers</div>
          </div>

          <div className="card-footer">
            <Link to="/admin/jobs" className="btn-link">View jobs →</Link>
          </div>
        </article>

        <article className="stat-card stat-apps" role="listitem" aria-label="Total applications">
          <div className="card-top">
            <div className="icon" aria-hidden>
              <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                <polyline points="7 10 12 15 17 10" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                <line x1="12" y1="15" x2="12" y2="3" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </div>
            <small className="label">Total Applications</small>
          </div>

          <div className="card-body">
            <div className="big-number">{appsCount}</div>
            <div className="meta">Total job applications submitted</div>
          </div>

          <div className="card-footer">
            <Link to="/admin/applications" className="btn-link">View applications →</Link>
          </div>
        </article>
      </div>

     
    </div>
  );
}
