import { createContext, ReactNode, useEffect, useState } from "react";
import themesData from "../config/themes.json";

type Theme = {
    id: string;
    name: string;
    background: string;
    text: string;
    primary: string;
};

type ThemeContextType = {
    currentTheme: string;
    availableThemes: Theme[];
    primaryColor: string;
    font: string;
    setTheme: (theme: string) => void;
    setPrimaryColor: (color: string) => void;
    setFont: (font: string) => void;
};

// Create Context
export const ThemeContext = createContext<ThemeContextType>({
    currentTheme: "light",
    availableThemes: themesData.themes,
    primaryColor: themesData.colors[0],
    font: themesData.fonts[0],
    setTheme: () => {},
    setPrimaryColor: () => {},
    setFont: () => {},
});

// Provider Component
export const ThemeProvider = ({ children }: { children: ReactNode }) => {
    const [currentTheme, setCurrentTheme] = useState("light");
    const [primaryColor, setPrimaryColor] = useState(themesData.colors[0]);
    const [font, setFont] = useState(themesData.fonts[0]);

    useEffect(() => {
        const savedTheme = localStorage.getItem("theme") || "light";
        const savedColor = localStorage.getItem("primaryColor") || themesData.colors[0];
        const savedFont = localStorage.getItem("font") || themesData.fonts[0];

        setCurrentTheme(savedTheme);
        setPrimaryColor(savedColor);
        setFont(savedFont);
    }, []);

    const changeTheme = (theme: string) => {
        setCurrentTheme(theme);
        localStorage.setItem("theme", theme);
    };

    const changeColor = (color: string) => {
        setPrimaryColor(color);
        localStorage.setItem("primaryColor", color);
    };

    const changeFont = (font: string) => {
        setFont(font);
        localStorage.setItem("font", font);
    };

    const selectedTheme = themesData.themes.find(t => t.id === currentTheme) || themesData.themes[0];

    return (
        <ThemeContext.Provider value={{ currentTheme, availableThemes: themesData.themes, primaryColor, font, setTheme: changeTheme, setPrimaryColor: changeColor, setFont: changeFont }}>
            <div className={`${selectedTheme.background} ${selectedTheme.text} font-${font}`} style={{ "--primary-color": primaryColor } as React.CSSProperties}>
                {children}
            </div>
        </ThemeContext.Provider>
    );
};
