import React, { createContext, useState, useEffect, ReactNode, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { User, AuthContextType } from "../types/AuthTypes.ts";

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(null);
    const [refreshToken, setRefreshToken] = useState<string | null>(null);
    const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
    const navigate = useNavigate();

    const verifyToken = async (): Promise<boolean> => {
        try {
            const response = await fetch("http://localhost:5028/api/RefreshToken/verify", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                credentials: 'include',
            });
            console.log(`Token verification response: ${response.status}`);
            return response.ok;
        } catch (error) {
            console.error("Token verification failed:", error);
            return false;
        }
    };

    useEffect(() => {
        const initializeAuth = async () => {
            const storedUser = localStorage.getItem('user');
            const rememberMe = localStorage.getItem('rememberMe') === 'true';

            console.log("Initializing auth:", { storedUser, rememberMe });

            if (storedUser && rememberMe) {
                const parsedUser: User = JSON.parse(storedUser);
                // Only verify if we're not freshly logged in
                if (!isAuthenticated) {
                    const isTokenValid = await verifyToken();
                    console.log("Token valid:", isTokenValid);
                    if (isTokenValid) {
                        setUser(parsedUser);
                        setToken('http-only-access-token');
                        setRefreshToken(parsedUser.refreshToken || null);
                        setIsAuthenticated(true);
                        navigate("/homepage");
                    } else if (parsedUser.refreshToken) {
                        try {
                            await refreshAuthToken(parsedUser.refreshToken);
                        } catch {
                            setIsAuthenticated(false);
                            localStorage.clear();
                            navigate("/login");
                        }
                    } else {
                        setIsAuthenticated(false);
                        localStorage.clear();
                        navigate("/login");
                    }
                } else {
                    // Fresh login, trust it
                    setUser(parsedUser);
                    setToken('http-only-access-token');
                    setRefreshToken(parsedUser.refreshToken || null);
                    setIsAuthenticated(true);
                    navigate("/homepage");
                }
            } else {
                setIsAuthenticated(false);
                navigate("/login");
            }
        };

        initializeAuth();
    }, [navigate, isAuthenticated]);

    const login = async (email: string, password: string, rememberMe: boolean) => {
        const response = await fetch("http://localhost:5028/api/Login/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                email,
                password,
                rememberMe
            }),
            credentials: 'include',
        });

        if (!response.ok) {
            throw new Error((await response.json()).message || "Login failed");
        }

        const data = await response.json();
        console.log("Login response:", JSON.stringify(data, null, 2));

        const user: User = {
            id: data.user.id,
            email: data.user.email,
            username: data.user.userName,
            firstName: data.user.firstName || '',
            lastName: data.user.lastName || '',
            refreshToken: data.refreshToken || '',
            createdAt: data.user.createdAt,
            refreshTokenExpiry: rememberMe ? new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString() : '',
            failedLoginAttempts: 0,
            isLockedOut: false,
            isAdmin: data.user.isAdmin,
            roles: data.user.roles || ['User'],
        };

        setUser(user);
        setToken('http-only-access-token');
        setRefreshToken(data.refreshToken || null);
        setIsAuthenticated(true);

        if (rememberMe) {
            localStorage.setItem("user", JSON.stringify(user));
            localStorage.setItem('rememberMe', 'true');
        } else {
            localStorage.removeItem('rememberMe');
            localStorage.removeItem('user');
        }

        console.log("User set:", JSON.stringify(user, null, 2));
        navigate("/homepage");
    };

    const register = async (email: string, password: string, confirmPassword: string, rememberMe: boolean) => {
        try {
            if (password !== confirmPassword) {
                throw new Error("Passwords do not match");
            }

            const response = await fetch("http://localhost:5028/api/Register/register", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    email,
                    password,
                    confirmPassword,
                    rememberMe
                }),
                credentials: 'include',
            });

            if (!response.ok) {
                throw new Error((await response.json()).message || "Registration failed");
            }

            const data = await response.json();
            const user: User = {
                id: data.id || 0,
                email: data.email || email,
                username: data.username || email.split('@')[0],
                firstName: data.firstName || '',
                lastName: data.lastName || '',
                refreshToken: data.refreshToken || '',
                createdAt: data.createdAt || new Date().toISOString(),
                refreshTokenExpiry: rememberMe ? new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString() : '',
                failedLoginAttempts: 0,
                isLockedOut: false,
                isAdmin: data.isAdmin || false,
                roles: data.roles || ['User'],
            };

            setUser(user);
            setToken('http-only-access-token');
            setRefreshToken(data.refreshToken || null);
            setIsAuthenticated(true);

            if (rememberMe) {
                localStorage.setItem("user", JSON.stringify(user));
                localStorage.setItem("rememberMe", 'true');
            } else {
                localStorage.removeItem('rememberMe');
            }
            navigate("/homepage");
        } catch (error) {
            console.error("Registration error: ", error);
            throw error;
        }
    };

    const logout = () => {
        setUser(null);
        setToken(null);
        setRefreshToken(null);
        setIsAuthenticated(false);
        localStorage.clear();
        document.cookie = 'access_token=; Max-Age=0; path=/';
        document.cookie = 'refresh_token=; Max-Age=0; path=/';
        navigate("/login");
    };

    const refreshAuthToken = async (existingRefreshToken: string) => {
        try {
            const response = await fetch("http://localhost:5028/api/RefreshToken/refresh", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({ refreshToken: existingRefreshToken }),
                credentials: 'include',
            });

            if (!response.ok) {
                throw new Error("Token refresh failed");
            }

            const data = await response.json();
            setToken('http-only-access-token');
            setRefreshToken(data.refreshToken || null);
            setIsAuthenticated(true);

            if (localStorage.getItem('rememberMe') === 'true' && user) {
                localStorage.setItem("user", JSON.stringify({ ...user, refreshToken: data.refreshToken || null }));
            }

            navigate("/homepage");
        } catch (error) {
            console.error('Token refresh failed:', error);
            // Don't logout immediately—let user retry manually
            return false;
        }
    };

    return (
        <AuthContext.Provider
            value={{
                user,
                token,
                refreshToken,
                isAuthenticated,
                login,
                register,
                logout,
                refreshAuthToken,
                setUser,
            }}
        >
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
};