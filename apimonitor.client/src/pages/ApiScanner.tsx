// import { useState } from "react";
// import axios from "axios";
//
const ApiScanner = () => {
//     const [apiUrl, setApiUrl] = useState("");
//     const [result, setResult] = useState<any>(null);
//     const [loading, setLoading] = useState(false);
//     const [error, setError] = useState<string | null>(null);
//
//     const handleScan = async () => {
//         setLoading(true);
//         setError(null);
//         setResult(null);
//
//         try {
//             const response = await axios.post(
//                 `http://localhost:5028/api/ApiScan/scan-single?apiUrl=${encodeURIComponent(apiUrl)}`,
//                 {}, // Empty body for POST request
//                 {
//                     headers: {
//                         Authorization: `Bearer ${localStorage.getItem("token")}`, // Include token if needed
//                     },
//                 }
//             );
//
//             setResult(response.data);
//         } catch (err: any) {
//             setError(err.response?.data?.message || "Failed to scan API.");
//         } finally {
//             setLoading(false);
//         }
//     };
//
//     return (
//         <div className="max-w-2xl mx-auto p-6 bg-gray-900 text-white rounded-lg shadow-md">
//             <h2 className="text-xl font-semibold mb-4">🔍 API Scanner</h2>
//
//             <input
//                 type="text"
//                 value={apiUrl}
//                 onChange={(e) => setApiUrl(e.target.value)}
//                 placeholder="Enter API URL"
//                 className="w-full p-2 mb-4 bg-gray-800 border border-gray-600 rounded"
//             />
//
//             <button
//                 onClick={handleScan}
//                 disabled={loading || !apiUrl}
//                 className={`w-full p-2 rounded ${loading ? "bg-gray-600" : "bg-blue-500 hover:bg-blue-600"}`}
//             >
//                 {loading ? "Scanning..." : "Scan API"}
//             </button>
//
//             {error && <p className="mt-4 text-red-400">❌ {error}</p>}
//
//             {result && (
//                 <div className="mt-4 p-4 bg-gray-800 rounded">
//                     <h3 className="text-lg font-semibold">📊 Scan Results</h3>
//                     <pre className="mt-2 text-sm">{JSON.stringify(result, null, 2)}</pre>
//                 </div>
//             )}
//         </div>
//     );
 };
//
export default ApiScanner;
