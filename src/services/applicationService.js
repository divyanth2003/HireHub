// src/services/applicationService.js
import api from "../api/api";

const create = (dto) => api.post("/Application", dto).then(r => r.data);

const apply = async ({ jobId, resumeId, coverLetter = "" }) => {
  if (!jobId) throw new Error("jobId is required");
  if (!resumeId) throw new Error("resumeId is required");
  const payload = {
    jobId,
    resumeId,
    coverLetter
  };
  const attempts = [
    () => api.post("/Application", payload),
    () => api.post("/Applications", payload),
    () => api.post("/application/apply", payload),
    () => api.post("/application", payload),
    () => api.post("/applications", payload),
    () => create(payload)
  ];
  let lastErr = null;
  for (const fn of attempts) {
    try {
      const res = await fn();
      return res.data ?? res;
    } catch (err) {
      lastErr = err;
      const status = err?.response?.status;
      if (status && status >= 500) break;
    }
  }
  throw lastErr || new Error("Apply failed: no endpoint responded.");
};

const getAll = () => api.get("/Application").then(r => r.data);
const getById = (id) => api.get(`/Application/${id}`).then(r => r.data);
const getByJob = (jobId) => api.get(`/Application/job/${jobId}`).then(r => r.data);
const getByJobSeeker = (jobSeekerId) => api.get(`/Application/jobseeker/${jobSeekerId}`).then(r => r.data);
const getShortlisted = (jobId) => api.get(`/Application/job/${jobId}/shortlisted`).then(r => r.data);
const getWithInterview = (jobId) => api.get(`/Application/job/${jobId}/interviews`).then(r => r.data);

const update = (id, dto) => api.put(`/Application/${id}`, dto).then(r => r.data);
const remove = (id) => api.delete(`/Application/${id}`).then(r => r.data);

const shortlist = (id, isShortlisted = true) =>
  update(id, { isShortlisted, status: isShortlisted ? "Shortlisted" : "Applied" });

const scheduleInterview = (id, interviewDate) =>
  update(id, { interviewDate, status: "Interview" });

const review = (id, { status, employerFeedback, isShortlisted, interviewDate }) =>
  update(id, { status, employerFeedback, isShortlisted, interviewDate });

const markReviewed = (appId, notes) =>
  api.post(`/Application/${appId}/review`, notes, {
    headers: { "Content-Type": "application/json" }
  }).then(r => r.data);

export default {
  create,
  apply,
  getAll,
  getById,
  getByJob,
  getByJobSeeker,
  getShortlisted,
  getWithInterview,
  update,
  remove,
  shortlist,
  scheduleInterview,
  review,
  markReviewed
};
