import { useContext, ReactNode } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { ThemeContext } from "../context/ThemeContext";


const ThemedWrapper = ({ children }: { children: ReactNode }) => {
    const { currentTheme } = useContext(ThemeContext);

    return (
        <AnimatePresence mode="wait">
            <motion.div
                key={currentTheme}
                initial={{ opacity: 0, scale: 0.98 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.98 }}
                transition={{ duration: 0.5, ease: "easeInOut" }}
                className="min-h-screen transition-all"
            >
                {children}
            </motion.div>
        </AnimatePresence>
    );

}

export default ThemedWrapper;