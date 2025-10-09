// src/services/jobSeekerService.js
import api from "../api/api";

const getAll = () => api.get("/JobSeeker").then(r => r.data);
const getById = (id) => api.get(`/JobSeeker/${id}`).then(r => r.data);
const getByUser = (userId) => api.get(`/JobSeeker/by-user/${userId}`).then(r => r.data);

const searchByCollege = (name) => api.get("/JobSeeker/search/college", { params: { name } }).then(r => r.data);
const searchBySkill = (skill) => api.get("/JobSeeker/search/skill", { params: { skill } }).then(r => r.data);

const create = (dto) => api.post("/JobSeeker", dto).then(r => r.data);
const update = (id, dto) => api.put(`/JobSeeker/${id}`, dto).then(r => r.data);
const remove = (id) => api.delete(`/JobSeeker/${id}`).then(r => r.data);

export default {
  getAll,
  getById,
  getByUser,
  searchByCollege,
  searchBySkill,
  create,
  update,
  remove
};
