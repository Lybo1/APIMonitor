import React, { createContext, useState, useEffect, ReactNode, useContext } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { User, AuthContextType } from "../types/AuthTypes";
import { jwtDecode, JwtPayload } from "jwt-decode"; // Import JwtPayload for standard properties

// Custom JWT Payload type to match your token structure, extending JwtPayload
interface CustomJwtPayload extends JwtPayload {
    sub: string; // User ID (string, not number, as JWT typically uses strings)
    email: string;
    username?: string;
    name?: string;
    firstName?: string;
    lastName?: string;
    createdAt?: string;
    refreshTokenExpiry?: string;
    failedLoginAttempts?: number;
    isLockedOut?: boolean;
    isAdmin?: boolean;
    roles?: string[]; // Array of roles (e.g., ["user", "admin"])
}

// Utility to parse cookies
const getCookie = (name: string): string | null => {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop()?.split(";").shift() || null;
    return null;
};

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(getCookie("AuthToken") || localStorage.getItem("accessToken"));
    const [refreshToken, setRefreshToken] = useState<string | null>(getCookie("refresh_token") || localStorage.getItem("refreshToken"));
    const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
    const navigate = useNavigate();
    const location = useLocation();

    const verifyToken = async (accessToken: string): Promise<boolean> => {
        try {
            const response = await fetch("http://localhost:5028/api/RefreshToken/verify", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${accessToken}`,
                },
                credentials: "include",
            });
            console.log(`Token verification response: ${response.status}`);
            return response.ok;
        } catch (error) {
            console.error("Token verification failed:", error);
            return false;
        }
    };

    useEffect(() => {
        let mounted = true; // Guard against multiple initializations

        const initializeAuth = async () => {
            const storedUser = localStorage.getItem("user");
            const storedToken = getCookie("AuthToken") || localStorage.getItem("accessToken");
            const storedRefresh = getCookie("refresh_token") || localStorage.getItem("refreshToken");
            const rememberMe = localStorage.getItem("rememberMe") === "true";

            console.log("Initializing auth:", { storedUser, storedToken, storedRefresh, rememberMe });

            if (mounted && storedToken && storedRefresh && rememberMe && storedUser) {
                const parsedUser: User = JSON.parse(storedUser);
                setUser(parsedUser);
                setToken(storedToken);
                setRefreshToken(storedRefresh);

                const isTokenValid = await verifyToken(storedToken);
                console.log("Token valid:", isTokenValid);

                if (mounted && isTokenValid) {
                    setIsAuthenticated(true);
                    if (location.pathname === "/login" || location.pathname === "/") {
                        navigate("/homepage");
                    }
                } else if (mounted && storedRefresh) {
                    await refreshAuthToken(storedRefresh);
                } else if (mounted) {
                    logout();
                }
            } else if (mounted && !isAuthenticated && location.pathname !== "/register" && location.pathname !== "/login") {
                navigate("/login");
            }
        };

        initializeAuth();

        return () => {
            mounted = false; // Cleanup on unmount
        };
    }, [navigate, location.pathname]); // Consider reducing dependencies if needed

    const login = async (email: string, password: string, rememberMe: boolean) => {
        try {
            const response = await fetch("http://localhost:5028/api/Login/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email, password, rememberMe }),
                credentials: "include",
            });

            const text = await response.text();
            console.log("Login response text:", text);

            if (!response.ok) {
                try {
                    const errorData = JSON.parse(text);
                    throw new Error(errorData.message || "Login failed");
                } catch {
                    throw new Error(text || "Login failed - unknown error");
                }
            }

            const data = JSON.parse(text);
            console.log("Login response parsed:", JSON.stringify(data, null, 2));

            const user: User = {
                id: parseInt(data.user.id, 10) || 0, // Parse string ID to number
                email: data.user.email,
                username: data.user.userName,
                firstName: data.user.firstName || "",
                lastName: data.user.lastName || "",
                refreshToken: data.refreshToken || "",
                createdAt: data.user.createdAt,
                refreshTokenExpiry: rememberMe ? new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString() : "",
                failedLoginAttempts: 0,
                isLockedOut: false,
                isAdmin: data.user.isAdmin || false,
                roles: data.user.roles?.map((role: string) => role.toLowerCase()) || ["user"], // Normalize to lowercase
            };

            const accessToken = data.accessToken;
            setUser(user);
            setToken(accessToken);
            setRefreshToken(data.refreshToken || null);
            setIsAuthenticated(true);

            if (rememberMe) {
                localStorage.setItem("user", JSON.stringify(user));
                localStorage.setItem("accessToken", accessToken);
                localStorage.setItem("refreshToken", data.refreshToken || "");
                localStorage.setItem("rememberMe", "true");
            } else {
                localStorage.removeItem("user");
                localStorage.removeItem("accessToken");
                localStorage.removeItem("refreshToken");
                localStorage.removeItem("rememberMe");
            }

            console.log("User set:", JSON.stringify(user, null, 2));
            navigate("/homepage");
        } catch (error) {
            console.error("Login error:", error);
            throw error;
        }
    };

    const logout = () => {
        setUser(null);
        setToken(null);
        setRefreshToken(null);
        setIsAuthenticated(false);
        localStorage.clear();
        document.cookie = "AuthToken=; Max-Age=0; path=/";
        document.cookie = "refresh_token=; Max-Age=0; path=/";
        navigate("/login");
    };

    const refreshAuthToken = async (existingRefreshToken: string) => {
        try {
            console.log("Attempting token refresh with:", existingRefreshToken);
            const response = await fetch("http://localhost:5028/api/RefreshToken/refresh", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ refreshToken: existingRefreshToken }),
                credentials: "include",
            });

            if (!response.ok) {
                console.error("Token refresh failed with status:", response.status);
                throw new Error("Token refresh failed");
            }

            const data = await response.json();
            console.log("Token refresh response:", JSON.stringify(data, null, 2));

            const newToken = data.accessToken;
            const newRefreshToken = data.refreshToken || null;

            const decoded = jwtDecode<CustomJwtPayload>(newToken);
            const userData: User = {
                id: parseInt(decoded.sub, 10) || 0, // Parse string ID to number
                email: decoded.email,
                username: decoded.username || decoded.name || "",
                firstName: decoded.firstName || "",
                lastName: decoded.lastName || "",
                refreshToken: newRefreshToken,
                createdAt: decoded.createdAt || new Date().toISOString(),
                refreshTokenExpiry: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
                failedLoginAttempts: 0,
                isLockedOut: false,
                isAdmin: decoded.isAdmin || false,
                roles: decoded.roles?.map((role: string) => role.toLowerCase()) || ["user"], // Normalize to lowercase
            };

            setUser(userData);
            setToken(newToken);
            setRefreshToken(newRefreshToken);
            setIsAuthenticated(true);

            if (localStorage.getItem("rememberMe") === "true") {
                localStorage.setItem("user", JSON.stringify(userData));
                localStorage.setItem("accessToken", newToken);
                localStorage.setItem("refreshToken", newRefreshToken || "");
            }

            console.log("Token refreshed, user updated:", JSON.stringify(userData, null, 2));
            if (location.pathname === "/login") navigate("/homepage");
        } catch (error) {
            console.error("Token refresh error:", error);
            logout();
        }
    };

    const register = async (email: string, password: string, confirmPassword: string, rememberMe: boolean) => {
        try {
            const response = await fetch("http://localhost:5028/api/Register/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email, password, confirmPassword, rememberMe }),
                credentials: "include",
            });

            const text = await response.text();
            console.log("Register response text:", text);

            if (!response.ok) {
                try {
                    const errorData = JSON.parse(text);
                    throw new Error(errorData.message || "Registration failed");
                } catch {
                    throw new Error(text || "Registration failed - unknown error");
                }
            }

            // Parse the minimal response (token in cookies, no user data in body)
            const data = JSON.parse(text);
            console.log("Register response parsed:", JSON.stringify(data, null, 2));

            // Get the tokens from cookies
            const authToken = getCookie("AuthToken");
            const refreshToken = getCookie("refresh_token");
            if (!authToken || !refreshToken) throw new Error("Authentication tokens not found in cookies");

            setToken(authToken);
            setRefreshToken(refreshToken);
            setIsAuthenticated(true);

            // Fetch user data using the token from /api/User/me
            const userResponse = await fetch("http://localhost:5028/api/User/me", {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${authToken}`,
                    "Content-Type": "application/json",
                },
                credentials: "include",
            });

            if (!userResponse.ok) throw new Error("Failed to fetch user data");

            const userData = await userResponse.json();
            console.log("User data from /api/User/me:", JSON.stringify(userData, null, 2));

            const user: User = {
                id: userData.Id || 0, // Match the response property name (capital 'Id')
                email: userData.Email || "",
                username: userData.UserName || "",
                firstName: userData.FirstName || "",
                lastName: userData.LastName || "",
                refreshToken: refreshToken || "", // Use the refresh_token from cookies
                createdAt: userData.CreatedAt || new Date().toISOString(),
                refreshTokenExpiry: rememberMe ? new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString() : "",
                failedLoginAttempts: userData.FailedLoginAttempts || 0,
                isLockedOut: userData.IsLockedOut || false,
                isAdmin: userData.IsAdmin || false,
                roles: userData.Roles?.map((role: string) => role.toLowerCase()) || ["user"], // Normalize to lowercase
            };

            setUser(user);

            if (rememberMe) {
                localStorage.setItem("user", JSON.stringify(user));
                localStorage.setItem("accessToken", authToken);
                localStorage.setItem("refreshToken", refreshToken);
                localStorage.setItem("rememberMe", "true");
            } else {
                localStorage.removeItem("user");
                localStorage.removeItem("accessToken");
                localStorage.removeItem("refreshToken");
                localStorage.removeItem("rememberMe");
            }

            console.log("User registered:", JSON.stringify(user, null, 2));
            navigate("/homepage");
        } catch (error) {
            console.error("Register error:", error);
            throw error;
        }
    };

    // Add periodic token refresh check
    useEffect(() => {
        let refreshInterval: ReturnType<typeof setInterval> | null = null;
        if (token && refreshToken) {
            const decoded = jwtDecode<CustomJwtPayload>(token);
            const expiry = new Date((decoded.exp ?? 0) * 1000); // Handle undefined exp with nullish coalescing
            const now = new Date();
            const timeLeft = expiry.getTime() - now.getTime();

            if (timeLeft < 5 * 60 * 1000) { // Refresh if less than 5 minutes remain
                refreshAuthToken(refreshToken);
            }

            // Set interval to check every minute
            refreshInterval = setInterval(() => {
                const currentTime = new Date();
                const newTimeLeft = expiry.getTime() - currentTime.getTime();
                if (newTimeLeft < 5 * 60 * 1000) {
                    refreshAuthToken(refreshToken);
                }
            }, 60 * 1000); // Check every minute
        }

        return () => {
            if (refreshInterval) clearInterval(refreshInterval);
        };
    }, [token, refreshToken]);

    return (
        <AuthContext.Provider
            value={{ user, token, refreshToken, isAuthenticated, login, logout, refreshAuthToken, register, setUser }}
        >
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (!context) throw new Error("useAuth must be used within an AuthProvider");
    return context;
};