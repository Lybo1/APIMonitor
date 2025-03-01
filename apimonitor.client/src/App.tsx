import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import RegisterPage from "./pages/Register";
import Homepage from "./pages/Homepage.tsx";
import LoginPage from "./pages/Login.tsx";
import { AuthProvider } from "./context/AuthContext.tsx";
import ProtectedRoute from "./ProtectedRoute.tsx";
import { QueryClientProvider, QueryClient } from "react-query";
import { ReactQueryDevtools } from "react-query/devtools";

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
                        <Route element={<ProtectedRoute />}>
                            <Route path="/homepage" element={<Homepage />} />
                        </Route>
                    </Routes>
                </AuthProvider>
            </BrowserRouter>
            {<ReactQueryDevtools initialIsOpen={false} />}
        </QueryClientProvider>
    );
}

export default App;
