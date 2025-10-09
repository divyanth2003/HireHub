import React, { useEffect, useState, useMemo, useCallback } from "react";
import adminService from "../../services/adminService";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";
import "../../styles/admin.css";


function useDebounce(value, ms = 400) {
  const [deb, setDeb] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDeb(value), ms);
    return () => clearTimeout(t);
  }, [value, ms]);
  return deb;
}

export default function AdminJobs() {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounce(search, 400);


  const [companyFilter, setCompanyFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all"); 
  const [locationFilter, setLocationFilter] = useState("all");


  const [page, setPage] = useState(1);
  const pageSize = 8;

  const navigate = useNavigate();

  const loadJobs = useCallback(async (q = "") => {
    setLoading(true);
    try {
      const data = await adminService.getJobs(q);
      setJobs(Array.isArray(data) ? data : []);
      setPage(1);
    } catch (err) {
      console.error(err);
      toast.error("Failed to load jobs");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadJobs(debouncedSearch);
  }, [debouncedSearch, loadJobs]);

  const handleDelete = async (id) => {
    if (!window.confirm("Delete this job?")) return;
    try {
      await adminService.deleteJob(id);
      toast.success("Job deleted");
      await loadJobs(debouncedSearch);
    } catch (err) {
      console.error(err);
      toast.error("Delete failed");
    }
  };

  
  const companies = useMemo(() => {
    const s = new Set();
    jobs.forEach(j => j.employerName && s.add(j.employerName));
    return ["all", ...Array.from(s).sort()];
  }, [jobs]);

  const locations = useMemo(() => {
    const s = new Set();
    jobs.forEach(j => j.location && s.add(j.location));
    return ["all", ...Array.from(s).sort()];
  }, [jobs]);

  const filtered = useMemo(() => {
    const q = (debouncedSearch || "").toString().toLowerCase().trim();
    return jobs.filter(j => {
     
      const matchesQuery = !q || (
        (j.title || "").toString().toLowerCase().includes(q) ||
        (j.employerName || "").toString().toLowerCase().includes(q) ||
        String(j.jobId || "").toLowerCase().includes(q)
      );

      const matchesCompany = companyFilter === "all" ||
        (j.employerName || "").toString() === companyFilter;

      const matchesStatus = statusFilter === "all" ||
        (j.status || "").toString().toLowerCase() === statusFilter.toLowerCase();

      const matchesLocation = locationFilter === "all" ||
        (j.location || "").toString() === locationFilter;

      return matchesQuery && matchesCompany && matchesStatus && matchesLocation;
    });
  }, [jobs, debouncedSearch, companyFilter, statusFilter, locationFilter]);

  const total = filtered.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const pageJobs = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filtered.slice(start, start + pageSize);
  }, [filtered, page]);

  return (
    <div className="container mt-4 admin-jobs">
      <div className="dash-header" style={{ marginBottom: 12 }}>
        <div>
          <h3 style={{ margin: 0 }}>Manage Jobs</h3>
          <div className="muted">Search, filter and manage jobs</div>
        </div>

        <div className="filters-row">
          <input
            type="text"
            className="form-control input-search"
            placeholder="Search by title, company or id..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ minWidth: 260 }}
          />

          <select
            className="form-select"
            value={companyFilter}
            onChange={(e) => { setCompanyFilter(e.target.value); setPage(1); }}
          >
            {companies.map(c => <option key={c} value={c}>{c === "all" ? "All companies" : c}</option>)}
          </select>

          <select
            className="form-select"
            value={statusFilter}
            onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          >
            <option value="all">All status</option>
            <option value="Open">Open</option>
            <option value="Closed">Closed</option>
          </select>

          <select
            className="form-select"
            value={locationFilter}
            onChange={(e) => { setLocationFilter(e.target.value); setPage(1); }}
          >
            {locations.map(loc => <option key={loc} value={loc}>{loc === "all" ? "All locations" : loc}</option>)}
          </select>
        </div>
      </div>

      <div className="card card-surface">
        <div className="card-body">
          {loading ? (
            <div className="table-placeholder">Loading jobs…</div>
          ) : (
            <>
              <div className="table-responsive">
                <table className="table modern-table">
                  <thead>
                    <tr>
                      <th style={{ width: 80 }}>ID</th>
                      <th>Title</th>
                      <th>Company</th>
                      <th>Location</th>
                      <th style={{ width: 120 }}>Status</th>
                      <th style={{ width: 160 }} className="text-end">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pageJobs.length === 0 ? (
                      <tr>
                        <td colSpan={6} className="text-center text-muted">No jobs found</td>
                      </tr>
                    ) : pageJobs.map(j => (
                      <tr key={j.jobId}>
                        <td className="mono-col">{j.jobId}</td>
                        <td>{j.title}</td>
                        <td>{j.employerName}</td>
                        <td>{j.location}</td>
                        <td>
                          <span className={`badge ${j.status === "Open" ? "bg-success" : "bg-secondary"}`}>
                            {j.status}
                          </span>
                        </td>
                        <td className="text-end">
                          <button
                            className="btn btn-sm btn-ghost me-2"
                            onClick={() => navigate(`/jobs/${j.jobId}`)}
                          >
                            View
                          </button>
                          <button
                            className="btn btn-sm btn-danger"
                            onClick={() => handleDelete(j.jobId)}
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
                  Showing <strong>{total === 0 ? 0 : (page - 1) * pageSize + 1}</strong> - <strong>{(page - 1) * pageSize + pageJobs.length}</strong> of <strong>{total}</strong>
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
