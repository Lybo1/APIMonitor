const ScanResult: React.FC<{ data: any }> = ({ data }) => {
    if (!data) return null;

    return (
        <div className="mt-6 p-6 bg-white text-black rounded-lg shadow-lg">
            <h3 className="text-xl font-bold mb-4">Scan Results:</h3>
            <div>
                <p><strong>Total Requests:</strong> {data.totalRequests}</p>
                <p><strong>Requests per Minute:</strong> {data.requestsPerMinute}</p>
                <p><strong>Average Response Time:</strong> {data.averageResponseTime} ms</p>
                <p><strong>Errors Count:</strong> {data.errorsCount}</p>
            </div>
        </div>
    );
};

export default ScanResult;
