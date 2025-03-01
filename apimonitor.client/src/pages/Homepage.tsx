import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext.tsx';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useQuery } from 'react-query';
import { motion } from 'framer-motion';
import { BellIcon, ShieldExclamationIcon, QuestionMarkCircleIcon } from '@heroicons/react/24/solid';

interface MetricSummary {
    totalRequests: number;
    averageResponseTime: number;
    errorsCount: number;
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
    const [cliOutput, setCliOutput] = useState<string[]>([`[${new Date().toISOString()}] Welcome to API Monitor CLI`]);
    const [command, setCommand] = useState('');
    const [connection, setConnection] = useState<any>(null);
    const [isScanning, setIsScanning] = useState(false);
    const cliRef = useRef<HTMLDivElement>(null);

    const { data: metrics, isLoading: metricsLoading, error: metricsError } = useQuery(
        'metrics',
        fetchMetrics,
        { enabled: !!token, retry: false }
    );
    const { data: threats, isLoading: threatsLoading, error: threatsError } = useQuery(
        'threats',
        fetchThreats,
        { enabled: !!token, retry: false }
    );
    const { data: notifications, isLoading: notifsLoading, error: notifsError, refetch: refetchNotifications } = useQuery(
        'notifications',
        fetchNotifications,
        { enabled: !!token, retry: false }
    );

    useEffect(() => {
        console.log("Homepage render - User:", JSON.stringify(user), "Token:", token);
        if (user) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Logged in as ${user.username}`]);
        }
    }, [user]);

    useEffect(() => {
        if (!token || !user) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Waiting for authentication...`]);
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
            .catch(err => setCliOutput(prev => [...prev, `[${new Date().toISOString()}] SignalR Error: ${err}`]));

        return () => {
            newConnection.stop();
        };
    }, [token, user]);

    useEffect(() => {
        if (cliRef.current) cliRef.current.scrollTop = cliRef.current.scrollHeight;
    }, [cliOutput]);

    useEffect(() => {
        if (metrics && !metricsLoading) {
            setCliOutput(prev => [
                ...prev,
                `[${new Date().toISOString()}] Metrics - Requests: ${metrics.totalRequests}, Avg Time: ${metrics.averageResponseTime.toFixed(2)}ms, Errors: ${metrics.errorsCount}`
            ]);
        }
        if (metricsError) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${metricsError.message}`]);
        }
        if (threatsError) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${threatsError.message}`]);
        }
        if (notifsError) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: ${notifsError.message}`]);
        }
    }, [metrics, metricsLoading, metricsError, threatsError, notifsError]);

    const handleCommand = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!command.trim()) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: Enter a command`]);
            return;
        }

        setCliOutput(prev => [...prev, `> ${command}`]);
        const [cmd, ...args] = command.trim().split(' ');

        if (!token) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error: No access token available`]);
            return;
        }

        if (cmd.toLowerCase() === 'scan' && !isScanning) {
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
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Scan Error: ${error.message}`]);
                setIsScanning(false);
            }
        } else if (cmd.toLowerCase() === 'scan-single' && args.length > 0 && !isScanning) {
            setIsScanning(true);
            const apiUrl = args.join(' ');
            try {
                const response = await fetch(`http://localhost:5028/api/ApiScan/scan-single?apiUrl=${encodeURIComponent(apiUrl)}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    credentials: 'include',
                });
                if (!response.ok) throw new Error(await response.text() || 'Unknown error');
                const result = await response.json();
                setCliOutput(prev => [
                    ...prev,
                    `[${new Date().toISOString()}] Scanned ${apiUrl} - Requests: ${result.totalRequests}, Time: ${result.averageResponseTime.toFixed(2)}ms, Errors: ${result.errorsCount}`
                ]);
                setIsScanning(false);
            } catch (error) {
                setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Single Scan Error: ${error.message}`]);
                setIsScanning(false);
            }
        } else if (isScanning) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Scan already in progress`]);
        } else {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Command not recognized`]);
        }
        setCommand('');
    };

    const markAsRead = async (id: number) => {
        try {
            await fetch('http://localhost:5028/api/Notification/mark-as-read', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ id }),
                credentials: 'include',
            });
            refetchNotifications();
        } catch (error) {
            setCliOutput(prev => [...prev, `[${new Date().toISOString()}] Error marking notification: ${error.message}`]);
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
                                threats.slice(0, 5).map(t => (
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
                        placeholder="Type 'scan' or 'scan-single <url>'..."
                        disabled={isScanning}
                    />
                    <button
                        type="submit"
                        className="bg-blue-500 text-white px-4 py-2 rounded-r hover:bg-blue-600 disabled:bg-gray-500"
                        disabled={isScanning}
                    >
                        Submit
                    </button>
                    <div className="relative group ml-2">
                        <QuestionMarkCircleIcon className="w-6 h-6 cursor-pointer" />
                        <motion.div
                            className="absolute -right-2 -top-24 w-64 bg-gray-700 rounded shadow-lg hidden group-hover:block z-20"
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                        >
                            <p className="text-sm p-2">Commands:</p>
                            <ul className="text-sm list-disc pl-6 pb-2">
                                <li><code>scan</code>: Scans all active APIs</li>
                                <li><code>scan-single &lt;url&gt</code>: Scans one API (e.g., "scan-single https://api.example.com")</li>
                            </ul>
                            <p className="text-sm p-2">Type the full command and press Enter or click Submit.</p>
                        </motion.div>
                    </div>
                </form>
            </div>
        </div>
);
};

export default Homepage;