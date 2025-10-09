
import React, { createContext, useState, useEffect } from "react";
import authService from "../services/authService";
import employerService from "../services/employerService";
import jobSeekerService from "../services/jobSeekerService";
import userService from "../services/userService";
import api from "../api/api";

export const AuthContext = createContext(null);

export default function AuthProvider({ children }) {

  const [user, setUser] = useState(() => {
    try {
      const raw = localStorage.getItem("user");
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  });

  const [token, setToken] = useState(() => localStorage.getItem("token") ?? null);

  
  useEffect(() => {
    if (token) {
      api.defaults.headers = api.defaults.headers || {};
      api.defaults.headers.Authorization = `Bearer ${token}`;
      localStorage.setItem("token", token);
    } else {
      if (api.defaults.headers) delete api.defaults.headers.Authorization;
      localStorage.removeItem("token");
    }
  }, [token]);

  
  useEffect(() => {
    if (user) localStorage.setItem("user", JSON.stringify(user));
    else localStorage.removeItem("user");
  }, [user]);

 
  const setUserLocal = (patch = {}) => {
    setUser((prev) => {
      const next = { ...(prev || {}), ...(patch || {}) };
      try {
        localStorage.setItem("user", JSON.stringify(next));
      } catch {}
      return next;
    });
  };


  const login = async (creds) => {
    
    const auth = await authService.login(creds);
    if (!auth?.token) throw new Error("Login failed: no token returned");

    setToken(auth.token);
    api.defaults.headers = api.defaults.headers || {};
    api.defaults.headers.Authorization = `Bearer ${auth.token}`;
    localStorage.setItem("token", auth.token);

    const userObj = {
      role: auth.role ?? null,
      userId: auth.userId ?? null,
      expiresAt: auth.expiresAt ?? null,
      userFullName: null,
      userEmail: auth.email ?? null,
  
      employerId: null,
      jobSeekerId: null,
      companyName: null
    };

    try {
      const u = await userService.getById(auth.userId).catch(() => null);
      if (u) {
        userObj.userFullName = u.fullName ?? userObj.userFullName;
        userObj.userEmail = u.email ?? userObj.userEmail;
      }
    } catch (e) {
     
      console.warn("userService.getById failed:", e?.message ?? e);
    }

    
    if (auth.role === "Employer") {
      try {
        const emp = await employerService.getByUser(auth.userId).catch(() => null);
        if (emp) {
        
          userObj.employerId = emp.employerId ?? emp.EmployerId ?? userObj.employerId;
          userObj.companyName = emp.companyName ?? emp.CompanyName ?? userObj.companyName;
 
        }
      } catch (e) {
        console.warn("employerService.getByUser failed:", e?.message ?? e);
      }
    }


    if (auth.role === "JobSeeker") {
      try {
        const js = await jobSeekerService.getByUser(auth.userId).catch(() => null);
        if (js) {
          userObj.jobSeekerId = js.jobSeekerId ?? js.JobSeekerId ?? userObj.jobSeekerId;
    
          userObj.userFullName = userObj.userFullName ?? js.fullName ?? js.userFullName ?? null;
        }
      } catch (e) {
        console.warn("jobSeekerService.getByUser failed:", e?.message ?? e);
      }
    }

  
    if (!userObj.userFullName) {
     
      userObj.userFullName = auth.fullName ?? auth.userFullName ?? null;
    }
    if (!userObj.userFullName) {
     
      if (userObj.userEmail) {
        userObj.userFullName = String(userObj.userEmail).split("@")[0];
      } else if (userObj.companyName) {
        userObj.userFullName = userObj.companyName;
      } else {
        userObj.userFullName = "User";
      }
    }

    userObj.userEmail = userObj.userEmail ?? auth.email ?? null;

 
    setUser(userObj);
    return auth;
  };


  const register = async (payload) => {
    return await authService.register(payload);
  };

 
  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    if (api.defaults.headers) delete api.defaults.headers.Authorization;
  };

  return (
    <AuthContext.Provider value={{ user, token, login, register, logout, setUserLocal }}>
      {children}
    </AuthContext.Provider>
  );
}
