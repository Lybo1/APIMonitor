
export const setToken = (accessToken: string, refreshToken: string, rememberMe: boolean) => {
    if (rememberMe) {
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
    } else {
        sessionStorage.removeItem('accessToken');
        sessionStorage.removeItem('refreshToken');
    }
};

export const getToken = () => {
    return localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken');
};

export const getRefreshToken = () => {
    return localStorage.getItem('refreshToken') || sessionStorage.getItem('refreshToken');
};

export const clearAuthTokens = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('refreshToken');
};