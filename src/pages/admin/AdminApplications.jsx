import React, { useEffect, useState, useMemo, useCallback } from "react";
import adminService from "../../services/adminService";
import { toast } from "react-toastify";
import "../../styles/admin.css";

function useDebounce(value, ms = 400) {
  const [deb, setDeb] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDeb(value), ms);
    return () => clearTimeout(t);
  }, [value, ms]);
  return deb;
}

export default function AdminApplications() {
  const [applications, setApplications] = useState([]);
  const [loading, setLoading] = useState(true);

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounce(search, 400);

  const [jobFilter, setJobFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");

  const [page, setPage] = useState(1);
  const pageSize = 8;

  const loadApplications = useCallback(async (q = "") => {
    setLoading(true);
    try {
      const data = await adminService.getApplications(q);
      setApplications(Array.isArray(data) ? data : []);
      setPage(1);
    } catch (err) {
      console.error(err);
      toast.error("Failed to load applications");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadApplications(debouncedSearch);
  }, [debouncedSearch, loadApplications]);

  const handleDelete = async (id) => {
    if (!window.confirm("Delete this application?")) return;
    try {
      await adminService.deleteApplication(id);
      toast.success("Application deleted");
      await loadApplications(debouncedSearch);
    } catch (err) {
      console.error(err);
      toast.error("Delete failed");
    }
  };

  const normalizeApplicantName = (a) => {
    if (!a) return "—";
    if (typeof a.applicantName === "string" && a.applicantName.trim()) return a.applicantName;
    if (typeof a.jobSeekerName === "string" && a.jobSeekerName.trim()) return a.jobSeekerName;
    if (a.jobSeeker) {
      if (a.jobSeeker.fullName) return a.jobSeeker.fullName;
      if (a.jobSeeker.user && a.jobSeeker.user.fullName) return a.jobSeeker.user.fullName;
    }
    if (a.jobSeekerFullName) return a.jobSeekerFullName;
    if (a.applicant && typeof a.applicant === "object") {
      if (a.applicant.fullName) return a.applicant.fullName;
      if (a.applicant.name) return a.applicant.name;
    }
    return "—";
  };

  const jobs = useMemo(() => {
    const s = new Set();
    applications.forEach(a => a.jobTitle && s.add(a.jobTitle));
    return ["all", ...Array.from(s).sort()];
  }, [applications]);

  const statuses = useMemo(() => {
    const s = new Set();
    applications.forEach(a => a.status && s.add(a.status));
    return ["all", ...Array.from(s).sort()];
  }, [applications]);

  const filtered = useMemo(() => {
    const q = (debouncedSearch || "").toString().toLowerCase().trim();
    return applications.filter(a => {
      const matchesQuery = !q || (
        (a.applicantName || "").toString().toLowerCase().includes(q) ||
        (a.jobTitle || "").toString().toLowerCase().includes(q) ||
        String(a.applicationId || a.id || "").toLowerCase().includes(q)
      );
      const matchesJob = jobFilter === "all" || (a.jobTitle || "") === jobFilter;
      const matchesStatus = statusFilter === "all" || (a.status || "").toString() === statusFilter;
      return matchesQuery && matchesJob && matchesStatus;
    });
  }, [applications, debouncedSearch, jobFilter, statusFilter]);

  const total = filtered.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const pageItems = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filtered.slice(start, start + pageSize);
  }, [filtered, page]);

  return (
    <div className="dash-header" style={{ marginBottom: 12 }}>
      <h3 style={{ margin: 0 }}>
        Manage Applications
        Review and manage job applications
      </h3>

      <div className="filters-row">
        <input
          type="text"
          className="form-control input-search"
          placeholder="Search by applicant, job or id..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ minWidth: 260 }}
        />

        <select
          className="form-select"
          value={jobFilter}
          onChange={(e) => { setJobFilter(e.target.value); setPage(1); }}
        >
          {jobs.map(j => <option key={j} value={j}>{j === "all" ? "All jobs" : j}</option>)}
        </select>

        <select
          className="form-select"
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
        >
          {statuses.map(s => <option key={s} value={s}>{s === "all" ? "All status" : s}</option>)}
        </select>
      </div>

      <div className="card card-surface">
        <div className="card-body">
          {loading ? (
            <div className="table-placeholder">Loading applications…</div>
          ) : (
            <>
              <div className="table-responsive">
                <table className="table modern-table">
                  <thead>
                    <tr>
                      <th style={{ width: 90 }}>ID</th>
                      <th>Applicant</th>
                      <th>Job</th>
                      <th>Applied On</th>
                      <th style={{ width: 120 }}>Status</th>
                      <th style={{ width: 100 }} className="text-end">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pageItems.length === 0 ? (
                      <tr>
                        <td colSpan={6} className="text-center text-muted">No applications found</td>
                      </tr>
                    ) : pageItems.map(a => (
                      <tr key={a.applicationId ?? a.id}>
                        <td className="mono-col">{a.applicationId ?? a.id}</td>
                        <td>
                          <div style={{ fontWeight: 600 }}>{normalizeApplicantName(a)}</div>
                          <div className="muted" style={{ fontSize: "0.9rem" }}>{a.applicantEmail}</div>
                        </td>
                        <td>{a.jobTitle}</td>
                        <td>{a.appliedAt ? new Date(a.appliedAt).toLocaleString() : "—"}</td>
                        <td>
                          <span className={`badge ${a.status?.toLowerCase() === "shortlisted"
                            ? "bg-success"
                            : a.status?.toLowerCase() === "interview"
                              ? "bg-info"
                              : "bg-secondary"}`}>
                            {a.status ?? "—"}
                          </span>
                        </td>
                        <td className="text-end">
                          <button
                            className="btn btn-sm btn-danger"
                            onClick={() => handleDelete(a.applicationId ?? a.id)}
                          >
                            Delete
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="table-footer">
                <div className="footer-left muted">
                  Showing <strong>{total === 0 ? 0 : (page - 1) * pageSize + 1}</strong> - <strong>{(page - 1) * pageSize + pageItems.length}</strong> of <strong>{total}</strong>
                </div>
                <div className="pagination">
                  <button className="btn-p small" onClick={() => setPage(1)} disabled={page === 1}>«</button>
                  <button className="btn-p small" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>‹</button>
                  <span className="page-indicator">{page} / {totalPages}</span>
                  <button className="btn-p small" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>›</button>
                  <button className="btn-p small" onClick={() => setPage(totalPages)} disabled={page === totalPages}>»</button>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
