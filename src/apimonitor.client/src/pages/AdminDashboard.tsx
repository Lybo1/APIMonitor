// src/pages/AdminDashboard.tsx
import React, { useState, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import { useNavigate, Navigate } from "react-router-dom";
import { useQuery, useMutation } from "@tanstack/react-query";


const AdminDashboard: React.FC = () => {
    const { user, token, logout } = useAuth();
    const navigate = useNavigate();

    // Ensure user and token exist before proceeding
    if (!token || !user) {
        return <Navigate to="/login" replace />;
    }

    // Case-insensitive role check for "admin"
    const hasAdminRole = user.roles?.some((role) => role.toLowerCase() === "admin");
    if (!hasAdminRole) {
        return <Navigate to="/error" replace />;
    }

    const [ipToBlock, setIpToBlock] = useState("");
    const [durationHours, setDurationHours] = useState(1);
    const [blockReason, setBlockReason] = useState("");
    const [selectedAuditLogId, setSelectedAuditLogId] = useState<number | null>(null);
    const [selectedBannedIp, setSelectedBannedIp] = useState<string | null>(null);

    const { data: auditLogs, refetch: refetchAuditLogs } = useQuery<AuditLog[], Error>({
        queryKey: ["adminAuditLogs"],
        queryFn: () => fetchAuditLogs(token),
        enabled: !!token,
        retry: false,
    });

    const { data: bannedIps, refetch: refetchBannedIps } = useQuery<IpBlock[], Error>({
        queryKey: ["bannedIps"],
        queryFn: () => fetchBannedIps(token),
        enabled: !!token,
        retry: false,
    });

    const { data: ipBlocks, refetch: refetchIpBlocks } = useQuery<IpBlock[], Error>({
        queryKey: ["ipBlocks"],
        queryFn: () => fetchIpBlocks(token),
        enabled: !!token,
        retry: false,
    });

    const deleteAuditLogMutation = useMutation({
        mutationFn: (id: number) => deleteAuditLog(id, token),
        onSuccess: () => refetchAuditLogs(),
    });

    const purgeAuditLogsMutation = useMutation({
        mutationFn: () => purgeAuditLogs(token),
        onSuccess: () => refetchAuditLogs(),
    });

    const unbanIpMutation = useMutation({
        mutationFn: (ipAddress: string) => unbanIp(ipAddress, token),
        onSuccess: () => refetchBannedIps(),
    });

    const clearAllBansMutation = useMutation({
        mutationFn: () => clearAllBans(token),
        onSuccess: () => refetchBannedIps(),
    });

    const blockIpMutation = useMutation({
        mutationFn: () => blockIp(ipToBlock, durationHours, blockReason, token),
        onSuccess: () => {
            refetchIpBlocks();
            setIpToBlock("");
            setDurationHours(1);
            setBlockReason("");
        },
    });

    const unblockIpMutation = useMutation({
        mutationFn: (ipAddress: string) => unblockIp(ipAddress, token),
        onSuccess: () => refetchIpBlocks(),
    });

    return (
        <div className="min-h-screen bg-gray-900 text-green-400 font-mono tracking-wide p-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Audit Logs Section */}
                <div className="bg-gray-800 p-4 rounded-lg">
                    <h2 className="text-xl mb-4">Audit Logs</h2>
                    {auditLogs && auditLogs.length > 0 ? (
                        <ul className="list-none max-h-64 overflow-y-auto p-0">
                            {auditLogs.map((log) => (
                                <li key={log.id} className="bg-gray-700 p-2 rounded mb-2 flex justify-between items-center">
                                    <span>{new Date(log.date).toLocaleString()} - {log.action}: {log.details.substring(0, 50)}...</span>
                                    <button
                                        onClick={() => deleteAuditLogMutation.mutate(log.id)}
                                        className="bg-red-500 text-white px-2 py-1 rounded hover:bg-red-600 ml-2"
                                    >
                                        Delete
                                    </button>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p>No audit logs available.</p>
                    )}
                    <button
                        onClick={() => purgeAuditLogsMutation.mutate()}
                        className="mt-4 bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600"
                    >
                        Purge All Logs
                    </button>
                </div>

                {/* Banned IPs Section */}
                <div className="bg-gray-800 p-4 rounded-lg">
                    <h2 className="text-xl mb-4">Banned IPs</h2>
                    {bannedIps && bannedIps.length > 0 ? (
                        <ul className="list-none max-h-64 overflow-y-auto p-0">
                            {bannedIps.map((ip) => (
                                <li key={ip.ip} className="bg-gray-700 p-2 rounded mb-2 flex justify-between items-center">
                                    <span>{ip.ip} (Until: {new Date(ip.blockedUntil).toLocaleString()}) - Reason: {ip.reason}</span>
                                    <button
                                        onClick={() => unbanIpMutation.mutate(ip.ip)}
                                        className="bg-green-400 text-white px-2 py-1 rounded hover:bg-green-500 ml-2"
                                    >
                                        Unban
                                    </button>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p>No banned IPs.</p>
                    )}
                    <button
                        onClick={() => clearAllBansMutation.mutate()}
                        className="mt-4 bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600"
                    >
                        Clear All Bans
                    </button>
                </div>

                {/* IP Blocks Section */}
                <div className="bg-gray-800 p-4 rounded-lg col-span-2">
                    <h2 className="text-xl mb-4">IP Blocks</h2>
                    {ipBlocks && ipBlocks.length > 0 ? (
                        <ul className="list-none max-h-64 overflow-y-auto p-0">
                            {ipBlocks.map((ip) => (
                                <li key={ip.ip} className="bg-gray-700 p-2 rounded mb-2 flex justify-between items-center">
                                    <span>{ip.ip} (Until: {new Date(ip.blockedUntil).toLocaleString()}) - Reason: {ip.reason}</span>
                                    <button
                                        onClick={() => unblockIpMutation.mutate(ip.ip)}
                                        className="bg-green-400 text-white px-2 py-1 rounded hover:bg-green-500 ml-2"
                                    >
                                        Unblock
                                    </button>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p>No IP blocks.</p>
                    )}
                    <div className="mt-4">
                        <input
                            type="text"
                            value={ipToBlock}
                            onChange={(e) => setIpToBlock(e.target.value)}
                            placeholder="IP Address"
                            className="bg-gray-700 text-green-400 p-2 rounded mr-2 w-1/3"
                        />
                        <input
                            type="number"
                            value={durationHours}
                            onChange={(e) => setDurationHours(parseInt(e.target.value) || 1)}
                            min="1"
                            placeholder="Hours"
                            className="bg-gray-700 text-green-400 p-2 rounded mr-2 w-1/6"
                        />
                        <input
                            type="text"
                            value={blockReason}
                            onChange={(e) => setBlockReason(e.target.value)}
                            placeholder="Reason"
                            className="bg-gray-700 text-green-400 p-2 rounded mr-2 w-1/3"
                        />
                        <button
                            onClick={() => blockIpMutation.mutate()}
                            className="bg-green-400 text-white px-4 py-2 rounded hover:bg-green-500"
                        >
                            Block IP
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default AdminDashboard;