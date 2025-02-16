import axios from 'axios';
import { authService } from './authService';

const axiosInstance = axios.create({
    baseURL: 'https://your-api-url.com/api',
    withCredentials: true,
});

axiosInstance.interceptors.request.use(
    (config) => {
        const token = authService.getAccessToken();
        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
        }

        const csrfToken = getCSRFToken();
        if (csrfToken) {
            config.headers['X-CSRF-Token'] = csrfToken;
        }

        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

axiosInstance.interceptors.response.use(
    (response) => response,
    async (error) => {
        if (error.response?.status === 401) {
            try {
                const newAccessToken = await authService.refreshTokens();
                error.config.headers['Authorization'] = `Bearer ${newAccessToken}`;
                return axiosInstance(error.config);
            } catch {
                authService.logout();
                window.location.href = '/login';
            }
        }
        return Promise.reject(error);
    }
);

function getCSRFToken() {
    const csrfToken = document.cookie.split(';').find(cookie => cookie.trim().startsWith('XSRF-TOKEN='));
    return csrfToken ? csrfToken.split('=')[1] : null;
}

export default axiosInstance;
