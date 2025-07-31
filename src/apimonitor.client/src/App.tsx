import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import RegisterPage from "./pages/Register";
import Homepage from "./pages/Homepage.tsx";
import LoginPage from "./pages/Login.tsx";
import { AuthProvider } from "./context/AuthContext.tsx";
import ProtectedRoute from "./ProtectedRoute.tsx";
import { QueryClientProvider, QueryClient } from "@tanstack/react-query";
import UserAccount from "./pages/UserPage";
import AdminDashboard from "./pages/AdminDashboard.tsx";
import UnauthorizedPage from "./pages/UnauthorizedPage";

const queryClient = new QueryClient();

const App = () => {
    return (
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <AuthProvider>
                    <Routes>
                        <Route path="/" element={<Navigate to="/register" />} />
                        <Route path="/register" element={<RegisterPage />} />
                        <Route path="/login" element={<LoginPage />} />
                        <Route path="/error" element={<UnauthorizedPage />} />

                        <Route element={<ProtectedRoute />}>
                            <Route path="/homepage" element={<Homepage />} />
                        </Route>

                        <Route element={<ProtectedRoute />}>
                            <Route path="/account" element={<UserAccount />} />
                        </Route>

                        <Route element={<ProtectedRoute />}>
                            <Route path="/admin" element={<AdminDashboard />} />
                        </Route>
                    </Routes>
                </AuthProvider>
            </BrowserRouter>
        </QueryClientProvider>
    );
}

export default App;
