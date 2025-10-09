
import React from "react";
import { Navigate } from "react-router-dom";
import useAuth from "../auth/useAuth";


export default function RoleGuard({ allowed = [], children }) {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  if (allowed.length && !allowed.includes(user.role)) return <Navigate to="/" replace />;
  return children;
}
