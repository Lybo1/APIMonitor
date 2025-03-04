export interface User {
    id: number;
    email: string;
    username: string;
    firstName: string;
    lastName: string;
    refreshToken: string;
    createdAt: string;
    refreshTokenExpiry: string;
    failedLoginAttempts: number;
    isLockedOut: boolean;
    isAdmin: boolean;
    roles: string[];
}

export interface AuthContextType {
    user: User | null;
    token: string | null;
    refreshToken: string | null;
    isAuthenticated: boolean;
    login: (email: string, password: string, rememberMe: boolean) => Promise<void>;
    logout: () => void;
    refreshAuthToken: (existingRefreshToken: string) => Promise<void>;
    register: (email: string, password: string, confirmPassword: string, rememberMe: boolean) => Promise<void>;
    setUser: React.Dispatch<React.SetStateAction<User | null>>;
}