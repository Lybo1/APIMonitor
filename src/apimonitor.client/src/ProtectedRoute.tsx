import React, { useEffect, useState } from "react";
import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "./context/AuthContext.tsx";

interface ProtectedRouteProps {
    redirectPath?: string;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ redirectPath = "/login" }) => {
    const { isAuthenticated, token } = useAuth();
    const [isChecking, setIsChecking] = useState(true);

    useEffect(() => {
        const checkAuth = async () => {
            if (token) {
                try {
                    // Simply fetch to verify token (no need to store isValid if not used)
                    await fetch("http://localhost:5028/api/RefreshToken/verify", {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            "Authorization": `Bearer ${token}`,
                        },
                        credentials: "include",
                    });
                } catch {
                    // No need for error variable if unused
                } finally {
                    setIsChecking(false);
                }
            } else {
                setIsChecking(false);
            }
        };
        checkAuth();
    }, [token]);

    if (isChecking) return null;

    if (!isAuthenticated) {
        return <Navigate to={redirectPath} replace />;
    }

    return <Outlet />;
};

export default ProtectedRoute;