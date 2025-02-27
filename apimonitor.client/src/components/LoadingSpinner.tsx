import React from 'react';
import { motion } from 'framer-motion';

const LoadingSpinner: React.FC = () => {
    return (
        <div className="flex justify-center items-center mt-8">
            <motion.div
                animate={{ rotate: 360 }}
                transition={{
                    repeat: Infinity,
                    repeatType: 'loop',
                    duration: 1,
                    ease: 'linear',
                }}
                className="w-16 h-16 border-4 border-t-4 border-blue-500 rounded-full"
            />
        </div>
    );
};

export default LoadingSpinner;
