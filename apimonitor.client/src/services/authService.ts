import axiosInstance from "../api/axiosInstance.ts";

const API_URL = 'http://localhost:5028/api/Register/register';

export const authService = {
    async login(email: string, password: string, rememberMe: boolean) {
        try {
            const response = await axiosInstance.post(`${API_URL}/login`, {
                email,
                password,
                rememberMe
            });

            const { accessToken, refreshToken, accessTokenExpiry } = response.data;

            if (rememberMe) {
                localStorage.setItem('accessToken', accessToken);
                localStorage.setItem('refreshToken', refreshToken);
                localStorage.setItem('accessTokenExpiry', accessTokenExpiry);
            } else {
                sessionStorage.setItem('accessToken', accessToken);
                sessionStorage.setItem('refreshToken', refreshToken);
                sessionStorage.setItem('accessTokenExpiry', accessTokenExpiry);
            }

            return response.data;
        } catch {
            throw new Error("Invalid credentials or server issue.");
        }
    },

    async logout() {
        try {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            localStorage.removeItem('accessTokenExpiry');

            sessionStorage.removeItem('accessToken');
            sessionStorage.removeItem('refreshToken');
            sessionStorage.removeItem('accessTokenExpiry');

            return { success: true };
        } catch {
            throw new Error("Error while logging out.");
        }
    },

    async register(email: string, password: string, confirmPassword: string, rememberMe: boolean) {
        try {
            const response = await axiosInstance.post(`${API_URL}/register`, {
                email,
                password,
                confirmPassword,
                rememberMe
            });

            const { accessToken, refreshToken } = response.data;

            localStorage.setItem("accessToken", accessToken);
            localStorage.setItem("refreshToken", refreshToken);

            return response.data;
        } catch {
            throw new Error("Error registering user.");
        }
    },

    async refreshToken() {
        try {
            const accessToken = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");

            if (!accessToken) {
                throw new Error('No access token available.');
            }

            const response = await axiosInstance.post(`${API_URL}/refreshToken`, {
                token: accessToken,
            });

            const { newAccessToken, newRefreshToken } = response.data;

            if (localStorage.getItem('accessToken')) {
                localStorage.setItem('accessToken', newAccessToken);
                localStorage.setItem('refreshToken', newRefreshToken);
            } else {
                sessionStorage.setItem('accessToken', newAccessToken);
                sessionStorage.setItem('refreshToken', newRefreshToken);
            }

            return response.data;
        } catch {
            throw new Error('Error refreshing the token.');
        }
    },

    async getUserData() {
        try {
            const accessToken = localStorage.getItem("accessToken");

            if (!accessToken) {
                throw new Error('No access token found.');
            }

            const response = await axiosInstance.get(`${API_URL}/user`, {
                headers: {
                    Authorization: `Bearer ${accessToken}`,
                },
            });

            return response.data;
        } catch {
            throw new Error('Failed to fetch user data.');
        }
    }
};