
import React, { useEffect, useState, useRef } from "react";
import { useNavigate } from "react-router-dom";
import jobSeekerService from "../services/jobSeekerService";
import userService from "../services/userService";
import useAuth from "../auth/useAuth";
import toast from "../lib/toast"; 
import "../styles/jobseekerprofile.css";


function isValidYMD(value) {
  if (!value || typeof value !== "string") return false;
  const m = value.match(/^([1-9]\d{3})-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/);
  if (!m) return false;
  const y = Number(m[1]), mm = Number(m[2]), dd = Number(m[3]);
  const d = new Date(`${y}-${String(mm).padStart(2, "0")}-${String(dd).padStart(2, "0")}T00:00:00Z`);
  return !Number.isNaN(d.getTime());
}

function toInputDate(serverDob) {
  if (!serverDob) return "";
  try {
    let raw = serverDob;
    if (typeof serverDob === "object") {
      raw = serverDob.dateOfBirth ?? serverDob.dob ?? serverDob.Dob ?? "";
    }
    if (!raw || typeof raw !== "string") return "";
    if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) return raw;
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) return "";
    const y = d.getUTCFullYear();
    if (y < 1900 || y > 9999) return "";
    const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
    const dd = String(d.getUTCDate()).padStart(2, "0");
    return `${y}-${mm}-${dd}`;
  } catch {
    return "";
  }
}

function ymdToPlain(ymd) {
  if (!isValidYMD(ymd)) return null;
  return ymd;
}

