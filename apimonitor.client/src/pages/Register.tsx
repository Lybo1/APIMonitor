"use client";
import React, { useState } from "react";
import { motion } from "framer-motion";
import useCustomNavigate from "../utils/navigation.ts";

const RegisterPage: React.FC = () => {
    const { redirectToHome } = useCustomNavigate();

    const [formData, setFormData] = useState({
        email: "",
        password: "",
        confirmPassword: "",
        rememberMe: true,
    });

    const [error, setError] = useState<string | null>(null);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value, type } = e.target;

        setFormData(prev => ({
            ...prev,
            [name]: type === "radio" ? value === "true" : value,
        }));

        if (error) {
            setError(null);
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
            const response = await fetch("http://localhost:5028/api/Register/register", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    email: formData.email,
                    password: formData.password,
                    confirmPassword: formData.confirmPassword,
                    rememberMe: formData.rememberMe,
                }),
            });

            if (!response.ok) {
                const errorData = await response.json();
                console.error("Server response error:", errorData);
                setError(errorData?.message || "An error occurred.");
                return;
            }

            const responseData = await response.json();

            if (responseData.message === "User registered successfully.") {
                console.log("Registration success", responseData);
                redirectToHome();
            } else {
                setError("Unexpected response from server.");
            }
        } catch (error) {
            console.error("Fetch error:", error);
            setError("Error occurred while making the request.");
        }
    };

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
                                        checked={formData.rememberMe}
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
                                        checked={formData.rememberMe}
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
                            disabled={!!error || !formData.email || !formData.password || !formData.confirmPassword}
                            className="w-full py-3 bg-white text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-gray-100 hover:scale-105"
                        >
                            Submit
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

export default RegisterPage;
