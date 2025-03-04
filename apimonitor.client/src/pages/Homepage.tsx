import React, { useState, useEffect, useRef, ErrorBoundary } from 'react';
import { useAuth } from '../context/AuthContext.tsx';
import * as signalR from '@microsoft/signalr'; // Correct full import
import { useQuery } from '@tanstack/react-query'; // v5 import
import { motion } from 'framer-motion';
import { BellIcon, ShieldExclamationIcon, QuestionMarkCircleIcon, UserIcon } from '@heroicons/react/24/solid';
import { useNavigate } from 'react-router-dom';
import { ToastContainer, toast } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

interface MetricSummary {
    totalRequests: number;
    averageResponseTimeMs: number;
    totalErrors: number;
}

interface ThreatLog {
    id: number;
    timestamp: string;
    ipAddress: string;
    threatType: string;
}

interface Notification {
    id: number;
    message: string;
    isRead: boolean;
}

// Error Boundary Component
class ErrorBoundaryComponent extends React.Component<{ children: React.ReactNode }, { hasError: boolean, error: Error | null }> {
    constructor(props: { children: React.ReactNode }) {
        super(props);
        this.state = { hasError: false, error: null };
    }

    static getDerivedStateFromError(error: Error) {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
        console.error('Error caught by boundary:', error, errorInfo);
    }

    render() {
        if (this.state.hasError) {
            return (
                <div className="min-h-screen bg-gray-900 text-green-400 font-mono p-4">
                    <h2 className="text-xl">Oops! Something went wrong.</h2>
                    <p>{this.state.error?.message || 'An unexpected error occurred.'}</p>
                    <button
                        onClick={() => window.location.reload()}
                        className="mt-4 bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
                    >
                        Reload Page
                    </button>
                </div>
            );
        }
        return this.props.children;
    }
}

const fetchMetrics = async () => {
    const response = await fetch('http://localhost:5028/api/ApiMetrics/summary', {
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
    });
    if (!response.ok) throw new Error(`Failed to fetch metrics: ${response.status}`);
    return response.json();
};

const fetchThreats = async () => {
    const response = await fetch('http://localhost:5028/api/ThreatLogs', {
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
    });
    if (response.status === 404) return [];
    if (!response.ok) throw new Error(`Failed to fetch threats: ${response.status}`);
    return response.json();
};

const fetchNotifications = async () => {
    const response = await fetch('http://localhost:5028/api/Notification/user', {
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
    });
    if (!response.ok) throw new Error(`Failed to fetch notifications: ${response.status}`);
    return response.json();
};

