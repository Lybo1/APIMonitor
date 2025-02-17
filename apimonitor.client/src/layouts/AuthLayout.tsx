import React from "react";
import { Link } from "react-router-dom";

interface AuthLayoutProps {
    children: React.ReactNode;
}

const AuthLayout: React.FC<AuthLayoutProps> = ({ children }) => {
    return (
        <div className="min-h-screen bg-gray-100 flex items-center justify-center">
            <div className="bg-white shadow-md rounded-lg p-8 w-full sm:w-96">
                <div className="mb-4 text-center">
                    <h2 className="text-2xl font-semibold text-gray-900">Your App Name</h2>
                    <p className="text-gray-600">Welcome back! Please log in to continue.</p>
                </div>

                {children}

                <div className="mt-4 text-center">
                    <p className="text-sm text-gray-600">
                        Don’t have an account?
                        <Link to="/register" className="text-blue-500"> Register</Link>
                    </p>
                </div>
            </div>
        </div>
    );
};

export default AuthLayout;