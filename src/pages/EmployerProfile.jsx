
import React, { useEffect, useState } from "react";
import { toast } from "react-toastify";
import employerService from "../services/employerService";
import userService from "../services/userService";
import jobService from "../services/jobService";
import applicationService from "../services/applicationService";
import useAuth from "../auth/useAuth";
import "react-toastify/dist/ReactToastify.css";


function toInputDate(value) {
  if (!value) return "";
  try {
    let raw = value;
    if (typeof value === "object") {
      raw = value.dateOfBirth ?? value.dob ?? value.Dob ?? "";
    }
    if (!raw || typeof raw !== "string") return "";


    if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) return raw;

    const d = new Date(raw);
    if (!isFinite(d.getTime())) return "";

    const y = d.getUTCFullYear();
   
    if (y < 1900 || y > 9999) return "";

    const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
    const dd = String(d.getUTCDate()).padStart(2, "0");
    return `${y}-${mm}-${dd}`;
  } catch {
    return "";
  }
}

function isValidYMD(ymd) {
  return typeof ymd === "string" && /^\d{4}-\d{2}-\d{2}$/.test(ymd);
}
function ymdToPlain(ymd) {
  if (!isValidYMD(ymd)) return null;
  return ymd;
}

export default function EmployerProfile() {
  const { user, setUserLocal } = useAuth();


  const TOAST_OPTS = {
    position: "bottom-right",
    autoClose: 1800,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    theme: "light",
    style: {
      borderRadius: 8,
      border: "1px solid rgba(13,110,253,0.12)",
      background: "#f3f8ff",
      color: "#0446b6"
    }
  };

  const [initialLoading, setInitialLoading] = useState(true);
  const [loading, setLoading] = useState(false);
  const [profile, setProfile] = useState(null);
  const [message, setMessage] = useState(null); 

  const [form, setForm] = useState({
    companyName: "",
    contactInfo: "",
    position: "",
    fullName: "",
    email: "",
    dateOfBirth: "",
    gender: "",
    address: ""
  });

  const [jobsCount, setJobsCount] = useState(0);
  const [appsCount, setAppsCount] = useState(0);

  useEffect(() => {
    let mounted = true;
    async function load() {
      if (!user?.userId) {
        setInitialLoading(false);
        return;
      }
      setInitialLoading(true);

      try {
       
        const u = await userService.getById(user.userId).catch(() => null);
       
        const emp = await employerService.getByUser(user.userId).catch(() => null);

        if (!mounted) return;

        if (emp) {
          setProfile(emp);
          setForm((f) => ({
            ...f,
            companyName: emp.companyName ?? "",
            contactInfo: emp.contactInfo ?? "",
            position: emp.position ?? ""
          }));
        }

        if (u) {
          setForm((f) => ({
            ...f,
            fullName: u.fullName ?? f.fullName,
            email: u.email ?? f.email,
            dateOfBirth: toInputDate(u.dateOfBirth ?? u.dob ?? u.Dob),
            gender: u.gender ?? f.gender,
            address: u.address ?? f.address
          }));
        } else {
         
          setForm((f) => ({ ...f, email: user?.userEmail ?? user?.email ?? f.email, fullName: user?.userFullName ?? f.fullName }));
        }

        
        try {
          const myJobs = await jobService.getByEmployer(emp?.employerId ?? user.userId).catch(() => []);
          setJobsCount(Array.isArray(myJobs) ? myJobs.length : 0);
          let totalApps = 0;
          if (Array.isArray(myJobs)) {
            for (const j of myJobs) {
              try {
                const apps = await applicationService.getByJob(j.jobId);
                totalApps += Array.isArray(apps) ? apps.length : 0;
              } catch {}
            }
          }
          setAppsCount(totalApps);
        } catch {}
      } catch (err) {
        console.error("Failed to load employer/profile:", err);
        setMessage({ type: "error", text: "Failed to load profile data." });
      } finally {
        if (mounted) setInitialLoading(false);
      }
    }

    load();
    return () => { mounted = false; };
  }, [user?.userId]);

  
  useEffect(() => {
    if (!message) return;
    const t = setTimeout(() => setMessage(null), 4500);
    return () => clearTimeout(t);
  }, [message]);

  const updateField = (name, value) => setForm((s) => ({ ...s, [name]: value }));

  async function handleSave(e) {
    e?.preventDefault();
    if (!user) {
      toast.error("Please sign in.", TOAST_OPTS);
      return;
    }
    if (!form.companyName || !form.companyName.trim()) {
      toast.error("Company name is required.", TOAST_OPTS);
      return;
    }

   
    if (!isValidYMD(form.dateOfBirth)) {
      toast.error("Please enter a valid Date of birth (YYYY-MM-DD).", TOAST_OPTS);
      return;
    }

    setLoading(true);
    try {

      const empDto = {
        companyName: form.companyName,
        contactInfo: form.contactInfo,
        position: form.position
      };

      let savedEmp;
      if (profile?.employerId) {
        savedEmp = await employerService.update(profile.employerId, empDto);
      } else {
      
        try {
          savedEmp = await employerService.create({ userId: user.userId, ...empDto });
        } catch (createErr) {
          const status = createErr?.response?.status;
          if (status === 409) {
            
            const existing = await employerService.getByUser(user.userId);
            if (existing) {
              savedEmp = await employerService.update(existing.employerId ?? existing.employerID, empDto);
            } else {
              throw createErr;
            }
          } else {
            throw createErr;
          }
        }
      }
      setProfile(savedEmp);

  
      const userDto = {
        fullName: form.fullName || undefined,
        address: form.address || undefined,
        gender: form.gender || undefined,
        role: "Employer"
      };

      const dobPlain = ymdToPlain(form.dateOfBirth);
      if (dobPlain) userDto.dateOfBirth = dobPlain;

      await userService.update(user.userId, userDto);

   
      if (typeof setUserLocal === "function") {
        setUserLocal({
          userFullName: userDto.fullName ?? form.fullName ?? user?.userFullName,
          role: "Employer"
        });
      }

     
      toast.success("Profile saved successfully.", TOAST_OPTS);
    } catch (err) {
      console.error("Save failed:", err);
      const txt = err?.response?.data?.message || err?.message || "Save failed";
      toast.error(txt, TOAST_OPTS);
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete() {
    if (!profile?.employerId) {
      toast.error("No employer profile to delete.", TOAST_OPTS);
      return;
    }
    if (!window.confirm("Delete employer profile? This action cannot be undone.")) return;

    setLoading(true);
    try {
      await employerService.remove(profile.employerId);
      setProfile(null);
      setForm((f) => ({ ...f, companyName: "", contactInfo: "", position: "" }));
      toast.success("Employer profile deleted.", TOAST_OPTS);
    } catch (err) {
      console.error("Delete failed:", err);
      toast.error("Delete failed.", TOAST_OPTS);
    } finally {
      setLoading(false);
    }
  }

  if (!user?.userId) return <div className="container mt-4">Please sign in to manage your employer profile.</div>;
  if (initialLoading) return <div className="container mt-4">Loading…</div>;

  return (
    <div className="container mt-4 employer-profile-page">
      <div className="d-flex justify-content-between align-items-start mb-3 profile-header">
        <div>
   
          <h2 className="mb-0">
            {form.fullName?.trim()
              || user?.userFullName
              || profile?.companyName
              || form.companyName
              || "Employer"}
          </h2>
          <div className="text-muted">Manage your company details and contact person</div>
        </div>
        <div className="text-end small">
          <div>Jobs: <strong>{jobsCount}</strong></div>
          <div>Applications: <strong>{appsCount}</strong></div>
        </div>
      </div>

      {message && (
        <div className={`alert ${message.type === "error" ? "alert-danger" : "alert-success"}`}>
          {message.text}
        </div>
      )}

      <div className="card p-3 mb-3">
        <form onSubmit={handleSave}>
          <div className="row">
            <div className="col-md-7">
              <div className="mb-3">
                <label className="form-label">Company Name *</label>
                <input
                  name="companyName"
                  className="form-control"
                  value={form.companyName}
                  onChange={(e) => updateField("companyName", e.target.value)}
                  required
                />
              </div>

              <div className="mb-3">
                <label className="form-label">Contact Info (optional)</label>
                <input
                  name="contactInfo"
                  className="form-control"
                  value={form.contactInfo}
                  onChange={(e) => updateField("contactInfo", e.target.value)}
                  placeholder="Phone, alternate email, address..."
                />
              </div>

              <div className="mb-3">
                <label className="form-label">Position / Title</label>
                <input
                  name="position"
                  className="form-control"
                  value={form.position}
                  onChange={(e) => updateField("position", e.target.value)}
                />
              </div>
            </div>

            <div className="col-md-5">
              <div className="mb-3">
                <label className="form-label">Registered Email</label>
                <input className="form-control" value={form.email} readOnly />
                <div className="form-text">This is the email you registered with (read-only).</div>
              </div>
            </div>
          </div>

          <hr />

          <h6>Account / Contact person</h6>

          <div className="row">
            <div className="col-md-6 mb-3">
              <label className="form-label">Full name</label>
              <input
                name="fullName"
                className="form-control"
                value={form.fullName}
                onChange={(e) => updateField("fullName", e.target.value)}
              />
            </div>

            <div className="col-md-3 mb-3">
              <label className="form-label">Date of birth</label>
              <input
                type="date"
                name="dateOfBirth"
                className="form-control"
                value={form.dateOfBirth ?? ""}
                onChange={(e) => updateField("dateOfBirth", e.target.value)}
              />
              <div className="form-text">Format: YYYY-MM-DD</div>
            </div>

            <div className="col-md-3 mb-3">
              <label className="form-label">Gender</label>
              <select
                className="form-control"
                value={form.gender ?? ""}
                onChange={(e) => updateField("gender", e.target.value)}
              >
                <option value="">Select</option>
                <option>Male</option>
                <option>Female</option>
                <option>Other</option>
              </select>
            </div>

            <div className="col-12 mb-3">
              <label className="form-label">Address</label>
              <input
                name="address"
                className="form-control"
                value={form.address}
                onChange={(e) => updateField("address", e.target.value)}
              />
            </div>
          </div>

         
          <div className="mb-3 form-actions">
            <button className="btn btn-primary me-2" type="submit" disabled={loading}>
              {loading ? "Saving..." : "Save Changes"}
            </button>
            {profile && (
              <button type="button" className="btn btn-danger me-2" onClick={handleDelete} disabled={loading}>
                {loading ? "Deleting..." : "Delete Profile"}
              </button>
            )}
            <button type="button" className="btn btn-secondary" onClick={() => window.location.reload()}>
              Refresh
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
