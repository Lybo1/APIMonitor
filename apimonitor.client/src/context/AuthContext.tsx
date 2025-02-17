import React, { createContext, useState, ReactNode, useEffect } from 'react';
import { authService } from "../services/authService.ts";

interface AuthContextType {
    isAuthenticated: boolean;
    login: () => void;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);

    useEffect(() => {
        const checkAuth = async () => {
            try {
                const accessToken = authService.getAccessToken();

                if (accessToken) {
                    setIsAuthenticated(true);
                }
            } catch {
                setIsAuthenticated(false);
            }
        };

        checkAuth();
    }, []);

    const login = () => setIsAuthenticated(true);
    const logout = () => {
        setIsAuthenticated(false);
    };

    return (
        <AuthContext.Provider value={{ isAuthenticated, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = React.useContext(AuthContext);

    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }

    return context;
};
