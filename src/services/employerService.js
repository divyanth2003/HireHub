import api from "../api/api";

const getAll = () => api.get("/Employer").then(r => r.data);
const getById = (id) => api.get(`/Employer/${id}`).then(r => r.data);
const getByUser = (userId) => api.get(`/Employer/by-user/${userId}`).then(r => r.data);
const searchByCompany = (company) => api.get("/Employer/search", { params: { company } }).then(r => r.data);
const getByJob = (jobId) => api.get(`/Employer/by-job/${jobId}`).then(r => r.data);

const create = (dto) => api.post("/Employer", dto).then(r => r.data);
const update = (id, dto) => api.put(`/Employer/${id}`, dto).then(r => r.data);
const remove = (id) => api.delete(`/Employer/${id}`).then(r => r.data);

export default {
  getAll,
  getById,
  getByUser,
  searchByCompany,
  getByJob,
  create,
  update,
  remove
};
