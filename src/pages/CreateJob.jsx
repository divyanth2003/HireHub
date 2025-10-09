
import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, Link } from "react-router-dom";
import jobService from "../services/jobService";
import useAuth from "../auth/useAuth";

export default function CreateJob() {
  const { user } = useAuth(); 
  const employerId = user?.employerId;

  const navigate = useNavigate();
  const { id } = useParams();
  const jobId = id ? Number(id) : null;

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const [form, setForm] = useState({
    title: "",
    description: "",
    location: "",
    salary: "",
    skills: "",
    academicEligibility: "",
    allowedBatches: "",
    backlogs: "",
    status: "Open",
  });

  const [touched, setTouched] = useState({});
  const [fieldErrors, setFieldErrors] = useState({});
  const [topMessage, setTopMessage] = useState(null); 

 
  const showToast = (text, type = "success", ttl = 3000) => {
    setTopMessage({ text, type });
    setTimeout(() => setTopMessage(null), ttl);
  };

  
  useEffect(() => {
    if (!jobId) return;
    setLoading(true);
    jobService
      .getById(jobId)
      .then((job) => {
        setForm({
          title: job.title ?? "",
          description: job.description ?? "",
          location: job.location ?? "",
          salary: job.salary != null ? String(job.salary) : "",
          skills: job.skillsRequired ?? "",
          academicEligibility: job.academicEligibility ?? "",
          allowedBatches: job.allowedBatches ?? "",
          backlogs: job.backlogs != null ? String(job.backlogs) : "",
          status: job.status ?? "Open",
        });
      })
      .catch((err) => {
        console.error("Failed loading job:", err);
        showToast("Failed to load job. See console.", "error", 4000);
      })
      .finally(() => setLoading(false));
  }, [jobId]);

  
  const errors = useMemo(() => {
    const e = {};

    if (!form.title || !form.title.trim()) e.title = "Title is required.";
    else if (form.title.trim().length < 3) e.title = "Title should be at least 3 characters.";

   
    if (!form.description || !form.description.trim()) e.description = "Please add a job description.";

    if (!form.location || !form.location.trim()) e.location = "Location is required.";

    
    if (form.salary === "" || form.salary === null) {
      e.salary = "Salary is required.";
    } else {
      const salaryNum = Number(form.salary);
      if (Number.isNaN(salaryNum) || salaryNum < 0) e.salary = "Enter a valid non-negative number.";
    }

    
    if (!form.skills || !form.skills.trim()) {
      e.skills = "Please specify at least one skill.";
    } else {
      const tokens = form.skills.split(",").map((s) => s.trim()).filter(Boolean);
      if (tokens.length === 0) e.skills = "Please specify at least one skill.";
    }

 
    if (!form.academicEligibility || !form.academicEligibility.trim())
      e.academicEligibility = "Specify academic eligibility.";

    
    if (!form.allowedBatches || !form.allowedBatches.trim()) {
      e.allowedBatches = "Specify allowed batches (comma separated).";
    } else {
      const years = form.allowedBatches.split(",").map((s) => s.trim()).filter(Boolean);
      if (years.length === 0) {
        e.allowedBatches = "Specify at least one year.";
      } else {
        const invalid = years.some((y) => !/^\d{4}$/.test(y));
        if (invalid) e.allowedBatches = "Allowed batches must be comma-separated 4-digit years (e.g. 2022, 2023).";
      }
    }


    if (form.backlogs === "" || form.backlogs === null) {
      e.backlogs = "Specify backlogs allowed (0 if none).";
    } else {
      const backlogsNum = Number(form.backlogs);
      if (Number.isNaN(backlogsNum) || backlogsNum < 0 || !Number.isInteger(backlogsNum))
        e.backlogs = "Backlogs must be a non-negative integer.";
    }

    return e;
  }, [form]);

  const isInvalid = (name) => touched[name] && !!errors[name];


  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));
  
    setFieldErrors((prev) => ({ ...prev, [name]: "" }));
  };
  const handleBlur = (e) => setTouched((t) => ({ ...t, [e.target.name]: true }));

  const mapBackendErrorsToFields = (data) => {
  
    const newFieldErrors = {};
    if (!data) return newFieldErrors;

    if (data.errors && typeof data.errors === "object") {
      Object.keys(data.errors).forEach((k) => {
        const plainKey = k.replace(/^createJobDto\./i, "").replace(/^CreateJobDto\./i, "");
        const camel = plainKey.charAt(0).toLowerCase() + plainKey.slice(1);
        newFieldErrors[camel] = Array.isArray(data.errors[k]) ? data.errors[k].join(" ") : String(data.errors[k]);
      });
    }
    return newFieldErrors;
  };

  const submit = async (e) => {
    e.preventDefault();
    if (!employerId) {
      showToast("EmployerId missing — login as Employer.", "error", 4000);
      return;
    }

    
    setTouched({
      title: true,
      description: true,
      location: true,
      salary: true,
      skills: true,
      academicEligibility: true,
      allowedBatches: true,
      backlogs: true,
    });

   
    setFieldErrors({});

    if (Object.keys(errors).length) {
   
      const first = Object.keys(errors)[0];
      const el = document.querySelector(`[name="${first}"]`);
      el?.focus?.();
      return;
    }

    const dto = {
      EmployerId: employerId,
      Title: form.title.trim(),
      Description: form.description.trim(),
      Location: form.location ? form.location.trim() : null,
      Salary: form.salary ? Number(form.salary) : null,
      SkillsRequired: form.skills ? form.skills.trim() : null,
      AcademicEligibility: form.academicEligibility ? form.academicEligibility.trim() : null,
      AllowedBatches: form.allowedBatches ? form.allowedBatches.trim() : null,
      Backlogs: form.backlogs ? Number(form.backlogs) : 0,
      Status: form.status || "Open",
    };

    setSaving(true);
    try {
      if (jobId) {
        await jobService.update(jobId, dto);
        showToast("Job updated successfully.", "success", 2000);
      } else {
        await jobService.create(dto);
        showToast("Job posted successfully.", "success", 2000);
      }
  
      setTimeout(() => navigate("/jobs"), 1200);
    } catch (err) {
      console.error("Create/Update failed:", err);

      const resp = err?.response?.data;
  
      const backendFieldErrors = mapBackendErrorsToFields(resp);
      if (Object.keys(backendFieldErrors).length) {
        setFieldErrors(backendFieldErrors);
        
        const first = Object.keys(backendFieldErrors)[0];
        const el = document.querySelector(`[name="${first}"]`);
        el?.focus?.();
        showToast("Fix highlighted errors.", "error", 3500);
      } else {
      
        const backendMsg = resp?.message || resp || err?.message || "Unknown error";
        showToast("Save failed: " + String(backendMsg), "error", 4000);
      }
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!jobId) return;
    if (!window.confirm("Delete this job? This cannot be undone.")) return;
    setDeleting(true);
    try {
      await jobService.remove(jobId);
      showToast("Job deleted.", "success", 2000);
      setTimeout(() => navigate("/jobs"), 1200);
    } catch (err) {
      console.error("Delete failed:", err);
      const backendMsg = err?.response?.data?.message || err?.response?.data || err?.message || "Unknown error";
      showToast("Delete failed: " + backendMsg, "error", 4000);
    } finally {
      setDeleting(false);
    }
  };

  const toggleStatus = async () => {
    if (!jobId) return;
    const newStatus = form.status === "Open" ? "Closed" : "Open";
    const dto = {
      Title: form.title,
      Description: form.description,
      Location: form.location || null,
      Salary: form.salary ? Number(form.salary) : null,
      SkillsRequired: form.skills || null,
      AcademicEligibility: form.academicEligibility || null,
      AllowedBatches: form.allowedBatches || null,
      Backlogs: form.backlogs ? Number(form.backlogs) : null,
      Status: newStatus,
    };
    setSaving(true);
    try {
      await jobService.update(jobId, dto);
      setForm((f) => ({ ...f, status: newStatus }));
      showToast(`Status updated to ${newStatus}`, "info", 2000);
    } catch (err) {
      console.error("Toggle status failed:", err);
      const backendMsg = err?.response?.data?.message || err?.message || "Unknown";
      showToast("Status update failed: " + backendMsg, "error", 4000);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="container py-5">
        <div className="placeholder-glow">
          <div className="placeholder col-6 mb-3" />
          <div className="placeholder col-12 mb-2" style={{ height: 140 }} />
          <div className="placeholder col-4 mb-2" />
          <div className="placeholder col-3 mb-2" />
          <div className="placeholder col-5 mb-2" />
        </div>
      </div>
    );
  }

  return (
    <div className="container py-4" style={{ maxWidth: 980 }}>

      <div style={{ position: "fixed", top: 12, right: 12, zIndex: 1200 }}>
        {topMessage && (
          <div
            className={`alert ${topMessage.type === "success" ? "alert-success" : topMessage.type === "error" ? "alert-danger" : "alert-info"}`}
            role="alert"
            style={{ minWidth: 240, boxShadow: "0 6px 20px rgba(0,0,0,0.06)" }}
          >
            {topMessage.text}
          </div>
        )}
      </div>

    
      <div className="d-flex align-items-center justify-content-between mb-3">
        <div>
          <nav aria-label="breadcrumb">
            <ol className="breadcrumb mb-1">
              <li className="breadcrumb-item"><Link to="/jobs">Jobs</Link></li>
              <li className="breadcrumb-item active" aria-current="page">
                {jobId ? "Edit Job" : "Create Job"}
              </li>
            </ol>
          </nav>
          <h3 className="mb-0">
            {jobId ? "Edit Job" : "Create Job"}
            {jobId && (
              <span
                className={`badge ms-2 ${
                  form.status === "Open" ? "bg-success-subtle text-success" : "bg-secondary-subtle text-secondary"
                }`}
                style={{ fontWeight: 600 }}
                title="Current status"
              >
                {form.status}
              </span>
            )}
          </h3>
        </div>

        <button className="btn btn-light" onClick={() => navigate(-1)}>
          ← Back
        </button>
      </div>

      <div className="card border-0 shadow-sm">
        <div className="card-body p-4">
          <form onSubmit={submit} noValidate>
      
            <div className="form-floating mb-3">
              <input
                required
                name="title"
                id="title"
                className={`form-control ${isInvalid("title") || fieldErrors.title ? "is-invalid" : ""}`}
                placeholder="Senior Software Engineer"
                value={form.title}
                onChange={handleChange}
                onBlur={handleBlur}
              />
              <label htmlFor="title">Title *</label>
              { (isInvalid("title") && <div className="invalid-feedback">{errors.title}</div>) ||
                (fieldErrors.title && <div className="invalid-feedback">{fieldErrors.title}</div>)}
            </div>

       
            <div className="form-floating mb-3">
              <textarea
                required
                name="description"
                id="description"
                className={`form-control ${isInvalid("description") || fieldErrors.description ? "is-invalid" : ""}`}
                placeholder="Describe the role"
                style={{ height: 160 }}
                value={form.description}
                onChange={handleChange}
                onBlur={handleBlur}
              />
              <label htmlFor="description">Description *</label>
              { (isInvalid("description") && <div className="invalid-feedback">{errors.description}</div>) ||
                (fieldErrors.description && <div className="invalid-feedback">{fieldErrors.description}</div>)}
              <div className="form-text">
                Tip: Include responsibilities, tech stack, and what success looks like.
              </div>
            </div>

           
            <div className="row g-3">
              <div className="col-md-4">
                <div className="form-floating">
                  <input
                    name="location"
                    id="location"
                    className={`form-control ${isInvalid("location") || fieldErrors.location ? "is-invalid" : ""}`}
                    placeholder="Bengaluru, Remote"
                    value={form.location}
                    onChange={handleChange}
                    onBlur={handleBlur}
                  />
                  <label htmlFor="location">Location *</label>
                  { (isInvalid("location") && <div className="invalid-feedback">{errors.location}</div>) ||
                    (fieldErrors.location && <div className="invalid-feedback">{fieldErrors.location}</div>)}
                </div>
                <div className="form-text">e.g., Bengaluru / Remote / Hybrid</div>
              </div>

              <div className="col-md-4">
                <div className="form-floating">
                  <input
                    name="salary"
                    id="salary"
                    type="number"
                    min="0"
                    className={`form-control ${isInvalid("salary") || fieldErrors.salary ? "is-invalid" : ""}`}
                    placeholder="800000"
                    value={form.salary}
                    onChange={handleChange}
                    onBlur={handleBlur}
                  />
                  <label htmlFor="salary">Salary (annual) *</label>
                  { (isInvalid("salary") && <div className="invalid-feedback">{errors.salary}</div>) ||
                    (fieldErrors.salary && <div className="invalid-feedback">{fieldErrors.salary}</div>)}
                </div>
                <div className="form-text">Numbers only; leave blank if not disclosed.</div>
              </div>

              <div className="col-md-4">
                <div className="form-floating">
                  <input
                    name="backlogs"
                    id="backlogs"
                    type="number"
                    min="0"
                    className={`form-control ${isInvalid("backlogs") || fieldErrors.backlogs ? "is-invalid" : ""}`}
                    placeholder="0"
                    value={form.backlogs}
                    onChange={handleChange}
                    onBlur={handleBlur}
                  />
                  <label htmlFor="backlogs">Backlogs allowed (int) *</label>
                  { (isInvalid("backlogs") && <div className="invalid-feedback">{errors.backlogs}</div>) ||
                    (fieldErrors.backlogs && <div className="invalid-feedback">{fieldErrors.backlogs}</div>)}
                </div>
              </div>
            </div>

      
            <div className="form-floating mt-3 mb-3">
              <input
                name="skills"
                id="skills"
                className={`form-control ${isInvalid("skills") || fieldErrors.skills ? "is-invalid" : ""}`}
                placeholder="React, Node, SQL"
                value={form.skills}
                onChange={handleChange}
                onBlur={handleBlur}
              />
              <label htmlFor="skills">Skills (comma separated) *</label>
              { (isInvalid("skills") && <div className="invalid-feedback">{errors.skills}</div>) ||
                (fieldErrors.skills && <div className="invalid-feedback">{fieldErrors.skills}</div>)}
              <div className="form-text">Example: React, Node.js, SQL, AWS</div>
            </div>

         
            <div className="row g-3">
              <div className="col-md-6">
                <div className="form-floating">
                  <input
                    name="academicEligibility"
                    id="academicEligibility"
                    className={`form-control ${isInvalid("academicEligibility") || fieldErrors.academicEligibility ? "is-invalid" : ""}`}
                    placeholder="60%+, No active backlogs"
                    value={form.academicEligibility}
                    onChange={handleChange}
                    onBlur={handleBlur}
                  />
                  <label htmlFor="academicEligibility">Academic Eligibility *</label>
                  { (isInvalid("academicEligibility") && <div className="invalid-feedback">{errors.academicEligibility}</div>) ||
                    (fieldErrors.academicEligibility && <div className="invalid-feedback">{fieldErrors.academicEligibility}</div>)}
                </div>
              </div>
              <div className="col-md-6">
                <div className="form-floating">
                  <input
                    name="allowedBatches"
                    id="allowedBatches"
                    className={`form-control ${isInvalid("allowedBatches") || fieldErrors.allowedBatches ? "is-invalid" : ""}`}
                    placeholder="2022, 2023, 2024"
                    value={form.allowedBatches}
                    onChange={handleChange}
                    onBlur={handleBlur}
                  />
                  <label htmlFor="allowedBatches">Allowed Batches *</label>
                  { (isInvalid("allowedBatches") && <div className="invalid-feedback">{errors.allowedBatches}</div>) ||
                    (fieldErrors.allowedBatches && <div className="invalid-feedback">{fieldErrors.allowedBatches}</div>)}
                </div>
                <div className="form-text">Example: 2022, 2023, 2024</div>
              </div>
            </div>

         
            <div className="d-flex flex-wrap gap-2 mt-4">
              <button className="btn btn-primary" type="submit" disabled={saving}>
                {saving ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-2" role="status" />
                    {jobId ? "Saving…" : "Creating…"}
                  </>
                ) : (
                  <>{jobId ? "Save Changes" : "Post Job"}</>
                )}
              </button>

              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={() => navigate(-1)}
                disabled={saving || deleting}
              >
                Cancel
              </button>

              {jobId && (
                <>
                  <button
                    type="button"
                    className="btn btn-outline-dark"
                    onClick={toggleStatus}
                    disabled={saving}
                    title="Toggle job visibility"
                  >
                    {saving ? "Updating…" : `Mark ${form.status === "Open" ? "Closed" : "Open"}`}
                  </button>

                  <button
                    type="button"
                    className="btn btn-danger ms-auto"
                    onClick={handleDelete}
                    disabled={deleting}
                  >
                    {deleting ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2" role="status" />
                        Deleting…
                      </>
                    ) : (
                      "Delete"
                    )}
                  </button>
                </>
              )}
            </div>
          </form>
        </div>
      </div>

   
      <p className="text-muted small mt-3 mb-0">
        Fields marked * are required. You can edit status after posting.
      </p>
    </div>
  );
}
