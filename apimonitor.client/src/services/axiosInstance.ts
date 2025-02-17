import axios from 'axios';
import { authService } from './authService';
import { useNavigate } from 'react-router';

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
        const navigate = useNavigate();

        const statusCode = error.response?.status;

        switch (statusCode) {
            case 401:
                try {
                    const newAccessToken = await authService.refreshTokens();

                    error.config.headers['Authorization'] = `Bearer ${newAccessToken}`;

                    return axiosInstance(error.config);
                } catch {
                    authService.logout();

                    navigate('/login');
                }
                break;

            case 403:
                navigate('/error', {
                    state: {
                        statusCode: 403,
                        message: 'You do not have permission to access this resource.',
                    }
                });
                break;

            case 404:
                navigate('/error', {
                    state: {
                        statusCode: 404,
                        message: 'The resource you are looking for is not found.',
                    }
                });
                break;

            case 500:
                navigate('/error', {
                   state: {
                       statusCode: 500,
                       message: 'Something went wrong on our end. Please try again later.',
                   }
                });
                break;

            case 502:
                navigate('/error', {
                   state: {
                       statusCode: 502,
                       message: 'There was an issue communicating with the server. Please try again later.',
                   }
                });
                break;

            default:
                navigate('/error', {
                    state: {
                        statusCode: statusCode || 500,
                        message: error.response?.statusText || 'An unexpected error occurred. Please try again later.',
                    }
                });
                break;
        }

        return Promise.reject(error);
    }
);

const getCSRFToken = () => {
    const csrfToken = document.cookie.split(';').find(cookie => cookie.trim().startsWith('XSRF-TOKEN='));

    return csrfToken ? csrfToken.split('=')[1] : null;
};

export default axiosInstance;
