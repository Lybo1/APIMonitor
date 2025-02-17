import { Navigate, Route, Routes} from "react-router-dom";
import Register from "./pages/Register.tsx";
import './index.css';

const App = () => {
    return (
       <Routes>
           <Route path='/' element={<Navigate to='/register' replace />} />
           <Route path="/register" element={<Register />} />
       </Routes>
    );
}

export default App
