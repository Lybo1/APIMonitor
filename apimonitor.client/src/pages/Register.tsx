"use client";
import React, { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { motion } from "framer-motion";

const RegisterPage: React.FC = () => {
    const { register } = useAuth();
    const [formData, setFormData] = useState({
        email: "",
        password: "",
        confirmPassword: "",
        rememberMe: "true",
    });

    // Modal and error state
    const [error, setError] = useState<string | null>(null);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();

        // Validate form fields
        if (!formData.email || !formData.password || !formData.confirmPassword) {
            setError("Please fill in all fields.");
            return;
        }

        // Validate email format
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(formData.email)) {
            setError("Invalid email format.");
            return;
        }

        // Check if passwords match
        if (formData.password !== formData.confirmPassword) {
            setError("Passwords do not match!");
            return;
        }

        try {
            const rememberMeBool = formData.rememberMe === "true"; // Convert to boolean
            await register(
                formData.email,
                formData.password,
                formData.confirmPassword,
                rememberMeBool
            );
            console.log("Registration successful!");
        } catch (error) {
            setError("Registration failed. Please try again.");
        }
    };

    // Close modal
    const closeModal = () => {
        setError(null);
    };

    return (
        <div className="flex items-center justify-center min-h-screen text-center">
            <div className="flex flex-col justify-center items-center px-6">
                <motion.h1
                    className="text-7xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-gray-800 via-gray-600 to-gray-400 mb-2"
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
                            "0px 5px 5px rgba(0, 0, 0, 0.6), 0px 0px 10px rgba(255, 255, 255, 0.6)",
                    }}
                >
                    API Monitor
                </motion.h1>
            </div>

            <div className="w-1/2 flex justify-center items-center">
                <form
                    onSubmit={handleRegister}
                    className="backdrop-blur-md bg-gray-800/20 border border-white/30 rounded-2xl p-8 max-w-md w-full text-white shadow-lg transition-transform transform hover:scale-105 hover:shadow-2xl"
                >
                    <div className="mb-4 text-gray-900 font-bold">
                        <label className="block mb-2">Email</label>
                        <input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            className="w-full p-3 bg-white/20 text-white placeholder-gray-300 border border-white/30 rounded-md focus:outline-none focus:ring-2 focus:ring-white transition-all ease-in-out hover:bg-white/25"
                        />
                    </div>

                    <div className="mb-4 text-gray-900 font-bold">
                        <label className="block mb-2">Password</label>
                        <input
                            type="password"
                            name="password"
                            value={formData.password}
                            onChange={handleChange}
                            className="w-full p-3 bg-white/20 text-white placeholder-gray-300 border border-white/30 rounded-md focus:outline-none focus:ring-2 focus:ring-white transition-all ease-in-out hover:bg-white/25"
                        />
                    </div>

                    <div className="mb-4 text-gray-900 font-bold">
                        <label className="block mb-2">Confirm password</label>
                        <input
                            type="password"
                            name="confirmPassword"
                            value={formData.confirmPassword}
                            onChange={handleChange}
                            className="w-full p-3 bg-white/20 text-white placeholder-gray-300 border border-white/30 rounded-md focus:outline-none focus:ring-2 focus:ring-white transition-all ease-in-out hover:bg-white/25"
                        />
                    </div>

                    <div className="flex justify-center items-center mt-6 mb-4">
                        <div className="text-center">
                            <span className="block mb-2 text-gray-900 font-bold">Remember Me</span>
                            <div className="flex justify-center items-center space-x-4">
                                <label className="flex items-center space-x-2">
                                    <input
                                        type="radio"
                                        name="rememberMe"
                                        value="true"
                                        checked={formData.rememberMe === "true"}
                                        onChange={handleChange}
                                        className="accent-white peer"
                                    />
                                    <span>Yes</span>
                                </label>
                                <label className="flex items-center space-x-2">
                                    <input
                                        type="radio"
                                        name="rememberMe"
                                        value="false"
                                        checked={formData.rememberMe === "false"}
                                        onChange={handleChange}
                                        className="accent-white peer"
                                    />
                                    <span>No</span>
                                </label>
                            </div>
                        </div>
                    </div>

                    <div>
                        <button
                            type="submit"
                            className="w-full py-3 bg-white text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-gray-100 hover:scale-105"
                        >
                            Submit
                        </button>
                    </div>
                </form>
            </div>

            {error && (
                <div className="fixed inset-0 flex justify-center items-center bg-black/50 z-50">
                    <div className="bg-red-600 text-white p-6 rounded-lg shadow-lg max-w-md w-full text-center">
                        <p className="mb-4">{error}</p>
                        <button
                            onClick={closeModal}
                            className="bg-white text-gray-900 px-4 py-2 rounded-md font-semibold transition-all ease-in-out hover:bg-gray-100"
                        >
                            Close
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default RegisterPage;
