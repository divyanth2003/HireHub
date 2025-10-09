
import api from "../api/api";

const baseUrl = api.defaults?.baseURL || "";
const base = baseUrl.includes("/api/v1") ? "/Notification" : "/api/v1/Notification";

const get = (url) => api.get(url).then(r => r.data);
const post = (url, body) => api.post(url, body).then(r => r.data);
const del = (url) => api.delete(url).then(r => r.data);

const getAll = (userId) => get(`${base}/user/${userId}`);
const getUnread = (userId) => get(`${base}/user/${userId}/unread`);
const getRecent = (userId, limit = 20) => get(`${base}/user/${userId}/recent?limit=${limit}`);
const markAsRead = (id) => post(`${base}/${id}/mark-read`);
const markAllAsRead = (userId) => post(`${base}/user/${userId}/mark-all-read`);
const deleteNotification = (id) => del(`${base}/${id}`);
const create = (dto) => post(`${base}`, dto);
const messageApplicant = (dto) => post(`${base}/application/message`, dto);
const testEmail = () => post(`${base}/test-email`);

export default {
  getAll,
  getUnread,
  getRecent,
  markAsRead,
  markAllAsRead,
  deleteNotification,
  create,
  messageApplicant,
  testEmail
};
