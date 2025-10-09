import React, { useEffect, useState } from "react";
import resumeService from "../services/resumeService";
import jobSeekerService from "../services/jobSeekerService";
import useAuth from "../auth/useAuth";

export default function ResumeUpload() {
  const { user } = useAuth();
  const [file, setFile] = useState(null);
  const [resumes, setResumes] = useState([]);
  const [loading, setLoading] = useState(false);
  const [jobSeekerId, setJobSeekerId] = useState(user?.jobSeekerId || null);
  const [resolving, setResolving] = useState(false);

  const [topMessage, setTopMessage] = useState(null);
  const showToast = (text, type = "success", ttl = 3000) => {
    setTopMessage({ text, type });
    window.setTimeout(() => setTopMessage(null), ttl);
  };

  useEffect(() => {
    if (jobSeekerId) return;
    if (!user?.userId) return;
    setResolving(true);
    jobSeekerService
      .getByUser(user.userId)
      .then((js) => {
        if (js?.jobSeekerId) setJobSeekerId(js.jobSeekerId);
      })
      .catch((err) => {
        console.error("Failed resolving jobSeeker:", err);
        showToast("Failed to resolve profile.", "error", 4000);
      })
      .finally(() => setResolving(false));
  }, [user?.userId, jobSeekerId]);

  useEffect(() => {
    if (!jobSeekerId) return;
    loadResumes();
  }, [jobSeekerId]);

  const loadResumes = async () => {
    try {
      const data = await resumeService.getByJobSeeker(jobSeekerId);
      setResumes(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error("Failed loading resumes", err);
      showToast("Failed loading resumes.", "error", 4000);
    }
  };

  // helper: isPdf (uses MIME type first, falls back to filename extension)
  const isPdfFile = (f) => {
    if (!f) return false;
    if (f.type) {
      // Some browsers/OS may not set type; check extension as fallback
      if (f.type === "application/pdf") return true;
      // handle any vendor variants (rare)
      if (f.type.toLowerCase().includes("pdf")) return true;
    }
    const name = (f.name || "").toLowerCase();
    return name.endsWith(".pdf");
  };

  const onFileChange = (e) => {
    const sel = e.target.files[0] || null;
    if (!sel) {
      setFile(null);
      return;
    }

    if (!isPdfFile(sel)) {
      // Reject non-pdf files
      showToast("Only PDF files are allowed. Please select a .pdf resume.", "error", 4000);
      // Clear the file input value so user can pick again
      e.target.value = "";
      setFile(null);
      return;
    }

    // Optional: you can also validate size here (e.g., < 5MB)
    // const maxBytes = 5 * 1024 * 1024;
    // if (sel.size > maxBytes) { ... }

    setFile(sel);
  };

  const handleUpload = async (e) => {
    e.preventDefault();
    if (!file) {
      showToast("Please select a PDF file first.", "error", 3000);
      return;
    }
    if (!isPdfFile(file)) {
      showToast("Selected file is not a valid PDF.", "error", 3500);
      setFile(null);
      return;
    }
    if (!jobSeekerId) {
      showToast("JobSeeker profile missing.", "error", 3500);
      return;
    }
    setLoading(true);
    try {
      const fd = new FormData();
      fd.append("file", file);
      fd.append("jobSeekerId", jobSeekerId);
      fd.append("resumeName", file.name);
      // normalize fileType to 'pdf'
      fd.append("fileType", "pdf");

      await resumeService.upload(fd);
      showToast("Resume uploaded successfully.", "success", 3000);
      setFile(null);
      await loadResumes();
    } catch (err) {
      console.error("Upload failed:", err);
      const msg = err?.response?.data?.message || err.message || "Upload failed";
      showToast(String(msg), "error", 5000);
    } finally {
      setLoading(false);
    }
  };

  const handleSetDefault = async (id, e) => {
    if (e?.preventDefault) e.preventDefault();
    try {
      await resumeService.setDefault(jobSeekerId, id);
      await loadResumes();
      showToast("Set as default resume.", "success", 2500);
    } catch (err) {
      console.error("Failed to set default", err);
      showToast("Failed to set default.", "error", 3500);
    }
  };

  const handleDelete = async (id, e) => {
    if (e?.preventDefault) e.preventDefault();
    if (!window.confirm("Delete this resume?")) return;
    try {
      await resumeService.remove(id);
      await loadResumes();
      showToast("Deleted resume.", "success", 2500);
    } catch (err) {
      console.error("Delete failed:", err);
      const msg = err?.response?.data?.message || err.message || "Delete failed";
      showToast(String(msg), "error", 4000);
    }
  };

  if (!user) return <div className="container mt-4">Please login as JobSeeker.</div>;
  if (resolving) return <div className="container mt-4">Loading…</div>;
  if (!jobSeekerId) return <div className="container mt-4">No JobSeeker profile yet.</div>;

  const origin = window.location.origin;

  return (
    <div className="container mt-4">
      <div style={{ position: "fixed", top: 12, right: 12, zIndex: 1200 }}>
        {topMessage && (
          <div
            className={`alert ${
              topMessage.type === "success" ? "alert-success" : topMessage.type === "error" ? "alert-danger" : "alert-info"
            }`}
            role="alert"
            style={{ minWidth: 220, boxShadow: "0 6px 20px rgba(0,0,0,0.06)" }}
          >
            {topMessage.text}
          </div>
        )}
      </div>

      <h3>Your Resumes</h3>

      <form onSubmit={handleUpload} className="mb-3">
        {/* accept hints to file picker; validation also enforced in onFileChange */}
        <input type="file" accept="application/pdf,.pdf" onChange={onFileChange} />
        <button className="btn btn-primary ms-2" type="submit" disabled={loading}>
          {loading ? "Uploading..." : "Upload"}
        </button>
      </form>

      <hr />

      {resumes.length === 0 ? (
        <div>No resumes uploaded yet.</div>
      ) : (
        <ul className="list-group">
          {resumes.map((r) => (
            <li className="list-group-item d-flex justify-content-between align-items-start" key={r.resumeId || r.id}>
              <div style={{ flex: "1 1 auto", minWidth: 0 }}>
                <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
                  <strong style={{ whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{r.resumeName}</strong>
                  {r.isDefault && <span className="badge bg-success ms-2">Default</span>}
                </div>

                <small className="text-muted d-block" style={{ marginTop: 6 }}>
                  {r.fileType ? r.fileType.toUpperCase() : "PDF"} • updated {new Date(r.updatedAt).toLocaleString()}
                </small>

                {r.filePath && (
                  <div style={{ marginTop: 6 }}>
                    <a
                      href={r.filePath.startsWith("http") ? r.filePath : `${origin}/${r.filePath.replace(/^\/+/, "")}`}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      Download
                    </a>
                  </div>
                )}
              </div>

              <div style={{ marginLeft: 12, display: "flex", gap: 8 }}>
                {!r.isDefault && (
                  <button type="button" className="btn btn-sm btn-outline-primary" onClick={(e) => handleSetDefault(r.resumeId || r.id, e)}>
                    Set Default
                  </button>
                )}
                <button type="button" className="btn btn-sm btn-outline-danger" onClick={(e) => handleDelete(r.resumeId || r.id, e)}>
                  Delete
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
