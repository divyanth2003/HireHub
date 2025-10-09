
import React, { useEffect, useState } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import adminService from "../../services/adminService";
import { toast } from "react-toastify";
import "../../styles/admin.css";



function SimpleModal({ open, title, children, onClose }) {
  if (!open) return null;
  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true">
      <div className="modal-panel">
        <div className="modal-header">
          <h5>{title}</h5>
          <button className="modal-close" onClick={onClose} aria-label="Close">✕</button>
        </div>
        <div className="modal-body">{children}</div>
      </div>
    </div>
  );
}

function formatCreatedAt(value) {
  if (!value) return "—";

  const d = new Date(value);
  if (isNaN(d.getTime())) return "—";
  const year = d.getFullYear();
  if (year <= 1970) return "—"; 
  return d.toLocaleString();
}

export default function AdminUserDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);


  const [deleteOpen, setDeleteOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let mounted = true;
    async function load() {
      setLoading(true);
      try {
        const data = await adminService.getUser(id);
        if (!mounted) return;
        setUser(data);
      } catch (err) {
        console.error(err);
        const msg = err?.response?.data?.message || "Failed to load user";
        toast.error(msg);
        if (err?.response?.status === 404) navigate("/admin/users");
      } finally {
        if (mounted) setLoading(false);
      }
    }
    if (id) load();
    else {
      toast.error("Invalid user id");
      navigate("/admin/users");
    }
    return () => (mounted = false);
  }, [id, navigate]);

  const confirmDelete = async () => {
    setSaving(true);
    try {
      await adminService.deleteUser(id);
      toast.success("User deleted");
      navigate("/admin/users");
    } catch (err) {
      console.error(err);
      const msg = err?.response?.data?.message || "Delete failed";
      toast.error(msg);
    } finally {
      setSaving(false);
      setDeleteOpen(false);
    }
  };

  if (loading) return <div className="container mt-4">Loading user…</div>;
  if (!user) return null;

  return (
    <div className="container mt-4 admin-user-details">
      <div className="detail-header">
        <div>
          <h2 className="mb-0">{user.fullName || user.name || "User details"}</h2>
          <div className="muted">{user.email}</div>
        </div>

        <div className="actions">
          <Link to="/admin/users" className="btn btn-outline me-2">Back</Link>
       
          <button className="btn btn-danger" onClick={() => setDeleteOpen(true)}>Delete</button>
        </div>
      </div>

      <div className="card card-surface p-3 mt-3">
        <dl className="row">
          <dt className="col-sm-3">ID</dt>
          <dd className="col-sm-9 mono-col">{user.userId ?? user.id ?? "—"}</dd>

          <dt className="col-sm-3">Role</dt>
          <dd className="col-sm-9">
            <span className={`badge role role-${(user.role || "").toString().toLowerCase()}`}>
              {user.role ?? "—"}
            </span>
          </dd>

  

          <dt className="col-sm-3">Date of birth</dt>
          <dd className="col-sm-9">{user.dateOfBirth ?? "—"}</dd>

          <dt className="col-sm-3">Gender</dt>
          <dd className="col-sm-9">{user.gender ?? "—"}</dd>

          <dt className="col-sm-3">Address</dt>
          <dd className="col-sm-9">{user.address ?? "—"}</dd>

          
        </dl>
      </div>

      <SimpleModal open={deleteOpen} title="Confirm Delete" onClose={() => setDeleteOpen(false)}>
        <p>Are you sure you want to permanently delete this user? This action cannot be undone.</p>
        <div className="modal-actions">
          <button className="btn btn-outline" onClick={() => setDeleteOpen(false)} disabled={saving}>Cancel</button>
          <button className="btn btn-danger" onClick={confirmDelete} disabled={saving}>
            {saving ? "Deleting…" : "Delete user"}
          </button>
        </div>
      </SimpleModal>
    </div>
  );
}
