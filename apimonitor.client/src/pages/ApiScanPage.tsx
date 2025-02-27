import ScanForm from "../components/ScanForm.tsx";
import ScanResult from "../components/ScanResult.tsx";
import LoadingSpinner from "../components/LoadingSpinner.tsx";
import useApiScan from "../hooks/useApiScan.ts";
import { motion } from "framer-motion";

const ApiScanPage: React.FC = () => {
    const { scanData, isLoading, scanError, triggerScan, triggerSingleScan } = useApiScan();

    return (
        <div className="flex flex-col items-center justify-center min-h-screen text-white bg-gradient-to-r from-blue-600 to-indigo-900">
            <motion.h1
                className="text-6xl font-bold mb-10"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 1 }}
            >
                API Scanner
            </motion.h1>

            <div className="flex justify-center items-center w-full">
                <ScanForm triggerSingleScan={triggerSingleScan} />
            </div>

            <div className="mt-6 w-full">
                <button
                    onClick={triggerScan}
                    className="py-3 px-6 bg-green-500 text-white rounded-full font-semibold hover:bg-green-400 transition-all duration-300"
                    disabled={isLoading}
                >
                    {isLoading ? "Scanning..." : "Start Full Scan"}
                </button>
            </div>

            {isLoading && <LoadingSpinner />}
            {scanData && <ScanResult data={scanData} />}
            {scanError && (
                <motion.div
                    className="fixed inset-0 flex justify-center items-center bg-black/50 z-50"
                    initial={{ scale: 0.8 }}
                    animate={{ scale: 1 }}
                >
                    <div className="bg-red-600 text-white p-6 rounded-lg shadow-lg text-center">
                        <p>{scanError}</p>
                    </div>
                </motion.div>
            )}
        </div>
    );
};

export default ApiScanPage;
