import React, { createContext, useState, useEffect, ReactNode, useContext } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { User, AuthContextType } from "../types/AuthTypes.ts";

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(localStorage.getItem('accessToken'));
    const [refreshToken, setRefreshToken] = useState<string | null>(localStorage.getItem('refreshToken'));
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
            const storedToken = localStorage.getItem('accessToken');
            const storedRefresh = localStorage.getItem('refreshToken');
            const rememberMe = localStorage.getItem('rememberMe') === 'true';

            console.log("Initializing auth:", { storedUser, storedToken, storedRefresh, rememberMe });

            if (storedUser && storedToken && rememberMe) {
                const parsedUser: User = JSON.parse(storedUser);
                setUser(parsedUser);
                setToken(storedToken);
                setRefreshToken(storedRefresh);

                const isTokenValid = await verifyToken(storedToken);
                console.log("Token valid:", isTokenValid);

                if (isTokenValid) {
                    setIsAuthenticated(true);
                    if (location.pathname === '/login' || location.pathname === '/') {
                        navigate("/homepage");
                    }
                } else if (storedRefresh) {
                    try {
                        await refreshAuthToken(storedRefresh);
                    } catch {
                        logout();
                    }
                } else {
                    logout();
                }
            } else if (!isAuthenticated && location.pathname !== '/register') {
                navigate("/login");
            }
        };

        initializeAuth();
    }, []);

    const login = async (email: string, password: string, rememberMe: boolean) => {
        const response = await fetch("http://localhost:5028/api/Login/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, password, rememberMe }),
            credentials: 'include',
        });

        if (!response.ok) throw new Error((await response.json()).message || "Login failed");

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
            roles: data.user.roles || ['USER'],
        };

        const accessToken = data.accessToken;
        setUser(user);
        setToken(accessToken);
        setRefreshToken(data.refreshToken || null);
        setIsAuthenticated(true);

        if (rememberMe) {
            localStorage.setItem("user", JSON.stringify(user));
            localStorage.setItem("accessToken", accessToken);
            localStorage.setItem("refreshToken", data.refreshToken || '');
            localStorage.setItem('rememberMe', 'true');
        } else {
            localStorage.removeItem('user');
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            localStorage.removeItem('rememberMe');
        }

        console.log("User set:", JSON.stringify(user, null, 2));
        navigate("/homepage");
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
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ refreshToken: existingRefreshToken }),
                credentials: 'include',
            });

            if (!response.ok) throw new Error("Token refresh failed");

            const data = await response.json();
            const newToken = data.accessToken;
            setToken(newToken);
            setRefreshToken(data.refreshToken || null);
            setIsAuthenticated(true);

            if (localStorage.getItem('rememberMe') === 'true' && user) {
                localStorage.setItem("accessToken", newToken);
                localStorage.setItem("refreshToken", data.refreshToken || '');
                localStorage.setItem("user", JSON.stringify({ ...user, refreshToken: data.refreshToken || null }));
            }

            if (location.pathname === '/login') navigate("/homepage");
        } catch (error) {
            console.error('Token refresh failed:', error);
            logout();
        }
    };

    return (
        <AuthContext.Provider value={{ user, token, refreshToken, isAuthenticated, login, logout, refreshAuthToken, setUser }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (!context) throw new Error("useAuth must be used within an AuthProvider");
    return context;
};