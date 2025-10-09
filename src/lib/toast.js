
import { toast } from "react-toastify";

const DEFAULT_OPTS = {
  position: "bottom-right",
  autoClose: 1800,
  hideProgressBar: false,
  closeOnClick: true,
  pauseOnHover: true,
  draggable: true,
  theme: "light",
  style: {
    borderRadius: 8,
    border: "1px solid rgba(13,110,253,0.12)",
    background: "#f3f8ff",
    color: "#0446b6"
  }
};

export const success = (msg, opts = {}) => toast.success(msg, { ...DEFAULT_OPTS, ...opts });
export const error = (msg, opts = {}) => toast.error(msg, { ...DEFAULT_OPTS, ...opts });
export const info = (msg, opts = {}) => toast.info(msg, { ...DEFAULT_OPTS, ...opts });
export const warn = (msg, opts = {}) => toast.warn(msg, { ...DEFAULT_OPTS, ...opts });
export default { success, error, info, warn };
