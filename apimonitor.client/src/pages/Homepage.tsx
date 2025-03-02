import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext.tsx';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useQuery } from '@tanstack/react-query'; // v5 import
import { motion } from 'framer-motion';
import { BellIcon, ShieldExclamationIcon, QuestionMarkCircleIcon, UserIcon } from '@heroicons/react/24/solid';
import { useNavigate } from 'react-router-dom';

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
    const { user, token, logout } = useAuth();
    const navigate = useNavigate();
    const [cliOutput, setCliOutput] = useState<string[]>([`[${new Date().toISOString()}] Welcome to API Monitor CLI`]);
    const [command, setCommand] = useState('');
    const [connection, setConnection] = useState<any>(null);
    const [isScanning, setIsScanning] = useState(false);
    const cliRef = useRef<HTMLDivElement>(null);

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

    const { data: notifications, isLoading: notifsLoading, error: notifsError, refetch: refetchNotifications } = useQuery({
        queryKey: ['notifications'],
        queryFn: fetchNotifications,
        enabled: !!token,
        retry: false,
    });

    useEffect(() => {
        console.log("Homepage render - User:", JSON.stringify(user), "Token:", token);
        if (user) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Logged in as ${user.username}`]);
        }
    }, [user]);

    useEffect(() => {
        if (!token || !user) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Waiting for authentication...`]);
            navigate('/login');
            return;
        }

        const newConnection = new HubConnectionBuilder()
            .withUrl('http://localhost:5028/notificationHub', {
                withCredentials: true,
            })
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        newConnection.on('ReceiveNotification', (message: string) => {
            setCliOutput(prev => [...prev.slice(-50), `[${new Date().toISOString()}] ${message}`]);
            if (message.includes('completed successfully')) setIsScanning(false);
        });

        newConnection.start()
            .then(() => {
                setConnection(newConnection);
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Connected to real-time updates`]);
            })
            .catch((err: Error) => setCliOutput(prev => [...prev, `[${new Date().toISOString()}] SignalR Error: ${err.message}`]));

        return () => newConnection.stop();
    }, [token, user, navigate]);

    useEffect(() => {
        if (cliRef.current) cliRef.current.scrollTop = cliRef.current.scrollHeight;
    }, [cliOutput]);

    useEffect(() => {
        if (metrics && !metricsLoading) {
            setCliOutput(prev => [
                ...prev,
                `[${new Date().toISOString()}] Metrics - Requests: ${metrics.totalRequests}, Avg Time: ${metrics.averageResponseTimeMs?.toFixed(2) ?? 'N/A'}ms, Errors: ${metrics.totalErrors}`
            ]);
        }
        if (metricsError) setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${(metricsError as Error).message}`]);
        if (threats && !threatsLoading && threats.length === 0) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] No recent threats`]);
        }
        if (threatsError) setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${(threatsError as Error).message}`]);
        if (notifsError) setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${(notifsError as Error).message}`]);
    }, [metrics, metricsLoading, metricsError, threats, threatsLoading, threatsError, notifsError]);

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
                '  scan-single <url> [key] - Scans a single API URL with optional API key',
            ];
            setCliOutput(prev => [...prev, ...helpText]);
        },
        whoami: () => {
            if (!user) {
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
                    `[${new Date().toISOString()}] Metrics - Requests: ${metrics.totalRequests}, Avg Time: ${metrics.averageResponseTimeMs?.toFixed(2) ?? 'N/A'}ms, Errors: ${metrics.totalErrors}`
                ]);
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
            const apiKey = args.length > 1 ? args[1] : null;
            try {
                const url = new URL(`http://localhost:5028/api/ApiScan/scan-single?apiUrl=${encodeURIComponent(apiUrl)}${apiKey ? `&apiKey=${encodeURIComponent(apiKey)}` : ''}`);
                const response = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                });
                if (!response.ok) throw new Error(await response.text() || 'Unknown error');
                const result = await response.json();
                setCliOutput(prev => [
                    ...prev,
                    `[${new Date().toISOString()}] Scan result - URL: ${apiUrl}, Status: ${result.statusCode}, Headers: ${result.headersResponseTime.toFixed(2)}ms, Total: ${result.totalResponseTime.toFixed(2)}ms, Errors: ${result.errorsCount}`,
                    `[${new Date().toISOString()}] Response Body:`,
                    result.responseBody
                ]);
                setIsScanning(false);
            } catch (error) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Single Scan Error: ${(error as Error).message}`]);
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

        if (!token && cmd !== 'help') {
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
        <div className="min-h-screen bg-gray-900 text-green-400 font-mono">
            <nav className="bg-gray-800 p-4 flex justify-between items-center shadow fixed w-full top-0 z-10">
                <h1 className="text-xl font-bold">API Monitor CLI</h1>
                <div className="flex items-center space-x-4">
                    <div className="relative group">
                        <BellIcon className="w-6 h-6 cursor-pointer" />
                        {notifications?.filter(n => !n.isRead).length > 0 && (
                            <span className="absolute -top-1 -right-1 bg-red-500 text-white rounded-full w-4 h-4 text-xs flex items-center justify-center">
                                {notifications.filter(n => !n.isRead).length}
                            </span>
                        )}
                        <motion.div
                            className="absolute right-0 mt-2 w-64 bg-gray-700 rounded shadow-lg hidden group-hover:block"
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
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
                    </div>
                    <div className="relative group">
                        <ShieldExclamationIcon className="w-6 h-6 cursor-pointer" />
                        {threats?.length > 0 && (
                            <span className="absolute -top-1 -right-1 bg-yellow-500 text-black rounded-full w-4 h-4 text-xs flex items-center justify-center">
                                {threats.length}
                            </span>
                        )}
                        <motion.div
                            className="absolute right-0 mt-2 w-64 bg-gray-700 rounded shadow-lg hidden group-hover:block"
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
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
                    <span>Welcome, {user?.username || 'Guest'}</span>
                    <UserIcon
                        className="w-6 h-6 cursor-pointer"
                        onClick={() => navigate('/account')}
                    />
                    <button onClick={logout} className="bg-red-500 text-white px-4 py-1 rounded hover:bg-red-600">Logout</button>
                </div>
            </nav>

            <div className="flex-1 p-4 pt-20 h-screen flex flex-col relative">
                <div ref={cliRef} className="flex-1 bg-black p-4 rounded overflow-y-auto">
                    {cliOutput.map((line, index) => (
                        <motion.div key={index} initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="mb-1">
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
                        className="flex-1 bg-gray-800 text-green-400 p-2 rounded-l outline-none"
                        placeholder="Type 'help' for commands..."
                        disabled={isScanning}
                    />
                    <button
                        type="submit"
                        className="bg-blue-500 text-white px-4 py-2 rounded-r hover:bg-blue-600 disabled:bg-gray-500 ml-2"
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
                            <p className="text-sm p-2">Commands:</p>
                            <ul className="text-sm list-disc pl-6 pb-2">
                                <li><code>help</code>: List all commands</li>
                                <li><code>whoami</code>: Show user info</li>
                                <li><code>logout</code>: Log out</li>
                                <li><code>metrics</code>: Show API metrics</li>
                                <li><code>threats</code>: Show threat logs</li>
                                <li><code>scan</code>: Full API scan</li>
                                <li><code>scan-single [key]</code>: Scan one API</li>
                                <li><code>clear</code>: Clear CLI</li>
                            </ul>
                        </motion.div>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default Homepage;