import api from "../api/api";


const upload = (formData) =>
  api.post("/Resume/upload", formData, {
    headers: { "Content-Type": "multipart/form-data" }
  }).then(r => r.data);

const getById = (id) => {
  if (!id) return Promise.resolve(null);
  return api.get(`/Resume/${id}`).then(r => r.data);
};

const download = async (id) => {
  if (!id) throw new Error("Missing resume id for download");
  const res = await api.get(`/Resume/${id}/download`, { responseType: "blob" });
  const contentType = (res.headers && res.headers["content-type"]) || "application/pdf";
  const blob = new Blob([res.data], { type: contentType });
  return URL.createObjectURL(blob);
};


const getViewUrl = async (resumeId) => {
  if (!resumeId) throw new Error("Missing resume id");

  
  try {
    const meta = await getById(resumeId).catch(() => null);
    if (meta) {
      if (meta.downloadUrl) return { url: meta.downloadUrl, isBlob: false };

      if (meta.filePath) {
        const base = (import.meta && import.meta.env && import.meta.env.VITE_API_URL) || "https://localhost:7273";
        const path = meta.filePath.startsWith("/") ? meta.filePath : `/${meta.filePath}`;
        return { url: base.replace(/\/$/, "") + path, isBlob: false };
      }
    }
  } catch (err) {
    console.warn("getViewUrl metadata read failed", err);
  }

  try {
    const base = (import.meta && import.meta.env && import.meta.env.VITE_API_URL) || "https://localhost:7273";
    const direct = `${base.replace(/\/$/, "")}/api/v1/Resume/${resumeId}`;
    return { url: direct, isBlob: false };
  } catch (err) {
  }

  const blobUrl = await download(resumeId);
  return { url: blobUrl, isBlob: true };
};

const getByJobSeeker = (jobSeekerId) => {
  if (!jobSeekerId) return Promise.resolve([]);
  return api.get(`/Resume/jobseeker/${jobSeekerId}`).then(r => r.data || []);
};

const getDefault = (jobSeekerId) => {
  if (!jobSeekerId) return Promise.resolve(null);
  return api.get(`/Resume/jobseeker/${jobSeekerId}/default`).then(r => r.data);
};


const getDefaultForJobSeeker = async (jobSeekerId) => {
  if (!jobSeekerId) return null;
  try {
    const d = await getDefault(jobSeekerId).catch(() => null);
    if (d) return d;
  } catch (e) {
   
  }

  try {
    const list = await getByJobSeeker(jobSeekerId);
    if (!Array.isArray(list) || list.length === 0) return null;
    return list.find(r => r.isDefault) || list[0] || null;
  } catch (err) {
    return null;
  }
};

const remove = (id) => {
  if (!id) return Promise.resolve(null);
  return api.delete(`/Resume/${id}`).then(r => r.data);
};


const setDefault = (jobSeekerId, resumeId) => {
  if (!jobSeekerId || !resumeId) return Promise.reject(new Error("jobSeekerId and resumeId required"));
  return api.post(`/Resume/jobseeker/${jobSeekerId}/set-default/${resumeId}`).then(r => r.data);
};

const update = (id, dto) => {
  if (!id) return Promise.reject(new Error("id required"));
  return api.put(`/Resume/${id}`, dto).then(r => r.data);
};

export default {
  upload,
  getById,
  getViewUrl,
  download,
  getByJobSeeker,
  getDefault,
  getDefaultForJobSeeker,
  remove,
  setDefault,
  update
};
