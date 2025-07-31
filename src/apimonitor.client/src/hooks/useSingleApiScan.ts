import { useMutation } from 'react-query';
import axios from 'axios';

const useSingleApiScan = () => {
    const mutation = useMutation(
        (apiUrl: string) => axios.post(`http://localhost:5028/api/ApiScan/scan-single?apiUrl=${encodeURIComponent(apiUrl)}`),
        {
            onSuccess: (data) => {
                console.log('Scan successful', data);
            },
            onError: (error: any) => {
                console.error('Scan failed', error);
            }
        }
    );

    return {
        scanData: mutation.data,
        isSubmitting: mutation.isLoading,
        scanError: mutation.isError ? "Error scanning the API" : null,
        scanSingleApi: mutation.mutate
    };
};

export default useSingleApiScan;
