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

    // Form data state
    const [formData, setFormData] = useState(initialFormData);
    // General error state (e.g., for server errors)
    const [error, setError] = useState<string | null>(null);
    // Real-time validation errors for each field
    const [formErrors, setFormErrors] = useState({
        email: "",
        password: "",
        confirmPassword: "",
    });
    // Real-time password match feedback
    const [passwordMatchMessage, setPasswordMatchMessage] = useState<string>("");

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value, type, checked } = e.target;

        setFormData(prev => ({
            ...prev,
            [name]: type === "checkbox" ? checked : value,
        }));

        // Real-time validation for email
        if (name === "email") {
            const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
            setFormErrors(prev => ({
                ...prev,
                email: !value
                    ? "Email is required."
                    : !emailRegex.test(value)
                    ? "Invalid email format."
                    : "",
            }));
        }
        // Real-time validation for password
        else if (name === "password") {
            const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
            setFormErrors(prev => ({
                ...prev,
                password: !value
                    ? "Password is required."
                    : value.length < 8
                    ? "Password must be at least 8 characters long."
                    : !passwordRegex.test(value)
                    ? "Password must include uppercase, lowercase, digit, and special character (@$!%*?&)."
                    : "",
            }));
            // Update confirmPassword validation if password changes
            setFormErrors(prev => ({
                ...prev,
                confirmPassword: !formData.confirmPassword
                    ? "Please confirm your password."
                    : formData.confirmPassword !== value
                    ? "Passwords do not match."
                    : "",
            }));
            // Update password match message
            setPasswordMatchMessage(
                !formData.confirmPassword
                    ? ""
                    : formData.confirmPassword !== value
                    ? "Passwords do not match"
                    : "Passwords match"
            );
        }
        // Real-time validation for confirmPassword
        else if (name === "confirmPassword") {
            setFormErrors(prev => ({
                ...prev,
                confirmPassword: !value
                    ? "Please confirm your password."
                    : value !== formData.password
                    ? "Passwords do not match."
                    : "",
            }));
            // Update password match message
            setPasswordMatchMessage(
                !value
                    ? ""
                    : value !== formData.password
                    ? "Passwords do not match"
                    : "Passwords match"
            );
        }

        if (error) {
            setError(null);
        }

        if (name === "rememberMe") {
            localStorage.setItem("rememberMe", checked.toString());
        }
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();

        // Required field check
        if (!formData.email || !formData.password || !formData.confirmPassword) {
            setError("Please fill in all fields.");
            return;
        }

        // Email validation (match server regex)
        const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
        if (!emailRegex.test(formData.email)) {
            setError("Invalid email format. Must be a valid email with a domain (e.g., user@domain.com).");
            return;
        }

        // Password validation (match server regex)
        const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
        if (formData.password.length < 8) {
            setError("Password must be at least 8 characters long.");
            return;
        }
        if (!passwordRegex.test(formData.password)) {
            setError("Password must include at least one uppercase letter, one lowercase letter, one digit, and one special character (@$!%*?&).");
            return;
        }

        // Confirm password match
        if (formData.password !== formData.confirmPassword) {
            setError("Passwords do not match!");
            return;
        }

        try {
            const { email, password, confirmPassword, rememberMe } = formData;
            await register(email, password, confirmPassword, rememberMe);
            setError(null);
            redirectToHome();
        } catch (error: any) {
            console.error("Fetch error:", error);
            const serverError = error.message?.includes("Password must have")
                ? error.message
                : "Error occurred while making the request.";
            setError(serverError);
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
                        "0px 5px 5px rgba(0, 0, 0, 0.6), 0px 0px 10px rgba(0, 128, 0, 0.8)",
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
                <form onSubmit={handleRegister} className="space-y-4">
                    <div className="mb-4">
                        <label className="block mb-2 text-green-400 font-bold">Email</label>
                        <motion.input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            className={`w-full p-3 bg-gray-800 text-green-400 placeholder-green-600 border ${
                                formErrors.email ? "border-red-500" : "border-green-500"
                            } rounded-md focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-700`}
                            whileFocus={{ scale: 1.02 }}
                            transition={{ duration: 0.2 }}
                            placeholder="Enter your email"
                            required
                            aria-label="Email address"
                        />
                        {formErrors.email && (
                            <p className="text-red-500 text-sm mt-1">{formErrors.email}</p>
                        )}
                    </div>

                    <div className="mb-4">
                        <label className="block mb-2 text-green-400 font-bold">Password</label>
                        <motion.input
                            type="password"
                            name="password"
                            value={formData.password}
                            onChange={handleChange}
                            className={`w-full p-3 bg-gray-800 text-green-400 placeholder-green-600 border ${
                                formErrors.password ? "border-red-500" : "border-green-500"
                            } rounded-md focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-700`}
                            whileFocus={{ scale: 1.02 }}
                            transition={{ duration: 0.2 }}
                            placeholder="Enter your password"
                            required
                            aria-label="Password"
                        />
                        {formErrors.password && (
                            <p className="text-red-500 text-sm mt-1">{formErrors.password}</p>
                        )}
                    </div>

                    <div className="mb-4">
                        <label className="block mb-2 text-green-400 font-bold">Confirm Password</label>
                        <motion.input
                            type="password"
                            name="confirmPassword"
                            value={formData.confirmPassword}
                            onChange={handleChange}
                            className={`w-full p-3 bg-gray-800 text-green-400 placeholder-green-600 border ${
                                formErrors.confirmPassword ? "border-red-500" : "border-green-500"
                            } rounded-md focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-700`}
                            whileFocus={{ scale: 1.02 }}
                            transition={{ duration: 0.2 }}
                            placeholder="Confirm your password"
                            required
                            aria-label="Confirm password"
                        />
                        {formErrors.confirmPassword && (
                            <p className="text-red-500 text-sm mt-1">{formErrors.confirmPassword}</p>
                        )}
                        {passwordMatchMessage && (
                            <motion.span
                                className={`block text-sm mt-1 ${
                                    passwordMatchMessage === "Passwords match"
                                        ? "text-green-400"
                                        : "text-red-500"
                                }`}
                                initial={{ opacity: 0 }}
                                animate={{ opacity: 1 }}
                                transition={{ duration: 0.3 }}
                            >
                                {passwordMatchMessage}
                            </motion.span>
                        )}
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
                                aria-label="Remember me"
                            />
                            <span className="ml-2 pb-3">Remember me</span>
                        </label>
                    </div>

                    <motion.button
                        type="submit"
                        className="w-full py-3 bg-green-500 text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-green-600 hover:scale-105 disabled:bg-gray-600 disabled:cursor-not-allowed"
                        whileHover={{ scale: 1.05 }}
                        transition={{ duration: 0.2 }}
                        disabled={
                            !!error ||
                            !formData.email ||
                            !formData.password ||
                            !formData.confirmPassword ||
                            formData.password !== formData.confirmPassword ||
                            !!formErrors.email ||
                            !!formErrors.password ||
                            !!formErrors.confirmPassword
                        }
                    >
                        Submit
                    </motion.button>

                    <motion.button
                        type="button"
                        onClick={() => navigate("/login")}
                        className="w-full py-3 bg-green-500 text-gray-900 font-semibold rounded-md shadow-md transition-all ease-in-out hover:bg-green-600 hover:scale-105"
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