import React, { useEffect, useState, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import jobService from "../services/jobService";
import "../styles/home.css";

export default function Home() {
  const [featured, setFeatured] = useState([]);
  const [loadingFeatured, setLoadingFeatured] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [modalQuery, setModalQuery] = useState("");
  const [modalLoading, setModalLoading] = useState(false);
  const [modalError, setModalError] = useState(null);
  const [modalJobs, setModalJobs] = useState([]);
  const navigate = useNavigate();
  const modalRef = useRef(null);

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoadingFeatured(true);
      try {
        const list = (await jobService.getAll?.()) || [];
        if (!mounted) return;
       
        setFeatured((list || []).slice(0, 6));
      } catch (err) {
        console.warn("Failed to load featured jobs", err);
        if (mounted) setFeatured([]);
      } finally {
        if (mounted) setLoadingFeatured(false);
      }
    })();
    return () => (mounted = false);
  }, []);

  
  const openModalFor = async (query) => {
    setModalQuery(query || "");
    setModalOpen(true);
    setModalError(null);
    setModalLoading(true);
    setModalJobs([]);
    try {
      const results = jobService.searchByTitle
        ? await jobService.searchByTitle(query)
        : await jobService.getAll();
      setModalJobs(results || []);
    } catch (err) {
      console.error("Modal search failed", err);
      setModalError("Failed to load jobs. Try again.");
      setModalJobs([]);
    } finally {
      setModalLoading(false);
    }
  };

  const closeModal = () => {
    setModalOpen(false);
    setModalJobs([]);
    setModalError(null);
    setModalLoading(false);
  };

  
  useEffect(() => {
    const onKey = (e) => {
      if (e.key === "Escape" && modalOpen) closeModal();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [modalOpen]);


  const showCopySuccess = (msg = "Link copied to clipboard!") => {
    toast.success(msg, {
      position: "bottom-right",
      autoClose: 1800,
      hideProgressBar: false,
      closeOnClick: true,
      pauseOnHover: true,
      draggable: true,
      theme: "light",
    });
  };

  const showCopyError = (msg = "Copy failed") => {
    toast.error(msg, {
      position: "bottom-right",
      autoClose: 2200,
      hideProgressBar: false,
      closeOnClick: true,
      pauseOnHover: true,
      draggable: true,
      theme: "light",
    });
  };

  const handleCopy = async (jobId) => {
    try {
      const link = window.location.origin + `/jobs/${jobId}`;
   
      if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(link);
      } else {
       
        const ta = document.createElement("textarea");
        ta.value = link;
        document.body.appendChild(ta);
        ta.select();
        try {
          document.execCommand("copy");
        } catch (ex) {
      
          throw ex;
        } finally {
          document.body.removeChild(ta);
        }
      }
      showCopySuccess();
    } catch (err) {
      console.error("Copy failed:", err);
      showCopyError();
    }
  };

  
  const goToJobsPage = (preset) => {
    if (preset) navigate(`/jobs?q=${encodeURIComponent(preset)}`);
    else navigate("/jobs");
  };

  return (
    <div className="home-page">
    
      <header className="hero">
        <div className="hero-inner container">
          <div className="hero-left">
            <h1 className="hero-title">Find your next role — faster</h1>
            <p className="hero-sub">
              Discover openings, save jobs, and apply — <b>HireHub</b> helps you find opportunities that match your goals.
            </p>

            <div className="hero-cta-row">
              <button
                className="btn-primary hero-cta"
                onClick={() => goToJobsPage()}
              >
                Find jobs
              </button>

              <div className="quick-suggestions">
                <span className="suggestion-label">Try:</span>
                <button className="chip" onClick={() => openModalFor("engineer")}>engineer</button>
                <button className="chip" onClick={() => openModalFor("react")}>react</button>
                <button className="chip" onClick={() => openModalFor("hexaware")}>Hexaware</button>
              </div>
            </div>

            <div className="mt-3">
              <small className="text-muted">Tip: click a suggestion to preview results in a modal</small>
            </div>
          </div>

          <div className="hero-right">
            <img
              src="/illustrations/job-hero-illustration.svg"
              alt="Job illustration"
              className="hero-illustration"
              onError={(e) => { e.currentTarget.style.display = "none"; }}
            />
          </div>
        </div>
      </header>

 
      <main className="container featured-section">
        <div className="d-flex align-items-center justify-content-between mb-3">
          <h2 className="section-title">Featured jobs</h2>
          <a className="see-all" href="/jobs">See all jobs →</a>
        </div>

        <div className="featured-grid">
          {loadingFeatured ? (
            Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="job-card skeleton">
                <div className="logo-skel" />
                <div className="lines">
                  <div className="line short" />
                  <div className="line medium" />
                  <div className="line tiny" />
                </div>
              </div>
            ))
          ) : featured.length === 0 ? (
            <div className="text-muted">No featured jobs yet.</div>
          ) : (
            featured.map((j, idx) => (
              <article
                key={j.jobId ?? j.id}
                className="job-card fade-in"
                style={{ animationDelay: `${idx * 90}ms` }}
                tabIndex={0}
                aria-label={j.title}
              >
                <div className="job-left">
                  <div className="company-logo" aria-hidden>
                    {j.companyLogoUrl ? (
                      <img src={j.companyLogoUrl} alt={`${(j.employerName ?? j.company ?? "Company")} logo`} />
                    ) : (
                      ((j.employerName || j.company || "H").slice(0, 1) || "H").toUpperCase()
                    )}
                  </div>

                  <div className="job-meta-left">
                    <a className="job-title" href={`/jobs/${j.jobId ?? j.id}`}>
                      {j.title || j.jobTitle}
                    </a>
                    <div className="meta small text-muted">
                      {(j.employerName || j.company || "")} • {(j.location || "Remote")}
                    </div>

                    <div className="skill-list">
                      {(j.skillsRequired || "")
                        .split(",")
                        .map((s) => s.trim())
                        .filter(Boolean)
                        .slice(0, 4)
                        .map((s, idx2) => (
                          <span key={idx2} className="skill-chip">{s}</span>
                        ))}
                    </div>
                  </div>
                </div>

                <div className="job-right">
                  <div className="salary small muted">
                    {j.salary
                      ? Number(j.salary) >= 1000
                        ? Number(j.salary).toLocaleString("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 })
                        : j.salary
                      : "—"}
                  </div>
                  <div className="job-actions">
                    <a className="btn-outline" href={`/jobs/${j.jobId ?? j.id}`}>View</a>
                    <button className="btn-outline small" onClick={() => handleCopy(j.jobId ?? j.id)}>Copy link</button>
                  </div>
                </div>
              </article>
            ))
          )}
        </div>
      </main>

      <section className="container info-cards">
        <div className="card-grid">
          <div className="info-card">
            <h4>Employers</h4>
            <p>Post jobs, review applicants, and hire the best talent quickly.</p>
          </div>
          <div className="info-card">
            <h4>JobSeekers</h4>
            <p>Create a profile, upload resumes, and apply in one click.</p>
          </div>
          <div className="info-card">
            <h4>Safe & Simple</h4>
            <p>Your data is private and secure — we make hiring human.</p>
          </div>
        </div>
      </section>

     
      {modalOpen && (
        <div
          className="modal-backdrop"
          onClick={(e) => {
            if (e.target === e.currentTarget) closeModal();
          }}
        >
          <div className="modal-card" role="dialog" aria-modal="true" aria-label={`Search results for ${modalQuery}`} ref={modalRef}>
            <div className="modal-header">
              <h4>Search results <small className="text-muted">for “{modalQuery}”</small></h4>
              <button className="modal-close" onClick={closeModal} aria-label="Close modal">×</button>
            </div>

            <div className="modal-body">
              {modalLoading ? (
                <div className="modal-loading">Loading…</div>
              ) : modalError ? (
                <div className="text-danger">{modalError}</div>
              ) : modalJobs.length === 0 ? (
                <div className="text-muted">No jobs found for “{modalQuery}”.</div>
              ) : (
                <ul className="modal-job-list">
                  {modalJobs.map((m) => (
                    <li key={m.jobId ?? m.id} className="modal-job-item">
                      <div className="modal-job-left">
                        <div className="modal-job-title">{m.title}</div>
                        <div className="modal-job-meta small text-muted">{m.employerName ?? m.company} • {m.location ?? "Remote"}</div>
                      </div>
                      <div className="modal-job-actions">
                        <a href={`/jobs/${m.jobId ?? m.id}`} className="btn-outline small">View</a>
                        <button className="btn-outline small" onClick={() => handleCopy(m.jobId ?? m.id)}>Copy link</button>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="modal-footer">
              <button className="btn-primary" onClick={() => { closeModal(); navigate(`/jobs?q=${encodeURIComponent(modalQuery)}`); }}>
                View more results
              </button>
              <button className="btn-secondary" onClick={closeModal}>Close</button>
            </div>
          </div>
        </div>
      )}

     
      <ToastContainer />
    </div>
  );
}
