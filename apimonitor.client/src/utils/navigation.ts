import { useNavigate } from 'react-router-dom';

const useCustomNavigate = () => {
    const navigate = useNavigate();

    const redirectToLogin = () => {
        navigate('/login');
    };

    const redirectToHome = () => {
        navigate('/homepage');
    };

    const redirectToErrorPage = (errorCode: string) => {
        navigate(`/error/${errorCode}`);
    };

    return { redirectToLogin, redirectToHome, redirectToErrorPage };
};

export default useCustomNavigate;