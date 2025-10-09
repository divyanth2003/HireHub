
import React, { useEffect, useState, useCallback } from "react";
import notificationService from "../services/notificationService";
import { toast } from "react-toastify";
import useAuth from "../auth/useAuth";

export default function Notifications() {
  const { user } = useAuth();
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(false);
  const [busyId, setBusyId] = useState(null);

  const loadNotifications = useCallback(async () => {
    if (!user?.userId) {
      setNotifications([]);
      return;
    }
    setLoading(true);
    try {
      const data = await notificationService.getAll(user.userId);
      setNotifications(data || []);
    } catch (err) {
      console.error("Failed to load notifications", err);
      toast.error("Failed to load notifications");
    } finally {
      setLoading(false);
    }
  }, [user?.userId]);

  useEffect(() => {
    loadNotifications();
  }, [loadNotifications]);

  const handleMarkAll = async () => {
    if (!user?.userId) return;
    try {
      await notificationService.markAllAsRead(user.userId);
      toast.success("All marked as read");
      loadNotifications();
    } catch (err) {
      console.error(err);
      toast.error("Failed to mark all as read");
    }
  };

  const handleMarkRead = async (id) => {
    try {
      setBusyId(id);
      await notificationService.markAsRead(id);
      await loadNotifications();
    } catch (err) {
      console.error(err);
      toast.error("Failed to mark as read");
    } finally {
      setBusyId(null);
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Delete this notification?")) return;
    try {
      await notificationService.deleteNotification(id);
      toast.success("Deleted");
      loadNotifications();
    } catch (err) {
      console.error(err);
      toast.error("Failed to delete");
    }
  };

  if (!user) {
    return <div className="container mt-4">Please sign in to see notifications.</div>;
  }

  return (
    <div className="container mt-4">
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h3>Notifications</h3>
        <button
          className="btn btn-outline-primary btn-sm"
          onClick={handleMarkAll}
          disabled={loading}
        >
          Mark all as read
        </button>
      </div>

      {loading ? (
        <div>Loading…</div>
      ) : notifications.length === 0 ? (
        <div className="text-muted">No notifications found.</div>
      ) : (
        <div className="list-group">
          {notifications.map((n) => (
            <div
              key={n.notificationId}
              className={`list-group-item d-flex justify-content-between align-items-start ${
                n.isRead ? "bg-light" : ""
              }`}
            >
              <div>
                <div className="fw-bold">{n.subject || "Notification"}</div>
                <div dangerouslySetInnerHTML={{ __html: n.message }} />
                <div className="text-muted small">
                  {new Date(n.createdAt).toLocaleString()}
                </div>
              </div>
              <div>
                {!n.isRead && (
                  <button
                    className="btn btn-sm btn-success me-2"
                    onClick={() => handleMarkRead(n.notificationId)}
                    disabled={busyId === n.notificationId}
                  >
                    {busyId === n.notificationId ? "..." : "Mark Read"}
                  </button>
                )}
                <button
                  className="btn btn-sm btn-danger"
                  onClick={() => handleDelete(n.notificationId)}
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
