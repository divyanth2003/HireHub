import React, { useEffect, useState, useMemo, useCallback } from "react";
import adminService from "../../services/adminService";
import { toast } from "react-toastify";
import "../../styles/admin.css";
import { Link } from "react-router-dom";

function useDebounce(value, ms = 450) {
  const [deb, setDeb] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDeb(value), ms);
    return () => clearTimeout(t);
  }, [value, ms]);
  return deb;
}

export default function AdminUsers() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounce(search, 400);
  const [roleFilter, setRoleFilter] = useState("all");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const loadUsers = useCallback(async (q = "") => {
    setLoading(true);
    try {
      const data = await adminService.getUsers(q);
      const list = Array.isArray(data) ? data : [];
      const filteredByQuery = q
        ? list.filter((u) => {
            const id = String(u.userId ?? u.id ?? "").toLowerCase();
            const name = String(u.fullName ?? u.name ?? "").toLowerCase();
            const email = String(u.email ?? "").toLowerCase();
            const ql = String(q).toLowerCase();
            return id.includes(ql) || name.includes(ql) || email.includes(ql);
          })
        : list;
      setUsers(filteredByQuery);
      setPage(1);
    } catch (err) {
      console.error(err);
      toast.error("Failed to load users");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers(debouncedSearch);
  }, [debouncedSearch, loadUsers]);

  const handleDelete = async (id) => {
    if (!window.confirm("Are you sure you want to permanently delete this user?")) return;
    try {
      await adminService.deleteUser(id);
      toast.success("User deleted");
      await loadUsers(debouncedSearch);
    } catch (err) {
      console.error(err);
      toast.error("Delete failed");
    }
  };

  const copyId = async (text) => {
    try {
      await navigator.clipboard.writeText(text);
      toast.success("ID copied");
    } catch {
      toast.info("Copy failed");
    }
  };

  const interpretActive = (u) => {
    if (typeof u.isActive === "boolean") return u.isActive;
    if (typeof u.IsActive === "boolean") return u.IsActive;
    if (typeof u.active === "boolean") return u.active;
    const byDeactivated = u.deactivatedAt ?? u.deactivated_at ?? u.deactivatedOn ?? u.deactivated;
    if (byDeactivated !== undefined && byDeactivated !== null) return false;
    const val = u.isActive ?? u.IsActive ?? u.active ?? u.status ?? null;
    if (val === null || val === undefined) return true;
    if (typeof val === "number") return val === 1;
    if (typeof val === "string") {
      const s = val.trim().toLowerCase();
      if (s === "1" || s === "true" || s === "active") return true;
      if (s === "0" || s === "false" || s === "inactive" || s === "banned") return false;
    }
    return Boolean(val);
  };

  const filteredUsers = useMemo(() => {
    if (roleFilter === "all") return users;
    const rf = String(roleFilter).toLowerCase();
    return users.filter((u) => {
      const roleRaw = String(u.role ?? u.roles ?? "").toLowerCase();
      const normalized = roleRaw.replace(/\s+/g, "");
      return normalized.includes(rf.replace(/\s+/g, ""));
    });
  }, [users, roleFilter]);

  const total = filteredUsers.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const pageUsers = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredUsers.slice(start, start + pageSize);
  }, [filteredUsers, page]);

  return (
    <div className="container mt-4 admin-users">
      <div className="card card-surface">
        <div className="card-header">
          <div>
            <h3 className="mb-0">Manage Users</h3>
            <div className="muted">View, search and manage platform users</div>
          </div>

          <div className="search-row">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                loadUsers(search);
              }}
              className="search-form"
            >
              <input
                aria-label="Search users by name or email"
                type="text"
                className="form-control input-search"
                placeholder="Search users by name, email or id..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              <button type="submit" className="btn btn-primary btn-search">Search</button>
              <button
                type="button"
                className="btn btn-outline"
                title="Reset"
                onClick={() => { setSearch(""); loadUsers(""); }}
              >
                Reset
              </button>
            </form>

            <div className="role-filter">
              <label htmlFor="roleSelect" className="sr-only">Filter by role</label>
              <select
                id="roleSelect"
                className="form-select"
                value={roleFilter}
                onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}
              >
                <option value="all">All roles</option>
                <option value="admin">Admin</option>
                <option value="employer">Employer</option>
                <option value="jobseeker">JobSeeker</option>
              </select>
            </div>
          </div>
        </div>

        <div className="card-body">
          {loading ? (
            <div className="table-placeholder">Loading users…</div>
          ) : (
            <>
              <div className="table-responsive">
                <table className="table modern-table">
                  <thead>
                    <tr>
                      <th>ID</th>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Role</th>
                      <th>Status</th>
                      <th className="text-end">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pageUsers.length === 0 ? (
                      <tr>
                        <td colSpan={6} className="text-center text-muted">No users found</td>
                      </tr>
                    ) : (
                      pageUsers.map((u) => {
                        const id = u.userId ?? u.id ?? u.user?.id ?? "—";
                        const name = u.fullName || u.name || "—";
                        const email = u.email || "—";
                        const role = (u.role || "User").toString();
                        const active = interpretActive(u);
                        return (
                          <tr key={id}>
                            <td className="mono-col">
                              <span className="id-trunc" title={id}>
                                {String(id).length > 14 ? `${String(id).slice(0, 10)}…${String(id).slice(-4)}` : id}
                              </span>
                              <button
                                className="btn-icon"
                                title="Copy ID"
                                onClick={() => copyId(id)}
                                aria-label={`Copy ID ${id}`}
                              >
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                                  <rect x="9" y="9" width="9" height="11" rx="2" stroke="currentColor" strokeWidth="1.2"/>
                                  <rect x="4" y="4" width="9" height="11" rx="2" stroke="currentColor" strokeWidth="1.2"/>
                                </svg>
                              </button>
                            </td>

                            <td>
                              <div className="user-name">{name}</div>
                            </td>

                            <td>
                              <a href={`mailto:${email}`} className="user-email">{email}</a>
                            </td>

                            <td>
                              <span className={`badge role role-${role.toLowerCase()}`}>{role}</span>
                            </td>

                            <td>
                              <span className={`badge status ${active ? "bg-success" : "bg-danger"}`}>
                                {active ? "Active" : "Inactive"}
                              </span>
                            </td>

                            <td className="text-end">
                              <Link to={`/admin/users/${id}`} className="btn btn-sm btn-ghost me-2">View</Link>

                              <button
                                className="btn btn-sm btn-danger"
                                onClick={() => handleDelete(id)}
                                aria-label={`Delete user ${name}`}
                              >
                                Delete
                              </button>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>

              <div className="table-footer">
                <div className="footer-left muted">
                  Showing <strong>{total === 0 ? 0 : (page - 1) * pageSize + 1}</strong> - <strong>{(page - 1) * pageSize + pageUsers.length}</strong> of <strong>{total}</strong>
                </div>

                <div className="pagination">
                  <button className="btn-p small" onClick={() => setPage(1)} disabled={page === 1}>«</button>
                  <button className="btn-p small" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>‹</button>
                  <span className="page-indicator">{page} / {totalPages}</span>
                  <button className="btn-p small" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>›</button>
                  <button className="btn-p small" onClick={() => setPage(totalPages)} disabled={page === totalPages}>»</button>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
