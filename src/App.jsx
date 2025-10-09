import React from "react";
import { Routes, Route } from "react-router-dom";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

import Login from "./pages/Login";
import Register from "./pages/Register";
import Settings from "./pages/Settings";
import JobSeekerProfile from "./pages/JobSeekerProfile";
import ResumeUpload from "./pages/ResumeUpload";
import PrivateRoute from "./components/PrivateRoute";
import RoleGuard from "./components/RoleGuard";
import JobsList from "./pages/JobsList";
import JobDetails from "./pages/JobDetails";
import CreateJob from "./pages/CreateJob";
import EmployerJobs from "./pages/EmployerJobs";
import Applications from "./pages/Applications";
import JobApplications from "./pages/JobApplications";
import Navbar from "./components/Navbar";
import EmployerProfile from "./pages/EmployerProfile";
import Notifications from "./pages/Notification";

import Home from "./pages/Home";
import EmployerDashboard from "./pages/EmployerDashboard";
import JobSeekerDashboard from "./pages/JobSeekerDashboard";

import AdminDashboard from "./pages/admin/AdminDashboard";
import AdminUsers from "./pages/admin/AdminUsers";
import AdminUserDetails from "./pages/admin/AdminUserDetails";
import AdminJobs from "./pages/admin/AdminJobs";
import AdminApplications from "./pages/admin/AdminApplications";

import ForgotPassword from "./pages/ForgotPassword";
import ResetPassword from "./pages/ResetPassword";




export default function App() {
  return (
    <div>
      <Navbar />

      <Routes>
        <Route path="/" element={<Home />} />

        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/settings" element={<Settings />} />

        <Route
          path="/employer"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Employer", "Admin"]}>
                <EmployerDashboard />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/seeker"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["JobSeeker", "Admin"]}>
                <JobSeekerDashboard />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/profile"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["JobSeeker", "Admin"]}>
                <JobSeekerProfile />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/resumes"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["JobSeeker"]}>
                <ResumeUpload />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/employer/profile"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Employer", "Admin"]}>
                <EmployerProfile />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route path="/jobs" element={<JobsList />} />

        <Route
          path="/jobs/create"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Employer"]}>
                <CreateJob />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/jobs/edit/:id"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Employer"]}>
                <CreateJob />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route path="/jobs/:id" element={<JobDetails />} />

        <Route
          path="/applications"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["JobSeeker"]}>
                <Applications />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/employer/jobs"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Employer"]}>
                <EmployerJobs />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/jobs/:id/applications"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Employer", "Admin"]}>
                <JobApplications />
              </RoleGuard>
            </PrivateRoute>
          }
        />

        <Route
          path="/admin"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Admin"]}>
                <AdminDashboard />
              </RoleGuard>
            </PrivateRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Admin"]}>
                <AdminUsers />
              </RoleGuard>
            </PrivateRoute>
          }
        />
        <Route
          path="/admin/jobs"
          element={
            <PrivateRoute>
              <RoleGuard allowed={["Admin"]}>
                <AdminJobs />
              </RoleGuard>
            </PrivateRoute>
          }
        />
        <Route path="/admin/users/:id" element={<AdminUserDetails />} />
        <Route path="/admin/applications" element={<AdminApplications />} />
        <Route path="/notifications" element={<Notifications />} />
        
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/reset-password" element={<ResetPassword />} />
       
        <Route path="*" element={<h2 className="container mt-4">Page not found</h2>} />
      </Routes>

      <ToastContainer
        position="bottom-right"
        autoClose={1800}
        hideProgressBar={false}
        newestOnTop={false}
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
        theme="light"
      />
    </div>
  );
}
