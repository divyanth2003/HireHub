
import api from "../api/api";


const login = async ({ email, password }) => {
  const res = await api.post("/user/login", { email, password });
  return res.data;
};

const register = async (payload) => {
  const res = await api.post("/User/register", payload);
  return res.data;
};

export default { login, register };
