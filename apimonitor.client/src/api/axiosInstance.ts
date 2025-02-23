import axios from 'axios'
import { getToken } from "../utils/authTokens.ts";
import useCustomNavigate from "../utils/navigation.ts";

const API_URL = '';

const axiosInstance = axios.create({
    baseURL: API_URL,
    timeout: 10000,
});

axiosInstance.interceptors.request.use(
    (config) => {
        const token = getToken();

        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

axiosInstance.interceptors.response.use(
    (response) => response,
    (error) => {
       const { response } = error;
       const { redirectToLogin } = useCustomNavigate();

       if (response) {
           switch (response.status) {
               case 401:
                   redirectToLogin();
                   break;
               case 403:
                   redirectToErrorPage('403');
                   break;
               case 404:
                   redirectToErrorPage('404');
                   break;
               case 500:
                   redirectToErrorPage('500');
                   break;
               default:
                   redirectToErrorPage('general');
                   break;
           }
       }
       return Promise.reject(error);
    }
);

const redirectToErrorPage = (errorCode: string) => {
    const { redirectToErrorPage } = useCustomNavigate();

    redirectToErrorPage(errorCode);
}

export default axiosInstance;