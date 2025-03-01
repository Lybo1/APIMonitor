import {motion} from "framer-motion";

const HeadingComponent = () => {
    return (
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
    );
};

export default HeadingComponent;
