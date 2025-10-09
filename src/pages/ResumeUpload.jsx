
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

  const onFileChange = (e) => setFile(e.target.files[0] || null);

  const handleUpload = async (e) => {
    e.preventDefault();
    if (!file) {
      showToast("Please select a file first.", "error", 3000);
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
      fd.append("fileType", (file.type || "").split("/").pop() || "");

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
        <input type="file" onChange={onFileChange} />
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
            <li
              className="list-group-item d-flex justify-content-between align-items-start"
              key={r.resumeId || r.id}
            >
              <div style={{ flex: "1 1 auto", minWidth: 0 }}>
                <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
                  <strong style={{ whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                    {r.resumeName}
                  </strong>
                  {r.isDefault && <span className="badge bg-success ms-2">Default</span>}
                </div>

                <small className="text-muted d-block" style={{ marginTop: 6 }}>
                  {r.fileType} • updated {new Date(r.updatedAt).toLocaleString()}
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
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-primary"
                    onClick={(e) => handleSetDefault(r.resumeId || r.id, e)}
                  >
                    Set Default
                  </button>
                )}
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger"
                  onClick={(e) => handleDelete(r.resumeId || r.id, e)}
                >
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
