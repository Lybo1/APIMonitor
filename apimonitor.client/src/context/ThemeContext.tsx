import React, { createContext, useContext, useState, useEffect } from 'react';
import { motion, AnimatePresence } from "framer-motion";
import themes from '../config/themes.json';

type Theme = {
    name: string;
    id: string;
    background: string;
    text: string;
    primary: string;
};

type ThemeContextType = {
    theme: Theme;
    setTheme: (themeId: string) => void;
};

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
    const [theme, setTheme] = useState<Theme>(themes.themes[0]);

    const switchTheme = (themeId: string) => {
        const selectedTheme = themes.themes.find((t: Theme) => t.id === themeId); // Fixed 'thems' typo

        if (selectedTheme) {
            setTheme(selectedTheme);
        }
    };

    useEffect(() => {
        const storedTheme = localStorage.getItem("theme");

        if (storedTheme) {
            switchTheme(storedTheme);
        }
    }, []);

    return (
        <ThemeContext.Provider value={{ theme, setTheme: switchTheme }}>
            <AnimatePresence initial={false}>
                <motion.div
                    key={theme.id}
                    className={`${theme.background} ${theme.text} transition-all duration-300`}
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                >
                    {children}
                </motion.div>
            </AnimatePresence>
        </ThemeContext.Provider>
    );
};

export const useTheme = () => {
    const context = useContext(ThemeContext);

    if (!context) {
        throw new Error('useTheme must be used within ThemeProvider');
    }

    return context;
};
