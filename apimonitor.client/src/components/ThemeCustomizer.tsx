import { useContext, useState } from "react";
import { ChromePicker } from "react-color";
import { motion } from "framer-motion";
import { ThemeContext } from "../context/ThemeContext";

const ThemeCustomizer = () => {
    const { primaryColor, font, setPrimaryColor, setFont } = useContext(ThemeContext);
    const [showPicker, setShowPicker] = useState(false);

    return (
        <div className="p-6 space-y-4 bg-white dark:bg-gray-800 rounded-lg shadow-lg">
            <h2 className="text-2xl font-bold mb-4 text-gray-900 dark:text-gray-100">
                Customize Your Theme
            </h2>

            <div>
                <label className="block mb-2 text-gray-700 dark:text-gray-300">Primary Color:</label>
                <button
                    onClick={() => setShowPicker(!showPicker)}
                    className="w-12 h-12 rounded-full border"
                    style={{ backgroundColor: primaryColor }}
                    title="Click to change color"
                />
                {showPicker && (
                    <div className="mt-2">
                        <ChromePicker
                            color={primaryColor}
                            onChangeComplete={(color: { hex: string }) => setPrimaryColor(color.hex)}
                        />
                    </div>
                )}
            </div>

            <div>
                <label className="block mb-2 text-gray-700 dark:text-gray-300">Font:</label>
                <select
                    value={font}
                    onChange={(e) => setFont(e.target.value)}
                    className="border p-2 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                    <option value="sans">Sans</option>
                    <option value="serif">Serif</option>
                    <option value="mono">Monospace</option>
                    <option value="poppins">Poppins</option>
                    <option value="roboto">Roboto</option>
                </select>
            </div>

            <motion.div
                className="mt-6 p-4 border rounded-lg"
                style={{
                    backgroundColor: primaryColor,
                    fontFamily: font,
                }}
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 0.5 }}
            >
                <p className="text-white">
                    This is a preview of your custom theme. The background color and font update in real time.
                </p>
            </motion.div>
        </div>
    );
};

export default ThemeCustomizer;

