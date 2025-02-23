import axiosInstance from "../api/axiosInstance.ts";
import { authService } from './authService.ts';

const API_URL = '';

export const refreshTokenService = {
    async refresh() {
        try {
            const refreshToken = localStorage.getItem("refreshToken");

            if (!refreshToken) {
                throw new Error('Refresh token is missing. Please login again.');
            }

            const response = await axiosInstance.post(`${API_URL}/refresh`, {
                refreshToken
            });

            const { accessToken, refreshToken: newRefreshToken } = response.data;

            localStorage.setItem("accessToken", accessToken);

            if (newRefreshToken) {
                localStorage.setItem("refreshToken", newRefreshToken);
            }

            return { accessToken, refreshToken: newRefreshToken };
        } catch {
            await authService.logout();
            throw new Error("Unable to refresh token. Please login again.");
        }
    },
};

export default refreshTokenService;