import api from "../api/api";

const getAll = () => api.get("/Job").then(r => r.data);
const getById = (id) => api.get(`/Job/${id}`).then(r => r.data);
const getByEmployer = (employerId) => api.get(`/Job/employer/${employerId}`).then(r => r.data);

const create = (dto) => api.post("/Job", dto).then(r => r.data);
const update = (id, dto) => api.put(`/Job/${id}`, dto).then(r => r.data);
const remove = (id) => api.delete(`/Job/${id}`).then(r => r.data);


const searchByTitle = (query) => api.get("/Job/search/title", { params: { query } }).then(r => r.data);
const searchByLocation = (location) => api.get("/Job/search/location", { params: { location } }).then(r => r.data);
const searchBySkill = (skill) => api.get("/Job/search/skill", { params: { skill } }).then(r => r.data);
const searchByCompany = (company) => api.get("/Job/search/company", { params: { company } }).then(r => r.data);

export default {
  getAll, getById, getByEmployer,
  create, update, remove,
  searchByTitle, searchByLocation, searchBySkill, searchByCompany
};
