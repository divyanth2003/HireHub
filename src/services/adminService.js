import api from "../api/api";

const getStats = () => api.get("/admin/stats").then(r => r.data);
const getUsers = (q) => api.get("/admin/users", { params: { q } }).then(r => r.data);
const getUser = (id) => api.get(`/admin/users/${id}`).then(r => r.data);
const updateUser = (id, dto) => api.put(`/admin/users/${id}`, dto).then(r => r.data);
const deleteUser = (id) => api.delete(`/admin/users/${id}`).then(r => r.data);
const getJobs = (q) => api.get("/admin/jobs", { params: { q } }).then(r => r.data);
const deleteJob = (id) => api.delete(`/admin/jobs/${id}`).then(r => r.data);
const getApplications = (q) => api.get("/admin/applications", { params: { q } }).then(r => r.data);
const deleteApplication = (id) => api.delete(`/admin/applications/${id}`).then(r => r.data);

export default {
  getStats,
  getUsers,
  getUser,
  updateUser,
  deleteUser,
  getJobs,
  deleteJob,
  getApplications,
  deleteApplication
};