const Homepage: React.FC = () => {
    const { user, token, logout, isAuthenticated } = useAuth(); // Include isAuthenticated explicitly
    const navigate = useNavigate();
    const [cliOutput, setCliOutput] = useState<string[]>([`[${new Date().toISOString()}] Welcome to API Monitor CLI`]);
    const [command, setCommand] = useState('');
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [isScanning, setIsScanning] = useState(false);
    const cliRef = useRef<HTMLDivElement>(null);
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const modalRef = useRef<HTMLDivElement>(null);

    const { data: metrics, isLoading: metricsLoading, error: metricsError, refetch: refetchMetrics } = useQuery({
        queryKey: ['metrics'],
        queryFn: fetchMetrics,
        enabled: !!token,
        retry: false,
    });

    const { data: threats, isLoading: threatsLoading, error: threatsError, refetch: refetchThreats } = useQuery({
        queryKey: ['threats'],
        queryFn: fetchThreats,
        enabled: !!token,
        retry: false,
    });

    const { data: notifs, isLoading: notifsLoading, error: notifsError, refetch: refetchNotifications } = useQuery({
        queryKey: ['notifications'],
        queryFn: fetchNotifications,
        enabled: !!token,
        retry: false,
    });

    // Define isAdmin for case-insensitive role check
    const isAdmin = isAuthenticated && user?.roles?.some((role) => role.toLowerCase() === "admin");

    useEffect(() => {
        console.log("Homepage render - User:", JSON.stringify(user), "Token:", token);
        if (user) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Logged in as ${user.username}`]);
        }
    }, [user]);

    useEffect(() => {
        if (!isAuthenticated || !token || !user) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Waiting for authentication...`]);
            navigate('/login');
            return;
        }

        let newConnection: signalR.HubConnection;
        try {
            newConnection = new signalR.HubConnectionBuilder()
                .withUrl('http://localhost:5028/notificationHub', {
                    withCredentials: true,
                    skipNegotiation: true, // Try skipping negotiation for WebSocket-only
                    transport: signalR.HttpTransportType.WebSockets // Force WebSockets
                })
                .configureLogging(signalR.LogLevel.Information)
                .withAutomaticReconnect([0, 2000, 10000, 30000, 60000]) // Extended retry delays
                .build();

            newConnection.on('ReceiveNotification', (message: string) => {
                setCliOutput(prev => [...prev.slice(-50), `[${new Date().toISOString()}] ${message}`]);
                if (message.includes('completed successfully')) setIsScanning(false);
                toast.info(message, { autoClose: 5000, position: 'top-right', style: { backgroundColor: '#2d3748', color: '#48bb78' } });
            });

            newConnection.on('Disconnected', (error) => {
                console.error(`[${new Date().toISOString()}] SignalR Disconnected: ${error?.message || 'Unknown error'}`);
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] SignalR Disconnected: ${error?.message || 'Unknown error'}`]);
                toast.error('SignalR connection lost. Attempting to reconnect...', { autoClose: 5000, position: 'top-right', style: { backgroundColor: '#2d3748', color: '#48bb78' } });
            });

            newConnection.start()
                .then(() => {
                    setConnection(newConnection);
                    setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Connected to real-time updates`]);
                })
                .catch((err: Error) => {
                    console.error(`[${new Date().toISOString()}] SignalR Error: ${err.message}`);
                    setCliOutput(prev => [...prev, `[${new Date().toISOString()}] SignalR Error: ${err.message}`]);
                    toast.error(`SignalR connection failed: ${err.message}`, { autoClose: 5000, position: 'top-right', style: { backgroundColor: '#2d3748', color: '#48bb78' } });
                });
        } catch (error) {
            console.error(`[${new Date().toISOString()}] SignalR Initialization Error: ${error.message}`);
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] SignalR Initialization Error: ${error.message}`]);
            toast.error(`SignalR initialization failed: ${error.message}`, { autoClose: 5000, position: 'top-right', style: { backgroundColor: '#2d3748', color: '#48bb78' } });
        }

        return () => {
            if (newConnection?.state === signalR.HubConnectionState.Connected) {
                newConnection.stop().catch(err => console.error(`[${new Date().toISOString()}] SignalR Stop Error: ${err.message}`));
            }
        };
    }, [isAuthenticated, token, user, navigate]);

    useEffect(() => {
        if (cliRef.current) cliRef.current.scrollTop = cliRef.current.scrollHeight;
    }, [cliOutput]);

    useEffect(() => {
        if (metrics && !metricsLoading) {
            setCliOutput(prev => [
                ...prev,
                `[${new Date().toISOString()}] Metrics - Requests: ${metrics.totalRequests}, Avg Time: ${metrics.averageResponseTimeMs?.toFixed(2) ?? 'N/A'}ms, Errors: ${metrics.totalErrors} (Note: Errors may include scan-related 400s from registration attempts)`
            ]);
        }
        if (metricsError) setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${(metricsError as Error).message}`]);
        if (threats && !threatsLoading && threats.length === 0) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] No recent threats`]);
        }
        if (threatsError) setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${(threatsError as Error).message}`]);
        if (notifsError) setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${(notifsError as Error).message}`]);
    }, [metrics, metricsLoading, metricsError, threats, threatsLoading, threatsError, notifs, notifsLoading, notifsError]);

    const commands: { [key: string]: (args: string[]) => Promise<void> | void } = {
        clear: () => setCliOutput([]),
        help: () => {
            const helpText = [
                `[${new Date().toISOString()}] Available Commands:`,
                '  clear - Clears the CLI output',
                '  help - Shows this help message',
                '  whoami - Displays current user details',
                '  logout - Logs out the current user',
                '  metrics - Fetches and displays API metrics',
                '  threats - Fetches and displays threat logs',
                '  scan - Initiates a full API scan',
                '  scan-single <url> [method] [key] - Scans a single API URL with optional HTTP method (GET, POST, PUT, DELETE) and API key. Use \'http://\' or \'https://\' prefix (e.g., \'http://localhost:5028/api/Register/register\').'
            ];
            setCliOutput(prev => [...prev, ...helpText]);
        },
        whoami: () => {
            if (!isAuthenticated || !user) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: Not logged in`]);
                return;
            }
            setCliOutput(prev => [
                ...prev,
                `[${new Date().toISOString()}] User: ${user.username} (ID: ${user.id}, Email: ${user.email}, Roles: ${user.roles.join(', ')})`
            ]);
        },
        logout: () => logout(),
        metrics: async () => {
            await refetchMetrics();
            if (metrics) {
                setCliOutput(prev => [
                    ...prev,
                    `[${new Date().toISOString()}] Metrics - Requests: ${metrics.totalRequests}, Avg Time: ${metrics.averageResponseTimeMs?.toFixed(2) ?? 'N/A'}ms, Errors: ${metrics.totalErrors} (Note: Errors may include scan-related 400s from registration attempts)`
                ]);
            } else if (metricsError) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error fetching metrics: ${(metricsError as Error).message}`]);
            }
        },
        threats: async () => {
            await refetchThreats();
            if (threats) {
                if (threats.length === 0) {
                    setCliOutput(prev => [...prev, `[${new Date().toISOString()}] No recent threats`]);
                } else {
                    const threatLines = threats.slice(0, 5).map((t: ThreatLog) =>
                        `[${new Date().toISOString()}] Threat: ${t.timestamp} - ${t.threatType} (${t.ipAddress})`
                    );
                    setCliOutput(prev => [...prev, ...threatLines]);
                }
            } else if (threatsError) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error fetching threats: ${(threatsError as Error).message}`]);
            }
        },
        scan: async () => {
            if (isScanning) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Scan already in progress`]);
                return;
            }
            setIsScanning(true);
            try {
                const response = await fetch('http://localhost:5028/api/ApiScan/scan', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'id': user!.id.toString(),
                    },
                    credentials: 'include',
                });
                if (!response.ok) throw new Error(await response.text() || 'Unknown error');
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Full scan initiated...`]);
            } catch (error) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Scan Error: ${(error as Error).message}`]);
                setIsScanning(false);
            }
        },
        'scan-single': async (args: string[]) => {
    if (isScanning) {
        setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Scan already in progress`]);
        return;
    }
    if (args.length === 0) {
        setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: URL required for scan-single`]);
        return;
    }
    setIsScanning(true);
    const apiUrl = args[0];
    const method = args.length > 1 ? args[1].toUpperCase() : "GET";
    if (method && !['GET', 'POST', 'PUT', 'DELETE'].includes(method)) {
        setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: Invalid method '${method}'. Use GET, POST, PUT, or DELETE.`]);
        setIsScanning(false);
        return;
    }

    let normalizedUrl = apiUrl.trim();
    if (!normalizedUrl.match(/^https?:\/\//i)) {
        normalizedUrl = `http://${normalizedUrl}`;
    }
    try {
        new URL(normalizedUrl);
    } catch (error) {
        setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: Invalid URL format. Use 'http://' or 'https://' prefix.`]);
        setIsScanning(false);
        return;
    }

    const apiKey = args.length > 2 ? args[2] : null;
    try {
        const url = new URL(`http://localhost:5028/api/ApiScan/scan-single`);
        url.searchParams.append("apiUrl", normalizedUrl);
        if (method) url.searchParams.append("method", method);
        if (apiKey) url.searchParams.append("apiKey", apiKey);

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`, // Ensure token is included
            },
            credentials: 'include',
        });

        if (!response.ok) {
            const errorText = await response.text() || 'Unknown error';
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Single Scan Error: ${errorText}`]);
            if (response.status === 400) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Hint: Check URL, method, or required parameters.`]);
            }
        } else {
            const result = await response.json();
            setCliOutput(prev => [
                ...prev,
                `[${new Date().toISOString()}] Scan result - URL: ${normalizedUrl}, Status: ${result.statusCode || 'N/A'}, Total: ${result.totalResponseTime?.toFixed(2) ?? 'N/A'}ms, Errors: ${result.errorsCount ?? 0}`
            ]);
            if (result.responseBody) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Response Body: ${result.responseBody}`]);
            }
        }
    } catch (error) {
        setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Single Scan Error: ${(error as Error).message}`]);
    } finally {
        setIsScanning(false);
    }
    },
    };

    const handleCommand = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!command.trim()) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: Enter a command`]);
            return;
        }

        setCliOutput(prev => [...prev, `> ${command}`]);
        const parts = command.trim().toLowerCase().split(' ');
        const cmd = parts[0];
        const args = parts.slice(1);

        if (!isAuthenticated && cmd !== 'help') {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: Not logged in`]);
            return;
        }

        if (commands[cmd]) {
            await commands[cmd](args);
        } else {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Command not recognized`]);
        }
        setCommand('');
    };

    const handleBellMouseEnter = () => {
        if (isAuthenticated) setIsModalOpen(true);
    };

    const handleBellMouseLeave = () => {
        setIsModalOpen(false);
    };

    const handleModalMouseEnter = () => {
        setIsModalOpen(true);
    };

    const handleModalMouseLeave = () => {
        setIsModalOpen(false);
    };

    const handleViewAll = () => {
        if (isAuthenticated) navigate('/notifications');
    };

    const markAsRead = async (id: number) => {
        try {
            await fetch('http://localhost:5028/api/Notification/mark-as-read', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id }),
                credentials: 'include',
            });
            refetchNotifications();
        } catch (error) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error marking notification: ${(error as Error).message}`]);
        }
    };

    if (metricsLoading || threatsLoading || notifsLoading) return <div className="min-h-screen bg-gray-900 text-green-400 font-mono p-4">Loading...</div>;

    return (
        <ErrorBoundaryComponent>
            <div className="min-h-screen bg-gray-900 text-green-400 font-mono">
                <nav className="bg-gray-800 p-4 flex justify-between items-center shadow fixed w-full top-0 z-10">
                    <h1 className="text-xl font-bold">API Monitor CLI</h1>
                    <div className="flex items-center space-x-4">
                        {isAuthenticated && (
                            <>
                                <div className="relative group">
                                    <BellIcon
                                        className="w-6 h-6 cursor-pointer"
                                        onMouseEnter={handleBellMouseEnter}
                                        onMouseLeave={handleBellMouseLeave}
                                        style={{ color: "#48bb78" }} // Match green theme
                                    />
                                    {notifications?.filter(n => !n.isRead).length > 0 && (
                                        <span className="absolute -top-1 -right-1 bg-red-500 text-white rounded-full w-4 h-4 text-xs flex items-center justify-center">
                                            {notifications.filter(n => !n.isRead).length}
                                        </span>
                                    )}
                                    {isModalOpen && (
                                        <motion.div
                                            ref={modalRef}
                                            className="absolute right-0 mt-2 w-64 bg-gray-700 rounded shadow-lg"
                                            initial={{ opacity: 0, y: -10 }}
                                            animate={{ opacity: 1, y: 0 }}
                                            exit={{ opacity: 0, y: -10 }}
                                            transition={{ duration: 0.2 }}
                                            onMouseEnter={handleModalMouseEnter}
                                            onMouseLeave={handleModalMouseLeave}
                                            style={{
                                                backgroundColor: "#2d3748", // Darker gray for modal
                                                color: "#48bb78", // Green text
                                                fontFamily: "'Courier New', Courier, monospace",
                                            }}
                                        >
                                            {notifications?.length > 0 ? (
                                                notifications.map(n => (
                                                    <div key={n.id} className={`p-2 ${n.isRead ? 'opacity-50' : ''}`}>
                                                        {n.message}
                                                        {!n.isRead && (
                                                            <button onClick={() => markAsRead(n.id)} className="ml-2 text-blue-400">Mark Read</button>
                                                        )}
                                                    </div>
                                                ))
                                            ) : (
                                                <div className="p-2">No notifications</div>
                                            )}
                                        </motion.div>
                                    )}
                                </div>
                                <div className="relative group">
                                    <ShieldExclamationIcon
                                        className="w-6 h-6 cursor-pointer"
                                        style={{ color: "#48bb78" }} // Match green theme
                                    />
                                    {threats?.length > 0 && (
                                        <span className="absolute -top-1 -right-1 bg-yellow-500 text-black rounded-full w-4 h-4 text-xs flex items-center justify-center">
                                            {threats.length}
                                        </span>
                                    )}
                                    <motion.div
                                        className="absolute right-0 mt-2 w-64 bg-gray-700 rounded shadow-lg hidden group-hover:block"
                                        initial={{ opacity: 0 }}
                                        animate={{ opacity: 1 }}
                                        style={{
                                            backgroundColor: "#2d3748", // Darker gray for tooltip
                                            color: "#48bb78", // Green text
                                            fontFamily: "'Courier New', Courier, monospace",
                                            fontSize: "12px",
                                        }}
                                    >
                                        {threats?.length > 0 ? (
                                            threats.slice(0, 5).map((t: ThreatLog) => (
                                                <div key={t.id} className="p-2">
                                                    {t.timestamp} - {t.threatType} ({t.ipAddress})
                                                </div>
                                            ))
                                        ) : (
                                            <div className="p-2">No recent threats</div>
                                        )}
                                    </motion.div>
                                </div>
                                <span className="text-green-400">Welcome, {user?.username || 'Guest'}</span>
                                <UserIcon
                                    className="w-6 h-6 cursor-pointer"
                                    style={{ color: "#48bb78" }} // Match green theme
                                    onClick={() => navigate('/account')}
                                />
                                <button
                                    onClick={logout}
                                    className="bg-red-500 text-white px-4 py-1 rounded hover:bg-red-600 transition-colors"
                                >
                                    Logout
                                </button>
                                {isAdmin && (
                                    <button
                                        onClick={() => navigate("/admin")}
                                        style={{
                                            backgroundColor: "#4a5568", // Gray-500
                                            color: "#fff",
                                            padding: "8px 16px",
                                            borderRadius: "4px",
                                            border: "none",
                                            cursor: "pointer",
                                            transition: "background-color 0.2s",
                                        }}
                                        onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = "#2d3744")} // Gray-600
                                        onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = "#4a5568")}
                                    >
                                        Admin Panel
                                    </button>
                                )}
                            </>
                        )}
                    </div>
                </nav>

                <div className="flex-1 p-4 pt-20 h-screen flex flex-col relative">
                    <div ref={cliRef} className="flex-1 bg-black p-4 rounded overflow-y-auto border-4 border-green-500">
                        {cliOutput.map((line, index) => (
                            <motion.div key={index} initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="mb-1 font-mono text-green-400">
                                {line}
                            </motion.div>
                        ))}
                    </div>
                    <form onSubmit={handleCommand} className="mt-2 flex items-center relative">
                        <span className="mr-2"></span>
                        <input
                            type="text"
                            value={command}
                            onChange={e => setCommand(e.target.value)}
                            className="flex-1 bg-gray-800 text-green-400 p-2 rounded-l outline-none font-mono"
                            placeholder="Type 'help' for commands..."
                            disabled={isScanning}
                        />
                        <button
                            type="submit"
                            className="bg-blue-500 text-white px-4 py-2 rounded-r hover:bg-blue-600 disabled:bg-gray-500 ml-2 font-mono"
                            disabled={isScanning}
                        >
                            Submit
                        </button>
                        <div className="relative group ml-4">
                            <QuestionMarkCircleIcon className="w-6 h-6 cursor-pointer" />
                            <motion.div
                                className="absolute -left-48 -top-32 w-64 bg-gray-700 rounded shadow-lg hidden group-hover:block z-20"
                                initial={{ opacity: 0 }}
                                animate={{ opacity: 1 }}
                            >
                                <p className="text-sm p-2 font-mono text-green-400">Commands:</p>
                                <ul className="text-sm list-disc pl-6 pb-2 font-mono text-green-400">
                                    <li><code>help</code>: List all commands</li>
                                    <li><code>whoami</code>: Show user info</li>
                                    <li><code>logout</code>: Log out</li>
                                    <li><code>metrics</code>: Show API metrics</li>
                                    <li><code>threats</code>: Show threat logs</li>
                                    <li><code>scan</code>: Full API scan</li>
                                    <li><code>scan-single [method] [key]</code>: Scan one API</li>
                                    <li><code>clear</code>: Clear CLI</li>
                                </ul>
                            </motion.div>
                        </div>
                    </form>
                </div>
            </div>
        </ErrorBoundaryComponent>
    );
};

export default Homepage;