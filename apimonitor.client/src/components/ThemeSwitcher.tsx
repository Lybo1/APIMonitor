import { useContext, useState } from "react";
import { ThemeContext } from "../context/ThemeContext.tsx";
import themesData from "../config/themes.json";
import { ChromePicker } from "react-color";
import { motion, AnimatePresence } from "framer-motion";

const ThemeSwitcher = () => {
    const { currentTheme, primaryColor, font, setTheme, setPrimaryColor, setFont, availableThemes } = useContext(ThemeContext);
    const [showColorPicker, setShowColorPicker] = useState(false);

    return (
        <motion.div
            className="p-6 flex flex-col gap-5 items-start bg-opacity-90 backdrop-blur-md shadow-lg rounded-lg border border-gray-300 dark:border-gray-600 transition-all"
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: "easeOut" }}
        >
            <motion.label className="flex flex-col w-full text-gray-800 dark:text-gray-200"
                          whileHover={{ scale: 1.05 }}
                          transition={{ duration: 0.2 }}
            >
                <span className="mb-1 text-lg font-semibold">Theme</span>
                <select
                    value={currentTheme}
                    onChange={(e) => setTheme(e.target.value)}
                    className="border p-2 rounded-md dark:bg-gray-800 dark:text-white transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                    {availableThemes.map((theme) => (
                        <option key={theme.id} value={theme.id}>
                            {theme.name}
                        </option>
                    ))}
                </select>
            </motion.label>

            <motion.label className="flex flex-col w-full text-gray-800 dark:text-gray-200"
                          whileHover={{ scale: 1.05 }}
                          transition={{ duration: 0.2 }}
            >
                <span className="mb-1 text-lg font-semibold">Primary Color</span>
                <motion.div className="relative">
                    <motion.button
                        className="border p-2 rounded-md shadow-md transition-all"
                        style={{ backgroundColor: primaryColor }}
                        onClick={() => setShowColorPicker(!showColorPicker)}
                        whileTap={{ scale: 0.95 }}
                        whileHover={{ scale: 1.1 }}
                    >
                        Pick Color
                    </motion.button>

                    <AnimatePresence>
                        {showColorPicker && (
                            <motion.div
                                className="absolute mt-2 z-10"
                                initial={{ opacity: 0, scale: 0.9 }}
                                animate={{ opacity: 1, scale: 1 }}
                                exit={{ opacity: 0, scale: 0.9 }}
                                transition={{ duration: 0.3 }}
                            >
                                <ChromePicker
                                    color={primaryColor}
                                    onChange={(color: { hex: string }) => setPrimaryColor(color.hex)}
                                />
                            </motion.div>
                        )}
                    </AnimatePresence>
                </motion.div>
            </motion.label>

            <motion.label className="flex flex-col w-full text-gray-800 dark:text-gray-200"
                          whileHover={{ scale: 1.05 }}
                          transition={{ duration: 0.2 }}
            >
                <span className="mb-1 text-lg font-semibold">Font</span>
                <select
                    value={font}
                    onChange={(e) => setFont(e.target.value)}
                    className="border p-2 rounded-md dark:bg-gray-800 dark:text-white transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                    {themesData.fonts.map((font) => (
                        <option key={font} value={font}>
                            {font.charAt(0).toUpperCase() + font.slice(1)}
                        </option>
                    ))}
                </select>
            </motion.label>
        </motion.div>
    );
};

export default ThemeSwitcher;
