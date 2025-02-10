import { Route, Routes } from "react-router-dom";
import Register from "./pages/Register.tsx";
import { ThemeProvider } from "./context/ThemeContext.tsx";
import ThemedWrapper from "./components/ThemedWrapper.tsx";
import ThemeSwitcher from "./components/ThemeSwitcher.tsx";
import ThemeCustomizer from "./components/ThemeCustomizer.tsx";

const App = () => {
    return (
        <ThemeProvider>
                <ThemedWrapper>
                    <header className="p-4 border-b">
                        <ThemeSwitcher />
                    </header>
                    <main className="p-4">
                        <Routes>
                            <Route path="/" element={<Register />} />
                            <Route path="/customize" element={<ThemeCustomizer />} />
                        </Routes>
                    </main>
                </ThemedWrapper>
        </ThemeProvider>
    );
}

export default App
