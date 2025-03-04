import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowLeftCircle } from 'lucide-react';
import { ExclamationTriangleIcon } from '@heroicons/react/24/outline';
import Particles from 'react-tsparticles';
import { loadFull } from 'tsparticles';
import { useEffect } from 'react';

const UnauthorizedPage: React.FC = () => {
    const navigate = useNavigate();

    useEffect(() => {
        loadFull();
    }, []);

    return (
        <div className="min-h-screen bg-gray-900 text-green-400 flex flex-col justify-center items-center relative">

            <motion.div
                initial={{ y: -50, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ duration: 0.5 }}
                className="z-10 p-8 bg-gray-800 border-4 border-green-400 rounded-lg shadow-2xl text-center"
            >
                <ExclamationTriangleIcon className="w-16 h-16 mx-auto text-yellow-400" />
                <h1 className="text-3xl font-bold mt-4">Unauthorized Access</h1>
                <p className="mt-2 text-lg">You don't have permission to access this page. Please contact an administrator or go back to the homepage.</p>

                <motion.button
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                    className="mt-6 px-6 py-3 bg-green-500 text-white rounded-lg shadow-lg flex items-center justify-center gap-2"
                    onClick={() => navigate('/homepage')}
                >
                    <ArrowLeftCircle className="w-5 h-5" />
                    Back to Homepage
                </motion.button>
            </motion.div>
        </div>
    );
};

export default UnauthorizedPage;