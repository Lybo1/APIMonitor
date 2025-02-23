import React, { createContext, useState, useEffect, ReactNode } from 'react';
import { authService } from "../services/authService.ts";
import { refreshTokenService } from "../services/refreshTokenService.ts";
import { isAccessTokenExpired } from "../services/tokenService.ts";
import { User, AuthContextType } from "../types/AuthTypes.ts";
import useCustomNavigate from "../utils/navigation.ts";

interface AuthProviderProps {
    children: ReactNode;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken'));
    const [refreshToken, setRefreshToken] = useState<string | null>(localStorage.getItem('refreshToken') || sessionStorage.getItem('refreshToken'));
    const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const { redirectToHome } = useCustomNavigate();

    useEffect(() => {
        const checkAuthStatus = async () => {
            try {
                if (token && refreshToken && !isAccessTokenExpired(token)) {
                    setIsAuthenticated(true);
                    const fetchedUser = await authService.getUserData();
                    setUser(fetchedUser);
                } else {
                    setIsAuthenticated(false);
                    await refreshTokensIfNeeded();
                }
            } catch {
                setIsAuthenticated(false);
            } finally {
                setIsLoading(false);
            }
        };

        checkAuthStatus();
    }, [token, refreshToken]);

    const login = async (email: string, password: string, rememberMe: boolean) => {
        try {
            const response = await authService.login(email, password, rememberMe);
            const { accessToken, refreshToken, accessTokenExpiry } = response;

            if (rememberMe) {
                localStorage.setItem('accessToken', accessToken);
                localStorage.setItem('refreshToken', refreshToken);
                localStorage.setItem('accessTokenExpiry', accessTokenExpiry);
            } else {
                sessionStorage.setItem('accessToken', accessToken);
                sessionStorage.setItem('refreshToken', refreshToken);
                sessionStorage.setItem('accessTokenExpiry', accessTokenExpiry);
            }

            setToken(accessToken);
            setRefreshToken(refreshToken);
            setIsAuthenticated(true);

            const fetchedUser = await authService.getUserData();
            setUser(fetchedUser);
        } catch (error) {
            console.error("Login failed:", error);
            throw error;
        }
    };

    const register = async (email: string, password: string, confirmPassword: string, rememberMe: boolean) => {
        try {
            const response = await authService.register(email, password, confirmPassword, rememberMe);
            const { accessToken, refreshToken, accessTokenExpiry } = response;

            if (rememberMe) {
                localStorage.setItem("accessToken", accessToken);
                localStorage.setItem("refreshToken", refreshToken);
                localStorage.setItem("accessTokenExpiry", accessTokenExpiry);
            } else {
                sessionStorage.setItem("accessToken", accessToken);
                sessionStorage.setItem("refreshToken", refreshToken);
                sessionStorage.setItem("accessTokenExpiry", accessTokenExpiry);
            }

            setToken(accessToken);
            setRefreshToken(refreshToken);
            setIsAuthenticated(true);

            const fetchedUser = await authService.getUserData();
            setUser(fetchedUser);
        } catch (error) {
            console.error("Registration failed:", error);
            throw error;
        }
    };

    const logout = async () => {
        await authService.logout();
        setToken(null);
        setRefreshToken(null);
        setIsAuthenticated(false);
        setUser(null);
        redirectToHome();
    };

    const refreshAuthToken = async () => {
        const newAccessToken = await authService.refreshToken();
        setToken(newAccessToken);
        return newAccessToken;
    };

    const refreshTokensIfNeeded = async () => {
        if (isAccessTokenExpired(token!)) {
            const newTokens = await refreshTokenService.refresh();
            const { accessToken, refreshToken } = newTokens;
            setToken(accessToken);
            setRefreshToken(refreshToken);

            localStorage.setItem('accessToken', accessToken);
            localStorage.setItem('refreshToken', refreshToken);

            const fetchedUser = await authService.getUserData();
            setUser(fetchedUser);
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
            {!isLoading && children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = React.useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};
