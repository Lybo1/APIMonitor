import React, { useState } from "react";
import { motion } from "framer-motion";
import { useAuth } from "../context/AuthContext.tsx";
import Heading from "../components/Heading.tsx";
import { useNavigate } from 'react-router-dom';

const LoginPage: React.FC = () => {
    const { login } = useAuth();
    const navigate = useNavigate();
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
        if (error) setError(null);
        if (name === "rememberMe") localStorage.setItem("rememberMe", checked.toString());
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
            setError("Username or password is incorrect"); // Updated error message
        } finally {
            setLoading(false);
        }
    };

    const closeModal = () => setError(null);

    return (
        <div className="flex flex-col items-center justify-center min-h-screen bg-gray-900 text-green-400 font-mono tracking-wide p-4">
            <Heading />
            <motion.div
                className="w-full max-w-md bg-black p-6 rounded-lg border-4 border-green-500 shadow-lg tracking-wide"
                initial={{ opacity: 0, y: -20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5, ease: "easeOut" }}
            >
                <form
                    onSubmit={handleLogin}
                    className="space-y-4"
                >
                    <div className="mb-4">
                        <label className="block mb-2 text-green-400 font-bold">Email</label>
                        <motion.input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            className="w-full p-3 bg-gray-800 text-green-400 placeholder-green-600 border border-green-500 rounded-md focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-700"
                            whileFocus={{ scale: 1.02 }}
                            transition={{ duration: 0.2 }}
                            placeholder="Enter your email"
                        />
                    </div>
                    <div className="mb-4">
                        <label className="block mb-2 text-green-400 font-bold">Password</label>
                        <motion.input
                            type="password"
                            name="password"
                            value={formData.password}
                            onChange={handleChange}
                            className="w-full p-3 bg-gray-800 text-green-400 placeholder-green-600 border border-green-500 rounded-md focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-700"
                            whileFocus={{ scale: 1.02 }}
                            transition={{ duration: 0.2 }}
                            placeholder="Enter your password"
                        />
                    </div>
                    <div className="flex items-center mb-4">
                        <label className="flex items-center text-green-400 font-bold">
                            <motion.input
                                type="checkbox"
                                name="rememberMe"
                                checked={formData.rememberMe}
                                onChange={handleChange}
                                className="accent-green-400 ml-2"
                                whileTap={{ scale: 1.1 }}
                                transition={{ duration: 0.2 }}
                            />
                            <span className=" ml-2 pb-3">Remember me</span>
                        </label>
                    </div>
                    <div className="space-y-4">
                        <motion.button
                            type="submit"
                            disabled={loading || !formData.email || !formData.password}
                            className="w-full py-3 bg-green-500 text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-green-600 hover:scale-105 disabled:bg-gray-600 disabled:cursor-not-allowed"
                            whileHover={{ scale: 1.05 }}
                            transition={{ duration: 0.2 }}
                        >
                            {loading ? "Logging in..." : "Submit"}
                        </motion.button>
                        <motion.button
                            type="button"
                            onClick={() => navigate('/register')}
                            className="w-full py-3 bg-green-500 text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-green-600 hover:scale-105"
                            whileHover={{ scale: 1.05 }}
                            transition={{ duration: 0.2 }}
                        >
                            Register
                        </motion.button>
                    </div>
                </form>
            </motion.div>
            {error && (
                <motion.div
                    className="fixed inset-0 flex items-center justify-center bg-black/50 z-50"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                >
                    <motion.div
                        className="bg-gray-800 p-6 rounded-lg border-4 border-red-500 shadow-lg max-w-md w-full text-center text-green-400 font-mono tracking-wide"
                        initial={{ scale: 0.9 }}
                        animate={{ scale: 1 }}
                        transition={{ duration: 0.3 }}
                    >
                        <p className="mb-4">{error}</p>
                        <motion.button
                            onClick={closeModal}
                            className="bg-red-500 text-gray-900 px-4 py-2 rounded-md font-semibold transition-all ease-in-out hover:bg-red-600 hover:scale-105"
                            whileHover={{ scale: 1.05 }}
                            transition={{ duration: 0.2 }}
                        >
                            Close
                        </motion.button>
                    </motion.div>
                </motion.div>
            )}
        </div>
    );
};

export default LoginPage;