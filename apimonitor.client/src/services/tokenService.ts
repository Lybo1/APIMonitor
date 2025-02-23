import { jwtDecode } from "jwt-decode";

// const decodeAccessToken = (token: string) => {
//     try {
//         const decodedToken = jwt_decode(token);
//
//         return decodedToken as { exp: number };
//     } catch {
//         return null;
//     }
// }
//
// export const isAccessTokenExpired = (token: string): boolean => {
//     const decodedToken = decodeAccessToken(token);
//
//     if (decodedToken && decodedToken.exp) {
//         const currentTime = Math.floor(Date.now() / 1000);
//
//         return decodedToken.exp < currentTime;
//     }
//
//     return true;
// }

export const isAccessTokenExpired =  (token: string): boolean => {
    try {
        const decodedToken: any = jwtDecode(token);
        const expiry = decodedToken.exp;

        if (!expiry) {
            return true;
        }

        return Date.now() >= expiry * 1000;
    } catch {
        return true;
    }
}