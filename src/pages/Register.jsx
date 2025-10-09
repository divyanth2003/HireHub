
import React, { useState, useRef, useEffect } from "react";
import { useNavigate, Link } from "react-router-dom";
import api from "../api/api"; 
import "../styles/Register.css";

const initialForm = {
  fullName: "",
  email: "",
  password: "",
  confirmPassword: "",
  role: "JobSeeker",
  dateOfBirth: "",
  gender: "",
  address: ""
};

function simplePasswordScore(pw = "") {
  let score = 0;
  if (pw.length >= 8) score++;
  if (/[A-Z]/.test(pw)) score++;
  if (/[0-9]/.test(pw)) score++;
  if (/[^A-Za-z0-9]/.test(pw)) score++;
  return score; // 0..4
}

export default function Register() {
  const nav = useNavigate();
  const [form, setForm] = useState(initialForm);
  const [loading, setLoading] = useState(false);
  const [topError, setTopError] = useState("");
  const [fieldErrors, setFieldErrors] = useState({});
  const [successMsg, setSuccessMsg] = useState("");
  const formRef = useRef(null);

  useEffect(() => {
    setTopError("");
    setFieldErrors({});
  }, []);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));
    setFieldErrors((p) => ({ ...p, [name]: "" }));
    setTopError("");
  };

  const setRole = (role) => {
    setForm((f) => ({ ...f, role }));
    setFieldErrors((p) => ({ ...p, role: "" }));
  };

  const resetForm = () => {
    setForm(initialForm);
    setFieldErrors({});
    setTopError("");
    setSuccessMsg("");
    if (formRef.current) formRef.current.reset();
  };

  const isValidEmail = (email) => /^\S+@\S+\.\S+$/.test(email);


  const convertDobToIso = (val) => {
    if (!val) return undefined;
    const ok = /^\d{4}-\d{2}-\d{2}$/.test(val);
    if (!ok) return undefined;
    
    const dt = new Date(val + "T00:00:00Z");
    return dt.toISOString();
  };

  const validate = () => {
    const errors = {};
    if (!form.fullName?.trim()) errors.fullName = "Full name is required";
    if (!form.email?.trim()) errors.email = "Email is required";
    else if (!isValidEmail(form.email)) errors.email = "Enter a valid email address";
    if (!form.password) errors.password = "Password is required";
    else if (form.password.length < 6) errors.password = "Password must be at least 6 characters";
    if (!form.confirmPassword) errors.confirmPassword = "Please confirm password";
    else if (form.confirmPassword !== form.password) errors.confirmPassword = "Passwords do not match";
    if (!form.role) errors.role = "Select a role";

    if (!form.dateOfBirth) {
      errors.dateOfBirth = "Date of birth is required";
    } else {
      const selectedDate = new Date(form.dateOfBirth);
      selectedDate.setHours(0, 0, 0, 0);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (selectedDate >= today) {
        errors.dateOfBirth = "Date of birth must be before today";
      }
    }

    if (!form.gender) errors.gender = "Select gender";
    if (!form.address?.trim()) errors.address = "Address is required";
    return errors;
  };

  const focusFirstError = (errors) => {
    const first = Object.keys(errors)[0];
    if (!first) return;
    const el =
      formRef.current?.querySelector(`[name="${first}"]`) ||
      document.querySelector(`[name="${first}"]`);
    if (el && typeof el.focus === "function") el.focus();
    if (first === "role") {
      const roleBtn = formRef.current?.querySelector(".role-card") || document.querySelector(".role-card");
      roleBtn?.focus?.();
    }
  };

  const submit = async (e) => {
    e.preventDefault();
    setTopError("");
    setFieldErrors({});
    setSuccessMsg("");

    const errors = validate();
    if (Object.keys(errors).length) {
      setFieldErrors(errors);
      focusFirstError(errors);
      return;
    }

    setLoading(true);
    try {
      const payload = {
        fullName: form.fullName,
        email: form.email,
        password: form.password,
        role: form.role,
        dateOfBirth: form.dateOfBirth,
        gender: form.gender,
        address: form.address
      };

   
      const res = await api.post("/User/register", payload);

      setLoading(false);
      resetForm();

    
      setSuccessMsg("Registration successful! Redirecting to login...");
      setTimeout(() => {
        setSuccessMsg("");
        nav("/login", { replace: true });
      }, 2000);
    } catch (err) {
      setLoading(false);
      console.error("Register error:", err);

      const resp = err?.response;
    
      if (resp && resp.status === 409) {
        
        const serverMsg = resp.data?.message || resp.data?.error || resp.data?.errorMessage;
        if (serverMsg && /already|exists|registered/i.test(String(serverMsg))) {
          setTopError("Email already exists. Try logging in or use a different email.");
        } else {
          setTopError(serverMsg || "Email already exists.");
        }
        return;
      }

      
      if (resp?.data) {
        const data = resp.data;
        if (data.errors && typeof data.errors === "object") {
          const newFieldErrors = {};
          Object.keys(data.errors).forEach((k) => {
        
            const plainKey = k.replace(/^createUserDto\./i, "").replace(/^CreateUserDto\./i, "");
            const key = plainKey.charAt(0).toLowerCase() + plainKey.slice(1);
            newFieldErrors[key] = Array.isArray(data.errors[k]) ? data.errors[k].join(" ") : String(data.errors[k]);
          });
          setFieldErrors((p) => ({ ...p, ...newFieldErrors }));
          focusFirstError(newFieldErrors);
          return;
        }

       
        if (data.message) {
          
          if (/already|exists|registered/i.test(String(data.message))) {
            setTopError("Email already exists. Try logging in or use a different email.");
          } else {
            setTopError(data.message);
          }
          return;
        }

        
        if (Array.isArray(data)) {
          setTopError(data.join(", "));
          return;
        }
      }

      setTopError(err?.message || "Registration failed. Try again.");
    }
  };

  const pwScore = simplePasswordScore(form.password);

  return (
    <div className="container py-5">
      <div className="row justify-content-center">
        <div className="col-lg-10">
          <div className="card register-card shadow-sm">
            <div className="row g-0">
              <div className="col-md-5 register-side d-none d-md-flex align-items-center">
                <div className="p-4 text-center">
                  <img src="hirehub-logo.png" alt="HireHub" className="mb-3 register-logo" />
                  <h3 className="mb-2">Create your profile</h3>
                  <p className="text-muted">Build your profile so recruiters can find you. Apply to jobs & track applications.</p>
                </div>
              </div>

              <div className="col-md-7">
                <div className="p-4">
                  <h2 className="mb-3">Register</h2>

              
                  {topError && <div className="alert alert-danger">{topError}</div>}

                  
                  {successMsg && <div className="alert alert-success">{successMsg}</div>}

                  <form ref={formRef} onSubmit={submit} noValidate>
                    <div className="row">
                      <div className="col-12 mb-3">
                        <label className="form-label">Full name</label>
                        <input
                          name="fullName"
                          value={form.fullName}
                          onChange={onChange}
                          placeholder="Your full name"
                          className={`form-control ${fieldErrors.fullName ? "is-invalid" : ""}`}
                        />
                        {fieldErrors.fullName && <div className="invalid-feedback">{fieldErrors.fullName}</div>}
                      </div>

                      <div className="col-12 mb-3">
                        <label className="form-label">Email</label>
                        <input
                          name="email"
                          type="email"
                          value={form.email}
                          onChange={onChange}
                          placeholder="you@company.com"
                          className={`form-control ${fieldErrors.email ? "is-invalid" : ""}`}
                        />
                        {fieldErrors.email && <div className="invalid-feedback">{fieldErrors.email}</div>}
                      </div>

                      <div className="col-md-6 mb-3">
                        <label className="form-label">Password</label>
                        <input
                          name="password"
                          type="password"
                          value={form.password}
                          onChange={onChange}
                          placeholder="Create a password"
                          className={`form-control ${fieldErrors.password ? "is-invalid" : ""}`}
                          aria-describedby="passwordHelp"
                        />
                        {fieldErrors.password && <div className="invalid-feedback">{fieldErrors.password}</div>}
                        <div className="mt-2 d-flex align-items-center gap-3">
                          <div className="pw-strength">
                            <div className={`pw-bar ${pwScore >= 1 ? "on" : ""}`} />
                            <div className={`pw-bar ${pwScore >= 2 ? "on" : ""}`} />
                            <div className={`pw-bar ${pwScore >= 3 ? "on" : ""}`} />
                            <div className={`pw-bar ${pwScore >= 4 ? "on" : ""}`} />
                          </div>
                          <small id="passwordHelp" className="text-muted">
                            {pwScore <= 1 ? "Very weak" : pwScore === 2 ? "Weak" : pwScore === 3 ? "Good" : "Strong"}
                          </small>
                        </div>
                      </div>

                      <div className="col-md-6 mb-3">
                        <label className="form-label">Confirm password</label>
                        <input
                          name="confirmPassword"
                          type="password"
                          value={form.confirmPassword}
                          onChange={onChange}
                          placeholder="Repeat password"
                          className={`form-control ${fieldErrors.confirmPassword ? "is-invalid" : ""}`}
                        />
                        {fieldErrors.confirmPassword && <div className="invalid-feedback">{fieldErrors.confirmPassword}</div>}
                      </div>

                      <div className="col-12 mb-3">
                        <label className="form-label mb-2">Role</label>
                        <div className="d-flex gap-3 role-card-row" role="radiogroup" aria-label="Choose role">
                          <button
                            type="button"
                            className={`role-card ${form.role === "JobSeeker" ? "selected" : ""}`}
                            onClick={() => setRole("JobSeeker")}
                            aria-pressed={form.role === "JobSeeker"}
                          >
                            <div className="role-icon" aria-hidden>
                              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                <circle cx="12" cy="7" r="3" stroke="currentColor" strokeWidth="1.4" />
                                <path d="M4 20c0-3.3137 3.5817-6 8-6s8 2.6863 8 6" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
                              </svg>
                            </div>
                            <div>
                              <div className="role-title">Job Seeker</div>
                              <div className="role-desc">Looking for roles — apply & track applications</div>
                            </div>
                          </button>

                          <button
                            type="button"
                            className={`role-card ${form.role === "Employer" ? "selected" : ""}`}
                            onClick={() => setRole("Employer")}
                            aria-pressed={form.role === "Employer"}
                          >
                            <div className="role-icon" aria-hidden>
                              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                <rect x="3" y="7" width="18" height="12" rx="2" stroke="currentColor" strokeWidth="1.4" />
                                <path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
                              </svg>
                            </div>
                            <div>
                              <div className="role-title">Employer</div>
                              <div className="role-desc">Hiring — post jobs & manage applicants</div>
                            </div>
                          </button>
                        </div>
                        {fieldErrors.role && <div className="text-danger mt-1">{fieldErrors.role}</div>}
                      </div>

                      <div className="col-md-6 mb-3">
                        <label className="form-label">DOB</label>
                        <input
                          name="dateOfBirth"
                          type="date"
                          value={form.dateOfBirth}
                          onChange={onChange}
                          className={`form-control ${fieldErrors.dateOfBirth ? "is-invalid" : ""}`}
                        />
                        {fieldErrors.dateOfBirth && <div className="invalid-feedback">{fieldErrors.dateOfBirth}</div>}
                      </div>

                      <div className="col-md-6 mb-3">
                        <label className="form-label">Gender</label>
                        <select
                          name="gender"
                          value={form.gender}
                          onChange={onChange}
                          className={`form-select ${fieldErrors.gender ? "is-invalid" : ""}`}
                        >
                          <option value="">Select</option>
                          <option value="Male">Male</option>
                          <option value="Female">Female</option>
                          <option value="Other">Other</option>
                        </select>
                        {fieldErrors.gender && <div className="invalid-feedback">{fieldErrors.gender}</div>}
                      </div>

                      <div className="col-12 mb-3">
                        <label className="form-label">Address</label>
                        <textarea
                          name="address"
                          value={form.address}
                          onChange={onChange}
                          placeholder="Your address (city, state, country)"
                          className={`form-control ${fieldErrors.address ? "is-invalid" : ""}`}
                          rows={2}
                        />
                        {fieldErrors.address && <div className="invalid-feedback">{fieldErrors.address}</div>}
                      </div>

                      <div className="col-12 d-flex justify-content-end gap-2">
                        <button type="button" className="btn btn-outline-secondary" onClick={resetForm} disabled={loading}>
                          Reset
                        </button>
                        <button type="submit" className="btn btn-primary" disabled={loading}>
                          {loading ? "Registering..." : "Register"}
                        </button>
                      </div>
                    </div>
                  </form>

                  <div className="mt-3 small text-center text-muted">
                    Already have an account? <Link to="/login">Login here</Link>
                  </div>
                </div>
              </div>
            </div>
          </div> 
        </div>
      </div>
    </div>
  );
}
