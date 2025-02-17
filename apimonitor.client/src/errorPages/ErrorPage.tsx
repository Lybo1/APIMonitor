import React from 'react';
import { useLocation } from 'react-router-dom';

const ErrorPage: React.FC = () => {
    const location = useLocation();
    const statusCode = location.state?.statusCode || 500;
    const message = location.state?.message || 'Something went wrong, please try again later.';

    return (
        <div>
            <h1>{statusCode} - Error</h1>
            <p>{message}</p>
        </div>
    );
};

export default ErrorPage;