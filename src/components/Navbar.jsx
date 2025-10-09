import React, { useState, useRef, useEffect } from "react";
import { NavLink, Link, useNavigate, useLocation } from "react-router-dom";
import useAuth from "../auth/useAuth";
import logoImg from "../assets/hirehub-logo.png";
import "../styles/navbar.css";
import notificationService from "../services/notificationService"; 

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [profileOpen, setProfileOpen] = useState(false);
  const [navOpen, setNavOpen] = useState(false); 
  const dropdownRef = useRef(null);
  const togglerRef = useRef(null);
  const collapseRef = useRef(null);

  const isRegisterPage = location.pathname === "/register";
  const isLoginPage = location.pathname === "/login";

  
  useEffect(() => {
    function handleClick(e) {
      if (
        profileOpen &&
        dropdownRef.current &&
        !dropdownRef.current.contains(e.target) &&
        togglerRef.current &&
        !togglerRef.current.contains(e.target)
      ) {
        setProfileOpen(false);
      }

     
      if (
        navOpen &&
        collapseRef.current &&
        !collapseRef.current.contains(e.target) &&
        togglerRef.current &&
        !togglerRef.current.contains(e.target)
      ) {
        setNavOpen(false);
      }
    }
    function handleEsc(e) {
      if (e.key === "Escape") {
        setProfileOpen(false);
        setNavOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    document.addEventListener("keydown", handleEsc);
    return () => {
      document.removeEventListener("mousedown", handleClick);
      document.removeEventListener("keydown", handleEsc);
    };
  }, [profileOpen, navOpen]);


  useEffect(() => {
    setNavOpen(false);
    setProfileOpen(false);
  }, [location.pathname]);

  const handleLogout = () => {
    try {
      logout?.();
    } catch (e) {
      console.error(e);
    }
    setProfileOpen(false);
    navigate("/login");
  };

  const shortUserLabel = () => {
    if (!user) return "";
    const nameCandidates = [
      user.userFullName,
      user.fullName,
      user.displayName,
      user.name,
      user.companyName,
    ];
    for (const n of nameCandidates) {
      if (n && String(n).trim()) return String(n).trim();
    }
    if (user.userEmail) return String(user.userEmail).split("@")[0];
    if (user.email) return String(user.email).split("@")[0];
    return String(user.userId || user.id || user.role || "").slice(0, 12);
  };

  const avatarInitial = () => {
    const label = shortUserLabel();
    return label ? String(label).charAt(0).toUpperCase() : "H";
  };


  const myJobsRoute = "/employer/jobs";
  const employerProfileRoute = "/employer/profile";
  const jobSeekerAppsRoute = "/applications";
  const jobSeekerProfileRoute = "/profile";

  
  function NotificationBell() {
    const [open, setOpen] = useState(false);
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(false);
    const bellRef = useRef(null);

    useEffect(() => {
      async function load() {
        if (!user?.userId) return;
        setLoading(true);
        try {
          const data = await notificationService.getUnread(user.userId);
          setItems(Array.isArray(data) ? data : []);
        } catch (err) {
          console.error("Failed to fetch unread notifications", err);
        } finally {
          setLoading(false);
        }
      }
      load();
    }, [user]);

    useEffect(() => {
      function onDocClick(e) {
        if (open && bellRef.current && !bellRef.current.contains(e.target)) {
          setOpen(false);
        }
      }
      document.addEventListener("mousedown", onDocClick);
      return () => document.removeEventListener("mousedown", onDocClick);
    }, [open]);

    const unreadCount = items.length;

    const handleOpen = async () => {
      setOpen(!open);
      if (!open && user?.userId) {
        try {
          const data = await notificationService.getUnread(user.userId);
          setItems(Array.isArray(data) ? data : []);
        } catch (err) {
          console.error(err);
        }
      }
    };

    const handleClickNotification = async (n) => {
      try {
        await notificationService.markAsRead(n.notificationId);
      } catch (err) {
        console.warn("Mark as read failed", err);
      } finally {
        navigate("/notifications");
        setOpen(false);
      }
    };

    return (
      <li className="nav-item position-relative me-2" ref={bellRef}>
        <button
          className="btn btn-light btn-icon-bell"
          onClick={handleOpen}
          aria-haspopup="true"
          aria-expanded={open}
          title="Notifications"
        >
          <span aria-hidden className="bell-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M15 17h5l-1.405-1.405A2.032 2.032 0 0 1 18.6 14.6V11a6 6 0 1 0-12 0v3c0 .414-.162.812-.455 1.105L4 17h11z" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
              <path d="M13.73 21a2 2 0 0 1-3.46 0" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </span>
          {unreadCount > 0 && (
            <span className="badge unread-badge">{unreadCount}</span>
          )}
        </button>

        {open && (
          <div
            className="notification-dropdown shadow-sm"
            role="menu"
            aria-label="Unread notifications"
          >
            <div className="nd-header d-flex justify-content-between align-items-center">
              <strong>Notifications</strong>
              <small className="text-muted">{unreadCount} new</small>
            </div>

            <div className="nd-list">
              {loading ? (
                <div className="p-3 text-center text-muted">Loading…</div>
              ) : items.length === 0 ? (
                <div className="p-3 text-muted small text-center">No new notifications</div>
              ) : (
                items.map(n => (
                  <button
                    key={n.notificationId}
                    className="nd-item"
                    onClick={() => handleClickNotification(n)}
                    title={n.message}
                  >
                    <div className="nd-subject">{n.subject || "Notification"}</div>
                    <div className="nd-message text-muted">{n.message}</div>
                    <div className="nd-time text-muted small">{new Date(n.createdAt).toLocaleString()}</div>
                  </button>
                ))
              )}
            </div>

            <div className="nd-footer text-center">
              <Link to="/notifications" className="small text-primary" onClick={() => setOpen(false)}>
                View all →
              </Link>
            </div>
          </div>
        )}
      </li>
    );
  } 

  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-white border-bottom py-3">
      <div className="container">
      
        <Link className="navbar-brand d-flex align-items-center gap-2" to="/">
          {logoImg ? <img src={logoImg} alt="HireHub" style={{ height: 36 }} /> : null}
          <span className="brand-text">
            <span className="brand-hi">Hire</span>
            <span className="brand-hub">Hub</span>
          </span>
        </Link>

        
        <button
          ref={togglerRef}
          className={`navbar-toggler ${navOpen ? "open" : ""}`}
          type="button"
          aria-controls="mainNav"
          aria-expanded={navOpen}
          aria-label="Toggle navigation"
          onClick={() => setNavOpen(o => !o)}
        >

          <span className="navbar-toggler-icon" aria-hidden />
        </button>

      
        <div
          ref={collapseRef}
          className={`collapse navbar-collapse ${navOpen ? "show" : ""}`}
          id="mainNav"
        >
        
          <ul className="navbar-nav me-auto mb-2 mb-lg-0 align-items-lg-center">
            <li className="nav-item"><NavLink className="nav-link" to="/">Home</NavLink></li>
            <li className="nav-item"><NavLink className="nav-link" to="/jobs">Jobs</NavLink></li>

          
            {user?.role === "Employer" && (
              <li className="nav-item"><NavLink className="nav-link" to="/employer">Dashboard</NavLink></li>
            )}

          
            {user?.role === "JobSeeker" && (
              <li className="nav-item"><NavLink className="nav-link" to="/seeker">Dashboard</NavLink></li>
            )}

           
            {user?.role === "Admin" && (
              <li className="nav-item"><NavLink className="nav-link" to="/admin">Dashboard</NavLink></li>
            )}
          </ul>

          <ul className="navbar-nav ms-auto align-items-center">
            {!user ? (
              <>
                {!isLoginPage && !isRegisterPage && (
                  <li className="nav-item me-2">
                    <NavLink to="/login" className="nav-link login-pill">Login</NavLink>
                  </li>
                )}
                {!isRegisterPage && !isLoginPage && (
                  <li className="nav-item">
                    <NavLink to="/register" className="nav-link register-pill">Register</NavLink>
                  </li>
                )}
              </>
            ) : (
              <>
              
                <li className="nav-item d-flex align-items-center me-3">
                  <div className="user-block d-flex align-items-center gap-2">
                    <div className="user-avatar" title={shortUserLabel()} aria-hidden>{avatarInitial()}</div>
                    <div className="inline-greeting">
                      Hi, <span className="user-name-inline">{shortUserLabel()}</span>
                    </div>
                  </div>
                </li>

                
                <NotificationBell />

                <li className="nav-item position-relative">
                  <button
                    ref={togglerRef}
                    type="button"
                    className="btn btn-outline-primary btn-sm rounded-circle d-flex align-items-center justify-content-center profile-icon-btn"
                    onClick={() => setProfileOpen(p => !p)}
                    aria-haspopup="true"
                    aria-expanded={profileOpen}
                    aria-controls="profile-dropdown"
                    title="Profile Menu"
                  >
                    <i className="bi bi-person-fill" style={{ fontSize: "1.2rem" }}></i>
                  </button>

                  {profileOpen && (
                    <div
                      id="profile-dropdown"
                      ref={dropdownRef}
                      className="profile-dropdown shadow-sm"
                      role="menu"
                      aria-label="Profile menu"
                    >
                      {user.role === "Employer" && (
                        <>
                          <NavLink to={myJobsRoute} className="dropdown-item" onClick={() => setProfileOpen(false)}>
                            My Jobs
                          </NavLink>
                          <NavLink to={employerProfileRoute} className="dropdown-item" onClick={() => setProfileOpen(false)}>
                            Profile
                          </NavLink>
                        </>
                      )}

                      {user.role === "JobSeeker" && (
                        <>
                          <NavLink to={jobSeekerAppsRoute} className="dropdown-item" onClick={() => setProfileOpen(false)}>
                            Applications
                          </NavLink>
                          <NavLink to={jobSeekerProfileRoute} className="dropdown-item" onClick={() => setProfileOpen(false)}>
                            Profile
                          </NavLink>
                        </>
                      )}

                      {user.role === "Admin" && (
                        <>
                          <NavLink to="/admin" className="dropdown-item" onClick={() => setProfileOpen(false)}>
                            Admin Dashboard
                          </NavLink>
                          <NavLink to="/admin/users" className="dropdown-item" onClick={() => setProfileOpen(false)}>
                            Manage Users
                          </NavLink>
                          <NavLink to="/admin/jobs" className="dropdown-item" onClick={() => setProfileOpen(false)}>
                            Manage Jobs
                          </NavLink>
                        </>
                      )}

                      <NavLink to="/settings" className="dropdown-item" onClick={() => setProfileOpen(false)}>
                        Settings
                      </NavLink>

                      <div className="dropdown-divider" />

                      <button className="dropdown-item text-danger" onClick={handleLogout}>Logout</button>
                    </div>
                  )}
                </li>
              </>
            )}
          </ul>
        </div>
      </div>
    </nav>
  );
}
