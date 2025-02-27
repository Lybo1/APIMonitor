import { useState } from "react";
import axios from "axios";
import { toast } from "react-toastify";

interface ApiScanResponse {
    message: string;
}

const useApiScan = () => {
    const [scanData, setScanData] = useState<ApiScanResponse | null>(null);
    const [scanError, setScanError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);

    const triggerScan = async () => {
        setIsLoading(true);
        setScanError(null);

        try {
            const response = await axios.post<ApiScanResponse>("http://localhost:5028/api/ApiScan/scan");
            setScanData(response.data);
            toast.success("API scan triggered successfully.");
        } catch (error: any) {
            setScanError(error.response?.data?.message || "Failed to trigger scan. Please try again later");
            toast.error(scanError || "An error occurred.");
        } finally {
            setIsLoading(false);
        }
    };

    const triggerSingleScan = async (apiUrl: string) => {
        setIsLoading(true);
        setScanError(null);

        try {
            const response = await axios.post<ApiScanResponse>("http://localhost:5028/api/ApiScan/scan-single", {
                params: {
                    apiUrl,
                }
            });

            setScanData(response.data);
            toast.success("API scan triggered successfully.");
        } catch (error: any) {
            setScanError(error.response?.data?.message || "An error occurred.");
            toast.error(scanError || "An error occurred.");
        }
    };

    return {
        scanData,
        isLoading,
        scanError,
        triggerScan,
        triggerSingleScan,
    };
};

export default useApiScan;