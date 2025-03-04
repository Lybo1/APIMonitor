import React, { useState } from "react";
import { motion } from "framer-motion";
import useCustomNavigate from "../utils/navigation.ts";
import { useAuth } from "../context/AuthContext.tsx";
import { useNavigate } from "react-router-dom";

const RegisterPage: React.FC = () => {
    const { register } = useAuth();
    const navigate = useNavigate();
    const { redirectToHome } = useCustomNavigate();

    const initialFormData = {
        email: "",
        password: "",
        confirmPassword: "",
        rememberMe: localStorage.getItem("rememberMe") === "true",
    };

    const [formData, setFormData] = useState(initialFormData);
    const [error, setError] = useState<string | null>(null);

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
            localStorage.setItem("rememberMe", checked.toString()); // Fixed to use checked.toString()
        }
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!formData.email || !formData.password || !formData.confirmPassword) {
            setError("Please fill in all fields.");
            return;
        }

        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!emailRegex.test(formData.email)) {
            setError("Invalid email format.");
            return;
        }

        if (formData.password !== formData.confirmPassword) {
            setError("Passwords do not match!");
            return;
        }

        try {
            const { email, password, confirmPassword, rememberMe } = formData;
            await register(email, password, confirmPassword, rememberMe);
            setError(null);
            redirectToHome();
        } catch (error) {
            console.error("Fetch error:", error);
            setError("Error occurred while making the request.");
        }
    };

    const closeModal = () => {
        setError(null);
    };

    return (
        <div className="flex flex-col items-center justify-center min-h-screen bg-gray-900 text-green-400 font-mono tracking-wide p-4">
            <motion.h1
                className="text-7xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-green-500 via-green-400 to-green-300 mb-8 font-mono tracking-wide"
                initial={{ scale: 1 }}
                animate={{ scale: [1, 1.05, 1] }}
                transition={{
                    duration: 1,
                    repeat: Infinity,
                    repeatDelay: 0,
                    ease: "easeInOut",
                }}
                style={{
                    textShadow:
                        "0px 5px 5px rgba(0, 0, 0, 0.6), 0px 0px 10px rgba(0, 128, 0, 0.8)", // Green glow
                }}
            >
                API Monitor
            </motion.h1>

            <motion.div
                className="w-full max-w-md bg-black p-6 rounded-lg border-4 border-green-500 shadow-lg tracking-wide"
                initial={{ opacity: 0, y: -20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5, ease: "easeOut" }}
            >
                <form
                    onSubmit={handleRegister}
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

                    <div className="mb-4">
                        <label className="block mb-2 text-green-400 font-bold">Confirm Password</label>
                        <motion.input
                            type="password"
                            name="confirmPassword"
                            value={formData.confirmPassword}
                            onChange={handleChange}
                            className="w-full p-3 bg-gray-800 text-green-400 placeholder-green-600 border border-green-500 rounded-md focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-700"
                            whileFocus={{ scale: 1.02 }}
                            transition={{ duration: 0.2 }}
                            placeholder="Confirm your password"
                        />
                    </div>

                    <div className="flex items-center mb-4">
                        <label className="flex items-center text-green-400 font-bold">
                            <motion.input
                                type="checkbox"
                                name="rememberMe"
                                checked={formData.rememberMe}
                                onChange={handleChange}
                                className="accent-green-400 mr-2"
                                whileTap={{ scale: 1.1 }}
                                transition={{ duration: 0.2 }}
                            />
                            <span className="ml-2 pb-3">Remember me</span>
                        </label>
                    </div>

                    <motion.button
                        type="submit"
                        disabled={!!error || !formData.email || !formData.password || !formData.confirmPassword}
                        className="w-full py-3 bg-green-500 text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-green-600 hover:scale-105 disabled:bg-gray-600 disabled:cursor-not-allowed"
                        whileHover={{ scale: 1.05 }}
                        transition={{ duration: 0.2 }}
                    >
                        Submit
                    </motion.button>

                    <motion.button
                        type="submit"
                        onClick={() => navigate("/login")}
                        className="w-full py-3 bg-green-500 text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-green-600 hover:scale-105 disabled:bg-gray-600 disabled:cursor-not-allowed"
                        whileHover={{ scale: 1.05 }}
                        transition={{ duration: 0.2 }}
                    >
                        Already have an account? Login
                    </motion.button>
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

export default RegisterPage;