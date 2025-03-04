import { motion } from "framer-motion";

const HeadingComponent = () => {
    return (
        <div className="flex flex-col justify-center items-center px-6 mb-8">
            <motion.h1
                className="text-7xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-green-500 via-green-400 to-green-300 mb-2 font-mono tracking-wide"
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
        </div>
    );
};

export default HeadingComponent;