export default function JobSeekerProfile() {
  const { user, setUserLocal } = useAuth();
  const navigate = useNavigate();

  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const [form, setForm] = useState({
    educationDetails: "",
    skills: "",
    college: "",
    workStatus: "",
    experience: "",
    fullName: "",
    email: "",
    dateOfBirth: "",
    gender: "",
    address: ""
  });

  const [skillChips, setSkillChips] = useState([]);
  const skillInputRef = useRef(null);

  
  useEffect(() => {
    if (!user?.userId) return;
    let mounted = true;
    setLoading(true);

    Promise.allSettled([
      userService.getById(user.userId).catch(() => null),
      jobSeekerService.getByUser(user.userId).catch(() => null)
    ])
      .then(([uRes, jsRes]) => {
        if (!mounted) return;

        const u = uRes?.value || null;
        const js = jsRes?.value || null;

        if (js) {
          setProfile(js);
          setForm((f) => ({
            ...f,
            educationDetails: js.educationDetails ?? "",
            skills: js.skills ?? "",
            college: js.college ?? "",
            workStatus: js.workStatus ?? "",
            experience: js.experience ?? ""
          }));
          setSkillChips((js.skills ?? "").split(",").map(s => s.trim()).filter(Boolean));
        }

        if (u) {
          const dobVal = toInputDate(u.dateOfBirth ?? u.dob ?? u.Dob);
          setForm((f) => ({
            ...f,
            fullName: u.fullName ?? f.fullName,
            email: u.email ?? f.email,
            dateOfBirth: dobVal || f.dateOfBirth,
            gender: u.gender ?? f.gender,
            address: u.address ?? f.address
          }));
        }
      })
      .catch((err) => {
        console.error("Failed to load profile:", err);
        toast.error("Failed to load profile.");
      })
      .finally(() => mounted && setLoading(false));

    return () => (mounted = false);
  }, [user?.userId]);

  const updateField = (name, value) => setForm((f) => ({ ...f, [name]: value }));

  const addSkillFromInput = (raw) => {
    const s = (raw || "").trim();
    if (!s) return;
    const parts = s.split(",").map(p => p.trim()).filter(Boolean);
    setSkillChips(prev => {
      const merged = [...prev];
      parts.forEach(p => { if (!merged.includes(p)) merged.push(p); });
      updateField("skills", merged.join(","));
      return merged;
    });
    if (skillInputRef.current) skillInputRef.current.value = "";
  };

  const onSkillKeyDown = (e) => {
    if (e.key === "Enter" || e.key === ",") {
      e.preventDefault();
      addSkillFromInput(e.target.value);
    }
  };

  const removeSkill = (idx) => {
    setSkillChips(prev => {
      const copy = prev.slice();
      copy.splice(idx, 1);
      updateField("skills", copy.join(","));
      return copy;
    });
  };

  const handleRefresh = async () => {
    if (!user?.userId) return;
    setLoading(true);
    try {
      const [u, js] = await Promise.all([
        userService.getById(user.userId).catch(() => null),
        jobSeekerService.getByUser(user.userId).catch(() => null)
      ]);

      if (js) {
        setProfile(js);
        setForm((f) => ({
          ...f,
          educationDetails: js.educationDetails ?? "",
          skills: js.skills ?? "",
          college: js.college ?? "",
          workStatus: js.workStatus ?? "",
          experience: js.experience ?? ""
        }));
        setSkillChips((js.skills ?? "").split(",").map(s => s.trim()).filter(Boolean));
      }

      if (u) {
        const dobVal = toInputDate(u.dateOfBirth ?? u.dob ?? u.Dob);
        setForm((f) => ({
          ...f,
          fullName: u.fullName ?? f.fullName,
          email: u.email ?? f.email,
          dateOfBirth: dobVal || f.dateOfBirth,
          gender: u.gender ?? f.gender,
          address: u.address ?? f.address
        }));
      }

      toast.info("Profile refreshed.");
    } catch (err) {
      console.error("Refresh failed:", err);
      toast.error("Refresh failed.");
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!user) return toast.error("Please login to save your profile.");
    if (!form.fullName?.trim()) return toast.error("Full name is required.");

    setSaving(true);
    try {
      const userPayload = {
        fullName: String(form.fullName ?? ""),
        role: user.role ?? "JobSeeker",
        gender: String(form.gender ?? ""),
        address: String(form.address ?? "")
      };
      if (isValidYMD(form.dateOfBirth))
        userPayload.dateOfBirth = ymdToPlain(form.dateOfBirth);

      await userService.update(user.userId, userPayload);

      if (typeof setUserLocal === "function")
        setUserLocal({ userFullName: userPayload.fullName, userEmail: form.email ?? undefined });

      const jobPayload = {
        educationDetails: String(form.educationDetails ?? ""),
        skills: String(skillChips.join(",")),
        college: String(form.college ?? ""),
        workStatus: String(form.workStatus ?? ""),
        experience: String(form.experience ?? "0")
      };

      if (profile?.jobSeekerId)
        await jobSeekerService.update(profile.jobSeekerId, jobPayload);
      else
        await jobSeekerService.create({ userId: user.userId, ...jobPayload });

      toast.success("Profile saved successfully.");
      await handleRefresh();
    } catch (err) {
      console.error("Save failed:", err);
      toast.error("Save failed — check console.");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!profile?.jobSeekerId) return toast.error("No profile to delete.");
    if (!window.confirm("Delete your jobseeker profile? This cannot be undone.")) return;
    setSaving(true);
    try {
      await jobSeekerService.remove(profile.jobSeekerId);
      setProfile(null);
      setForm({
        educationDetails: "",
        skills: "",
        college: "",
        workStatus: "",
        experience: "",
        fullName: form.fullName,
        email: form.email,
        dateOfBirth: form.dateOfBirth,
        gender: form.gender,
        address: form.address
      });
      setSkillChips([]);
      toast.success("JobSeeker profile deleted.");
    } catch (err) {
      console.error("Delete failed:", err);
      toast.error("Delete failed.");
    } finally {
      setSaving(false);
    }
  };

  if (!user) return <div className="container mt-4">Please log in to edit your profile.</div>;

  const completeness = Math.min(100,
    Math.round(
      (form.educationDetails ? 30 : 0) +
      (skillChips.length ? 25 : 0) +
      (form.college ? 20 : 0) +
      (form.workStatus ? 15 : 0) +
      (form.experience ? 10 : 0)
    )
  );

  return (
    <div className="profile-page container">
      <div className="profile-header">
        <div className="profile-left">
          <div className="avatar">{(user.userFullName || user.email || "U").charAt(0).toUpperCase()}</div>
          <div>
            <h2 className="mb-0">{user.userFullName ?? (user.email?.split("@")[0] ?? "JobSeeker")}</h2>
            <div className="muted">Manage your public jobseeker profile</div>
          </div>
        </div>

        <div className="profile-right">
          <div className="profile-complete">
            <div className="progress-label">Profile completeness</div>
            <div className="progress-bar-outer" aria-hidden>
              <div className="progress-bar-inner" style={{ width: `${completeness}%` }} />
            </div>
            <div className="progress-percent">{completeness}%</div>
          </div>

          <div className="profile-actions">
            <button className="btn btn-outline-secondary" onClick={handleRefresh} disabled={loading}>Refresh</button>
            <button className="btn btn-primary" onClick={() => navigate("/resumes")}>Manage resumes</button>
          </div>
        </div>
      </div>

      <div className="profile-form card">
        <div className="form-row">
          <h5>Account</h5>
        </div>

        <div className="form-row">
          <label>Full name</label>
          <input className="form-control" value={form.fullName} onChange={(e) => updateField("fullName", e.target.value)} placeholder="Your display name" />
        </div>

        <div className="two-col">
          <div className="form-row">
            <label>Email</label>
            <input className="form-control" value={form.email} onChange={(e) => updateField("email", e.target.value)} />
          </div>

          <div className="form-row">
            <label>Gender</label>
            <select className="form-control" value={form.gender} onChange={(e) => updateField("gender", e.target.value)}>
              <option value="">Select</option>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
              <option value="Other">Other</option>
            </select>
          </div>
        </div>

        <div className="two-col">
          <div className="form-row">
            <label>Date of birth</label>
            <input type="date" className="form-control" name="dateOfBirth" value={form.dateOfBirth} onChange={(e) => updateField("dateOfBirth", e.target.value)} />
          </div>

          <div className="form-row">
            <label>Address</label>
            <input className="form-control" value={form.address} onChange={(e) => updateField("address", e.target.value)} />
          </div>
        </div>

        <hr style={{ margin: "14px 0" }} />

        <div className="form-row">
          <label>Education details</label>
          <input className="form-control" value={form.educationDetails} onChange={(e) => updateField("educationDetails", e.target.value)} placeholder="e.g. BE Computer Science" />
        </div>

        <div className="form-row">
          <label>Skills (press Enter to add)</label>
          <div className="skills-row">
            <div className="chips">
              {skillChips.map((s, i) => (
                <button key={s + i} type="button" className="chip" onClick={() => removeSkill(i)} aria-label={`Remove ${s}`}>
                  {s} <span className="x">×</span>
                </button>
              ))}
            </div>
            <input
              ref={skillInputRef}
              className="form-control skill-input"
              placeholder="Type a skill and press Enter (or comma)"
              onKeyDown={onSkillKeyDown}
            />
          </div>
          <div className="muted small">Tip: separate skills by pressing Enter or comma.</div>
        </div>

        <div className="two-col">
          <div className="form-row">
            <label>College</label>
            <input className="form-control" value={form.college} onChange={(e) => updateField("college", e.target.value)} />
          </div>

          <div className="form-row">
            <label>Work status</label>
            <select className="form-control" value={form.workStatus} onChange={(e) => updateField("workStatus", e.target.value)}>
              <option value="">Select status</option>
              <option value="Open to work">Open to work</option>
              <option value="Employed">Employed</option>
              <option value="Not looking">Not looking</option>
            </select>
          </div>
        </div>

        <div className="two-col">
          <div className="form-row">
            <label>Experience (years)</label>
            <input type="number" min="0" step="0.1" className="form-control" value={form.experience} onChange={(e) => updateField("experience", e.target.value)} />
          </div>

          <div className="form-row">
            <label>&nbsp;</label>
            <div className="muted small">Leave extra fields blank if unknown.</div>
          </div>
        </div>

        <div className="form-actions">
          <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? "Saving…" : (profile ? "Update Profile" : "Create Profile")}</button>
          {profile && <button className="btn btn-danger" onClick={handleDelete} disabled={saving}>Delete</button>}
        </div>
      </div>
    </div>
  );
}
