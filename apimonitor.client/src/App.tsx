import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import RegisterPage from "./pages/Register";
import Homepage from "./pages/Homepage.tsx";

const App = () => {
    return (
        <BrowserRouter>
                <Routes>
                    <Route path="/" element={<Navigate to="/register" />} />
                    <Route path="/register" element={<RegisterPage />} />
                    <Route path="/homepage" element={<Homepage />} />
                </Routes>
        </BrowserRouter>
    );
}

export default App;
