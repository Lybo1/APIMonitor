import { useState } from "react";
import { motion } from "framer-motion";
import { animated, useSpring } from "react-spring";
import { toast } from "react-toastify";

const ScanForm: React.FC<{ triggerSingleScan: (apiUrl: string) => void }> = ({
                                                                                 triggerSingleScan,
                                                                             }) => {
    const [apiUrl, setApiUrl] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [showErrorModal, setShowErrorModal] = useState(false); // Error modal visibility state
    const [scanError, setScanError] = useState<string | null>(null); // Store error message

    // Handle form submission
    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (apiUrl) {
            setIsSubmitting(true);
            setShowErrorModal(false); // Reset modal visibility before new scan
            triggerSingleScan(apiUrl); // Trigger the scan
            toast.success("API scan in progress...");
        }
    };

    // Close modal
    const handleErrorClose = () => {
        setShowErrorModal(false); // Close error modal
        setScanError(null); // Clear error
    };

    // Cinematic animation for input field and button
    const inputAnimation = {
        initial: { opacity: 0, y: 50, scale: 0.5 },
        animate: { opacity: 1, y: 0, scale: 1 },
        transition: { duration: 1, ease: "easeOut" },
    };

    const buttonAnimation = {
        initial: { opacity: 0, y: 50, scale: 0.8 },
        animate: { opacity: 1, y: 0, scale: 1 },
        transition: { duration: 1.2, delay: 0.3, ease: "easeInOut" },
    };

    const springProps = useSpring({
        opacity: isSubmitting ? 0.4 : 1,
        transform: isSubmitting ? "scale(1.1)" : "scale(1)",
        config: { tension: 300, friction: 22 },
    });

    // Create an intense expanding wave loading animation
    const loadingAnimation = (
        <div className="relative flex justify-center items-center">
            <div className="absolute w-4 h-4 bg-blue-600 rounded-full animate-pulse"></div>
            <div className="absolute w-4 h-4 bg-blue-600 rounded-full animate-pulse delay-200"></div>
            <div className="absolute w-4 h-4 bg-blue-600 rounded-full animate-pulse delay-400"></div>
        </div>
    );

    return (
        <div className="w-full max-w-lg mx-auto py-12 px-6 bg-white shadow-xl rounded-lg border border-gray-200 transform transition-all duration-700 ease-in-out">
            {/* Cinematic form with slow-motion transitions */}
            <motion.form
                onSubmit={handleSubmit}
                className="flex flex-col items-center space-y-8"
                initial="initial"
                animate="animate"
            >
                {/* Input with slow motion cinematic effect */}
                <motion.input
                    type="text"
                    placeholder="Enter API URL"
                    value={apiUrl}
                    onChange={(e) => setApiUrl(e.target.value)}
                    className="py-3 px-5 rounded-full text-black w-full focus:ring-4 focus:ring-blue-600 outline-none shadow-lg"
                    {...inputAnimation}
                />

                {/* Cinematic button with epic hover effect */}
                <animated.button
                    type="submit"
                    className={`py-4 px-7 rounded-full font-bold text-white transition-all duration-300 transform ${
                        isSubmitting
                            ? "bg-gray-400 cursor-not-allowed"
                            : "bg-blue-500 hover:bg-blue-400"
                    } hover:scale-110 hover:rotate-6`}
                    disabled={isSubmitting}
                    style={springProps}
                    {...buttonAnimation}
                >
                    {isSubmitting ? loadingAnimation : "Scan Single API"}
                </animated.button>
            </motion.form>

            {/* Cinematic Error Modal */}
            {showErrorModal && scanError && (
                <motion.div
                    className="fixed inset-0 flex justify-center items-center bg-black/50 z-50"
                    initial={{ scale: 0.8 }}
                    animate={{ scale: 1, opacity: 1 }}
                    transition={{
                        duration: 1.5,
                        ease: "easeInOut",
                        delay: 0.5,
                        opacity: 1,
                    }}
                >
                    <div className="bg-red-600 text-white p-8 rounded-lg shadow-lg text-center transform transition-all duration-700 ease-in-out">
                        <motion.p
                            className="text-xl font-semibold"
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            transition={{ delay: 0.5 }}
                        >
                            {scanError}
                        </motion.p>
                        <motion.button
                            onClick={handleErrorClose}
                            className="mt-6 bg-white text-red-600 rounded-full py-3 px-8 hover:bg-red-100 transition-all duration-300"
                            whileHover={{ scale: 1.1, rotate: 10 }}
                            transition={{ duration: 0.2 }}
                        >
                            Close
                        </motion.button>
                    </div>
                </motion.div>
            )}
        </div>
    );
};

export default ScanForm;
