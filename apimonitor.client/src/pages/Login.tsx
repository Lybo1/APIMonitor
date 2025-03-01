import React, { useState } from "react";
import { motion } from "framer-motion";
import { useAuth } from "../context/AuthContext.tsx";
import Heading from "../components/Heading.tsx";

const LoginPage: React.FC = () => {
    const { login } = useAuth();

    const initialFormData = {
        email: "",
        password: "",
        rememberMe: localStorage.getItem("rememberMe") === "true",
    };

    const [formData, setFormData] = useState(initialFormData);
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value, type, checked } = e.target;

        setFormData(prev => ({
            ...prev,
            [name]: type === "checkbox" ? checked : value,
        }));

        if (error) {
            setError(null);
        }

        if (name === "rememberMe") {
            localStorage.setItem("rememberMe", checked.toString());
        }
    };

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!formData.email || !formData.password) {
            setError("Please fill in all fields.");
            return;
        }

        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!emailRegex.test(formData.email)) {
            setError("Invalid email format.");
            return;
        }

        setLoading(true);

        try {
            await login(formData.email, formData.password, formData.rememberMe);
        } catch (error) {
            console.error("Fetch error:", error);
            setError("An error occurred while making the request.");
        } finally {
            setLoading(false);
        }
    };

    const closeModal = () => {
        setError(null);
    };

    return (
        <div className="flex items-center justify-center min-h-screen text-center">
           <Heading/>

            <div className="w-1/2 flex justify-center items-center">
                <form
                    onSubmit={handleLogin}
                    className="backdrop-blur-md bg-gray-800/20 border border-white/30 rounded-2xl p-8 max-w-md w-full text-white shadow-lg transition-transform transform hover:scale-105 hover:shadow-2xl"
                >
                    <div className="mb-4 text-gray-900 font-bold">
                        <label className="block mb-2">Email</label>
                        <div>
                            <input
                                type="email"
                                name="email"
                                value={formData.email}
                                onChange={handleChange}
                                className="w-full p-3 bg-white/20 text-white placeholder-gray-300 border border-white/30 rounded-md focus:outline-none focus:ring-2 focus:ring-white transition-all ease-in-out hover:bg-white/25"
                            />
                        </div>
                    </div>

                    <div className="mb-4 text-gray-900 font-bold">
                        <label className="block mb-2">Password</label>
                        <div>
                            <input
                                type="password"
                                name="password"
                                value={formData.password}
                                onChange={handleChange}
                                className="w-full p-3 bg-white/20 text-white placeholder-gray-300 border border-white/30 rounded-md focus:outline-none focus:ring-2 focus:ring-white transition-all ease-in-out hover:bg-white/25"
                            />
                        </div>
                    </div>

                    <div className="flex justify-center items-center mt-6 mb-4">
                        <label className="flex items-center text-gray-900 font-bold">
                            <input
                                type="checkbox"
                                name="rememberMe"
                                value="true"
                                checked={formData.rememberMe}
                                onChange={handleChange}
                                className="accent-white"
                            />
                            <span className="ml-2">Remember Me</span>
                        </label>
                    </div>

                    <div>
                        <button
                            type="submit"
                            disabled={loading || !formData.email || !formData.password}
                            className="w-full py-3 bg-white text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-gray-100 hover:scale-105"
                        >
                            {loading ? "Logging in.." : "Submit"}
                        </button>
                    </div>
                </form>
            </div>

            {error && (
                <div className="fixed inset-0 flex justify-center items-center bg-black/50 z-50">
                    <motion.div className="bg-red-600 text-white p-6 rounded-lg shadow-lg max-w-md w-full text-center"
                                initial={{ scale: 0.8 }}
                                animate={{ scale: 1 }}
                    >
                        <p className="mb-4">{error}</p>
                        <button
                            onClick={closeModal}
                            className="bg-white text-gray-900 px-4 py-2 rounded-md font-semibold transition-all ease-in-out hover:bg-gray-100"
                        >
                            Close
                        </button>
                    </motion.div>
                </div>
            )}
        </div>
    );
};

export default LoginPage;
