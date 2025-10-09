import api from "../api/api";

const base = "/User";

const getAll = () => api.get(`${base}`).then((r) => r.data);
const getById = (id) => api.get(`${base}/${id}`).then((r) => r.data);
const getByRole = (role) => api.get(`${base}/by-role/${role}`).then((r) => r.data);
const searchByName = (name) => api.get(`${base}/search`, { params: { name } }).then((r) => r.data);
const update = (id, dto) => api.put(`${base}/${id}`, dto).then((r) => r.data);
const remove = (id) => api.delete(`${base}/${id}`).then((r) => r.data);
const deletePermanently = (id) => api.delete(`${base}/${id}/delete-permanently`).then((r) => r.data);
const deactivate = (id) => api.post(`${base}/${id}/deactivate`).then((r) => r.data);
const reactivate = (id) => api.post(`${base}/${id}/reactivate`).then((r) => r.data);
const requestDeletion = (userId, days = 30) => api.post(`${base}/${userId}/schedule-deletion?days=${days}`).then((r) => r.data);

export default {
  getAll,
  getById,
  getByRole,
  searchByName,
  update,
  remove,
  deletePermanently,
  deactivate,
  reactivate,
  requestDeletion,
};
