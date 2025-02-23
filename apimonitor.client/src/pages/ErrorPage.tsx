import { useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import errorMessages from '../assets/errorMessages.json';
import useCustomNavigate from "../utils/navigation.ts";

type ErrorMessages = {
    [key: string]: string;
};

const ErrorPage = () => {
    const { errorCode } = useParams();
    const [errorMessage, setErrorMessage] = useState<string>('');
    const { redirectToHome } = useCustomNavigate();

    useEffect(() => {
        const messages: ErrorMessages = errorMessages;

        if (errorCode && messages[errorCode]) {
            setErrorMessage(messages[errorCode]);
        } else {
            setErrorMessage(messages['general']);
        }
    }, [errorCode]);

    return (
        <div className="flex flex-col items-center justify-center h-screen bg-gray-100 text-center">
            <h1 className="text-6xl font-bold text-red-600">{errorCode}</h1>
            <p className="text-lg text-gray-800">{errorMessage}</p>
            <button
                onClick={redirectToHome}
                className="mt-4 p-2 px-6 bg-blue-600 text-white rounded hover:bg-blue-700">
                Go to Home
            </button>
        </div>
    );
};

export default ErrorPage;