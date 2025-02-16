import axiosInstance from './axiosInstance';

class AuthService {
    async register(email: string, password: string, rememberMe: boolean) {
        const response = await axiosInstance.post('/register/register', {
            email,
            password,
            rememberMe,
        });

        if (response.data.accessToken && response.data.refreshToken) {
            this.storeTokens(response.data.accessToken, response.data.refreshToken, rememberMe);
        }

        return response.data;
    }

    // Store access and refresh tokens
    private storeTokens(accessToken: string, refreshToken: string, rememberMe: boolean) {
        if (rememberMe) {
            document.cookie = `refreshToken=${refreshToken}; path=/; secure; HttpOnly; SameSite=Strict`;
        } else {
            localStorage.setItem('accessToken', accessToken);
        }
    }

    getAccessToken() {
        return localStorage.getItem('accessToken');
    }

    private getRefreshToken() {
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            cookie = cookie.trim();
            if (cookie.startsWith('refreshToken=')) {
                return cookie.substring('refreshToken='.length, cookie.length);
            }
        }
        return null;
    }

    logout() {
        localStorage.removeItem('accessToken');
        document.cookie = 'refreshToken=; path=/; secure; HttpOnly; SameSite=Strict; expires=Thu, 01 Jan 1970 00:00:00 GMT';
    }

    // Refresh the token
    async refreshTokens() {
        const refreshToken = this.getRefreshToken();

        if (refreshToken) {
            try {
                const response = await axiosInstance.post('/refresh/refresh', {}, {
                    headers: { Authorization: `Bearer ${refreshToken}` },
                });
                this.storeTokens(response.data.accessToken, response.data.refreshToken, true);
                return response.data.accessToken;
            } catch {
                this.logout();
                throw new Error('Session expired, please log in again.');
            }
        }
        throw new Error('No refresh token found.');
    }
}

export const authService = new AuthService();
