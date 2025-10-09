import React, { useEffect, useState, useRef } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import jobService from "../services/jobService";
import "../styles/joblist.css";

const PAGE_SIZE_OPTIONS = [5, 10, 20];

export default function JobsList() {
  const [jobs, setJobs] = useState([]);
  const [q, setQ] = useState("");
  const [skill, setSkill] = useState("");
  const [locationInput, setLocationInput] = useState("");
  const [company, setCompany] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);


  const [activeFilters, setActiveFilters] = useState([]); 
  const [sortBy, setSortBy] = useState("newest"); 
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const debounceRef = useRef(null);
  const location = useLocation();
  const navigate = useNavigate();

  const loadAll = async () => {
    setLoading(true);
    try {
      const data = await jobService.getAll();
      setJobs(data || []);
      setError(null);
    } catch (err) {
      console.error("loadAll failed", err);
      setError("Failed to load jobs");
      setJobs([]);
    } finally {
      setLoading(false);
    }
  };

 
  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const companyParam = params.get("company");
    const titleParam = params.get("title");
    const qParam = params.get("q");
    const locParam = params.get("loc");

    const run = async () => {
      setPage(1);
      if (companyParam) {
        setLoading(true);
        try {
          const data = await jobService.searchByCompany(companyParam);
          setJobs(data || []);
          setError(null);
        } catch {
          setError("Search failed");
          setJobs([]);
        } finally {
          setLoading(false);
        }
        return;
      }
      if (titleParam || qParam) {
        setLoading(true);
        try {
          const data = await jobService.searchByTitle(titleParam || qParam);
          setJobs(data || []);
          setError(null);
        } catch {
          setError("Search failed");
          setJobs([]);
        } finally {
          setLoading(false);
        }
        return;
      }
      if (locParam) {
        setLoading(true);
        try {
          const data = await jobService.searchByLocation(locParam);
          setJobs(data || []);
          setError(null);
        } catch {
          setError("Search failed");
          setJobs([]);
        } finally {
          setLoading(false);
        }
        return;
      }
      await loadAll();
    };

    run();
    
  }, [location.search]);

 
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (!q) {
     
      debounceRef.current = setTimeout(() => {
        loadAll();
      }, 350);
      return;
    }

    debounceRef.current = setTimeout(async () => {
      setLoading(true);
      try {
        const data = await jobService.searchByTitle(q);
        setJobs(data || []);
        setError(null);
        setPage(1);
      } catch (err) {
        console.error("title search failed", err);
        setError("Search failed");
        setJobs([]);
      } finally {
        setLoading(false);
      }
    }, 350);

    return () => clearTimeout(debounceRef.current);
   
  }, [q]);

 
  const searchSkill = async () => {
    if (!skill) return;
    setLoading(true);
    try {
      const data = await jobService.searchBySkill(skill);
      setJobs(data || []);
      setError(null);
      setActiveFilters((f) => addFilter(f, `skill:${skill}`));
      setPage(1);
    } catch (err) {
      console.error("skill search failed", err);
      setError("Search failed");
      setJobs([]);
    } finally {
      setLoading(false);
    }
  };

  const searchLocation = async () => {
    if (!locationInput) return;
    setLoading(true);
    try {
      const data = await jobService.searchByLocation(locationInput);
      setJobs(data || []);
      setError(null);
      setActiveFilters((f) => addFilter(f, `loc:${locationInput}`));
      setPage(1);
    } catch (err) {
      console.error("location search failed", err);
      setError("Search failed");
      setJobs([]);
    } finally {
      setLoading(false);
    }
  };

  const searchCompany = async () => {
    if (!company) return;
    setLoading(true);
    try {
      const data = await jobService.searchByCompany(company);
      setJobs(data || []);
      setError(null);
      setActiveFilters((f) => addFilter(f, `company:${company}`));
      setPage(1);
    } catch (err) {
      console.error("company search failed", err);
      setError("Search failed");
      setJobs([]);
    } finally {
      setLoading(false);
    }
  };

  
  const addFilter = (current, val) => {
    if (!val) return current;
    if (current.includes(val)) return current;
    return [...current, val];
  };

  const removeFilter = (val) => {
    setActiveFilters((f) => f.filter((x) => x !== val));
  
    loadAll();
  };

  const clearFilters = () => {
    setActiveFilters([]);
    setQ("");
    setSkill("");
    setLocationInput("");
    setCompany("");
    loadAll();
  };

 
  const formatDateField = (job) => {
    if (!job || typeof job !== "object") return "—";
    const dateFields = ["postedOn", "createdAt", "datePosted", "createdDate", "createdOn"];
    for (const key of dateFields) {
      const val = job[key];
      if (!val && val !== 0) continue;
      const d = new Date(val);
      if (!isNaN(d.getTime())) {
      
        return new Intl.DateTimeFormat("en-GB", {
          day: "numeric",
          month: "short",
          year: "numeric"
        }).format(d);
      }
    }
    return "—";
  };

  const sortedJobs = (() => {
    const arr = Array.isArray(jobs) ? [...jobs] : [];
    if (sortBy === "salary_high") {
      arr.sort((a, b) => Number(b.salary || 0) - Number(a.salary || 0));
    } else if (sortBy === "salary_low") {
      arr.sort((a, b) => Number(a.salary || 0) - Number(b.salary || 0));
    } else {
      
      arr.sort((a, b) => {
        const pa = (() => {
          const aDate = a?.postedOn ?? a?.createdAt ?? a?.datePosted ?? a?.createdDate ?? a?.createdOn;
          const d = new Date(aDate);
          return isNaN(d.getTime()) ? 0 : d.getTime();
        })();
        const pb = (() => {
          const bDate = b?.postedOn ?? b?.createdAt ?? b?.datePosted ?? b?.createdDate ?? b?.createdOn;
          const d = new Date(bDate);
          return isNaN(d.getTime()) ? 0 : d.getTime();
        })();
        return pb - pa;
      });
    }
    return arr;
  })();

  const total = sortedJobs.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const pagedJobs = sortedJobs.slice((page - 1) * pageSize, page * pageSize);

 
  const formatSalary = (s) => {
    if (!s && s !== 0) return "—";
    const n = Number(s);
    if (Number.isNaN(n)) return s;
    return n >= 1000 ? n.toLocaleString("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }) : `${n}`;
  };

  
  const handleCopyLink = async (jobId) => {
    try {
      await navigator.clipboard.writeText(window.location.origin + `/jobs/${jobId}`);
     
      alert("Job link copied to clipboard");
    } catch (e) {
      console.error("copy failed", e);
      alert("Failed to copy link");
    }
  };

  return (
    <div className="container mt-3 jobs-list-page">
      <div className="d-flex align-items-center justify-content-between mb-3">
        <h3 className="mb-0">Jobs</h3>

        <div className="d-flex gap-2 align-items-center">
          <select className="form-select form-select-sm" value={sortBy} onChange={(e) => { setSortBy(e.target.value); setPage(1); }}>
            <option value="newest">Sort: Newest</option>
            <option value="salary_high">Sort: Salary — High → Low</option>
            <option value="salary_low">Sort: Salary — Low → High</option>
          </select>

          <select className="form-select form-select-sm" value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}>
            {PAGE_SIZE_OPTIONS.map(o => <option key={o} value={o}>{o} / page</option>)}
          </select>
        </div>
      </div>

    
      <div className="search-area mb-3">
        <div className="input-with-btn mb-2">
          <div className="icon-left">🔎</div>
          <input className="form-control" placeholder="Search title (type to search)" value={q} onChange={(e) => setQ(e.target.value)} />
          <button className="btn btn-outline-primary ms-2" onClick={() => navigate(`/jobs?title=${encodeURIComponent(q)}`)}>Search</button>
        </div>

        <div className="d-flex gap-2 mb-2">
          <div className="input-with-btn flex-grow-1">
            <div className="icon-left">⚙️</div>
            <input className="form-control" placeholder="Skill" value={skill} onChange={(e) => setSkill(e.target.value)} />
            <button className="btn btn-outline-primary ms-2" onClick={searchSkill}>By Skill</button>
          </div>

          <div className="input-with-btn" style={{ minWidth: 240 }}>
            <div className="icon-left">📍</div>
            <input className="form-control" placeholder="Location" value={locationInput} onChange={(e) => setLocationInput(e.target.value)} />
            <button className="btn btn-outline-primary ms-2" onClick={searchLocation}>By Location</button>
          </div>
        </div>

        <div className="d-flex gap-2 mb-2">
          <div className="input-with-btn flex-grow-1">
            <div className="icon-left">🏢</div>
            <input className="form-control" placeholder="Company (e.g. Hexaware)" value={company} onChange={(e) => setCompany(e.target.value)} />
            <button className="btn btn-outline-primary ms-2" onClick={searchCompany}>By Company</button>
          </div>

          <div className="d-flex align-items-center gap-2">
            <button className="btn btn-outline-secondary" onClick={clearFilters}>Clear</button>
          </div>
        </div>

     
        <div className="mb-2">
          {activeFilters.length > 0 ? (
            <>
              <small className="text-muted me-2">Active filters:</small>
              {activeFilters.map((f) => (
                <button key={f} className="chip" onClick={() => removeFilter(f)}>
                  {f.replace(/^skill:|^loc:|^company:/, (m) => m === "skill:" ? "Skill: " : m === "loc:" ? "Location: " : "Company: ")}
                  <span className="chip-x">✕</span>
                </button>
              ))}
            </>
          ) : (
            <small className="text-muted">No filters active</small>
          )}
        </div>
      </div>

      {loading ? (
        <div className="jobs-skeleton">
        
          {Array.from({ length: Math.min(pageSize, 6) }).map((_, i) => (
            <div key={i} className="job-card-skel">
              <div className="s-title" />
              <div className="s-meta" />
              <div className="s-desc" />
            </div>
          ))}
        </div>
      ) : error ? (
        <div className="text-danger">{error}</div>
      ) : (
        <>
          <div className="jobs-grid">
            {pagedJobs.length === 0 ? (
              <div className="text-muted">No jobs found.</div>
            ) : pagedJobs.map((j) => (
              <article key={j.jobId} className="job-card">
                <div className="job-card-left">
                  <Link to={`/jobs/${j.jobId}`} className="job-title-link"><b>{j.title}</b></Link>
                  <div className="job-meta text-muted">
                    {j.location} • {j.employerName} • {formatSalary(j.salary)}
                  </div>
                  <div className="job-desc-snippet">
                    {j.description ? (j.description.length > 120 ? j.description.slice(0, 120) + "…" : j.description) : ""}
                  </div>
                  <div className="skill-line mt-2">
                    {(j.skillsRequired || "").split(",").map(s => s.trim()).filter(Boolean).slice(0,5).map((s, idx) => (
                      <span key={idx} className="skill-pill">{s}</span>
                    ))}
                  </div>
                </div>

                <div className="job-card-right">
                  <div className="small text-muted">Posted: {formatDateField(j)}</div>
                  <div className="mt-2 d-flex flex-column gap-2">
                    <Link className="btn btn-outline-primary btn-sm" to={`/jobs/${j.jobId}`}>View</Link>
                    <button className="btn btn-outline-secondary btn-sm" onClick={() => handleCopyLink(j.jobId)}>Copy link</button>
                  </div>
                </div>
              </article>
            ))}
          </div>

      
          <div className="d-flex justify-content-between align-items-center mt-3">
            <div>
              <small className="text-muted">Showing {(page - 1) * pageSize + 1} – {Math.min(page * pageSize, total)} of {total}</small>
            </div>

            <nav aria-label="Jobs pagination">
              <ul className="pagination mb-0">
                <li className={`page-item ${page === 1 ? "disabled" : ""}`}>
                  <button className="page-link" onClick={() => setPage(1)}>First</button>
                </li>
                <li className={`page-item ${page === 1 ? "disabled" : ""}`}>
                  <button className="page-link" onClick={() => setPage(p => Math.max(1, p - 1))}>Prev</button>
                </li>

                <li className="page-item active"><span className="page-link">{page}</span></li>

                <li className={`page-item ${page === totalPages ? "disabled" : ""}`}>
                  <button className="page-link" onClick={() => setPage(p => Math.min(totalPages, p + 1))}>Next</button>
                </li>
                <li className={`page-item ${page === totalPages ? "disabled" : ""}`}>
                  <button className="page-link" onClick={() => setPage(totalPages)}>Last</button>
                </li>
              </ul>
            </nav>
          </div>
        </>
      )}
    </div>
  );
}
