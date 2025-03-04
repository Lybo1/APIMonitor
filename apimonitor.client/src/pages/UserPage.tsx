import React, { useState, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import { useNavigate } from "react-router-dom";
import { useQuery, useMutation } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { UserIcon } from "@heroicons/react/24/solid";

interface UserData {
    id: number;
    userName: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    roles: string[];
}

interface AuditLog {
    id: number;
    userId: number;
    action: string;
    details: string;
    date: string;
    ipv4Address: string;
    ipv6Address: string;
    userAgent: string;
    requestTimestamp: string;
    responseTimeMs: number;
}

const fetchUser = async (token: string) => {
    const response = await fetch("http://localhost:5028/api/User", {
        headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
        credentials: "include",
    });
    if (!response.ok) throw new Error(await response.text() || "Failed to fetch user");
    return response.json();
};

const fetchAuditLogs = async (token: string) => {
    const response = await fetch("http://localhost:5028/api/AuditLog/user-logs", {
        headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
        credentials: "include",
    });
    if (!response.ok) return [];
    return response.json();
};

const UserPage: React.FC = () => {
    const { user, token, logout, setUser } = useAuth();
    const navigate = useNavigate();
    const [username, setUsername] = useState("");
    const [firstName, setFirstName] = useState<string>("");
    const [lastName, setLastName] = useState<string>("");
    const [password, setPassword] = useState("");
    const [currentPassword, setCurrentPassword] = useState("");
    const [page, setPage] = useState(1);
    const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null); // State for modal log
    const logsPerPage = 10;

    const { data: userData, refetch: refetchUser } = useQuery<UserData, Error>({
        queryKey: ["user"],
        queryFn: () => fetchUser(token!),
        enabled: !!token,
        retry: false,
    });

    const { data: auditLogs } = useQuery<AuditLog[], Error>({ // Removed refetchAuditLogs since it's unused
        queryKey: ["auditLogs"],
        queryFn: () => fetchAuditLogs(token!),
        enabled: !!token,
        retry: false,
    });

    const updateUsername = useMutation({
        mutationFn: async () => {
            const response = await fetch("http://localhost:5028/api/User/username", {
                method: "PUT",
                headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
                body: JSON.stringify({ newUsername: username }),
                credentials: "include",
            });
            if (!response.ok) throw new Error(await response.text() || "Failed to update username");
            return response.json();
        },
        onSuccess: async () => {
            await refetchUser(); // Await the Promise to handle it properly
            if (user) setUser({ ...user, username });
        },
    });

    const updateName = useMutation({
        mutationFn: async () => {
            const response = await fetch("http://localhost:5028/api/User/name", {
                method: "PUT",
                headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
                body: JSON.stringify({ firstName, lastName }),
                credentials: "include",
            });
            if (!response.ok) throw new Error(await response.text() || "Failed to update name");
            return response.json();
        },
        onSuccess: async () => {
            await refetchUser(); // Await the Promise to handle it properly
            if (user) setUser({ ...user, firstName, lastName });
        },
    });

    const updatePassword = useMutation({
        mutationFn: async () => {
            const response = await fetch("http://localhost:5028/api/User/password", {
                method: "PUT",
                headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
                body: JSON.stringify({ currentPassword, newPassword: password }),
                credentials: "include",
            });
            if (!response.ok) throw new Error(await response.text() || "Failed to update password");
            return response.json();
        },
    });

    const deleteAccount = useMutation({
        mutationFn: async () => {
            const response = await fetch("http://localhost:5028/api/User", {
                method: "DELETE",
                headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
                body: JSON.stringify({ currentPassword }),
                credentials: "include",
            });
            if (!response.ok) throw new Error(await response.text() || "Failed to delete account");
            return response.json();
        },
        onSuccess: () => logout(),
    });

    useEffect(() => {
        if (userData) {
            setUsername(userData.userName);
            setFirstName(userData.firstName || "");
            setLastName(userData.lastName || "");
        }
    }, [userData]);

    const paginatedLogs = auditLogs?.slice((page - 1) * logsPerPage, page * logsPerPage) || [];
    const totalPages = Math.ceil((auditLogs?.length || 0) / logsPerPage);

    const truncateText = (text: string, maxLength: number) => {
        if (text.length > maxLength) return text.substring(0, maxLength) + "...";
        return text;
    };

    const closeModal = () => setSelectedLog(null);

    return (
        <div className="min-h-screen bg-gray-900 text-green-400 font-mono p-4">
            <div className="max-w-6xl mx-auto pt-20 flex flex-row items-start">
                <motion.div
                    className="mr-8"
                    initial={{ scale: 1 }}
                    whileHover={{ scale: 1.1 }} // Zoom on hover
                    transition={{ duration: 0.3 }}
                >
                    <div className="border border-black rounded-full p-2"> {/* Added padding inside the border */}
                        <UserIcon
                            className="w-28 h-28 text-green-400" // Reduced icon size to create space within the border
                        />
                    </div>
                </motion.div>

                {/* User Data and Audit Logs in a Vertical Stack on the Right */}
                <div className="flex-1 grid grid-cols-1 gap-4">
                    {/* User Data in Bento Boxes */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <motion.div
                            className="bg-gray-800 p-4 rounded-lg border-4 border-green-500 shadow-lg"
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.5 }}
                        >
                            <label className="block mb-1 text-green-400 font-bold">Username</label>
                            <input
                                type="text"
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                className="w-full bg-gray-700 p-2 rounded text-green-400 placeholder-green-600 focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-600"
                            />
                            <motion.button
                                onClick={() => updateUsername.mutate()}
                                className="mt-2 bg-green-500 text-gray-900 px-4 py-1 rounded hover:scale-105 transition-all ease-in-out"
                                whileHover={{ scale: 1.05 }}
                                transition={{ duration: 0.2 }}
                            >
                                Update
                            </motion.button>
                        </motion.div>

                        <motion.div
                            className="bg-gray-800 p-4 rounded-lg border-4 border-green-500 shadow-lg"
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.5, delay: 0.1 }}
                        >
                            <label className="block mb-1 text-green-400 font-bold">Email</label>
                            <input
                                type="text"
                                value={userData?.email || ""}
                                disabled
                                className="w-full bg-gray-700 p-2 rounded text-green-400 opacity-50"
                            />
                        </motion.div>

                        <motion.div
                            className="bg-gray-800 p-4 rounded-lg border-4 border-green-500 shadow-lg"
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.5, delay: 0.2 }}
                        >
                            <label className="block mb-1 text-green-400 font-bold">First Name</label>
                            <input
                                type="text"
                                value={firstName}
                                onChange={(e) => setFirstName(e.target.value)}
                                className="w-full bg-gray-700 p-2 rounded text-green-400 placeholder-green-600 focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-600"
                            />
                        </motion.div>

                        <motion.div
                            className="bg-gray-800 p-4 rounded-lg border-4 border-green-500 shadow-lg"
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.5, delay: 0.3 }}
                        >
                            <label className="block mb-1 text-green-400 font-bold">Last Name</label>
                            <input
                                type="text"
                                value={lastName}
                                onChange={(e) => setLastName(e.target.value)}
                                className="w-full bg-gray-700 p-2 rounded text-green-400 placeholder-green-600 focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-600"
                            />
                            <motion.button
                                onClick={() => updateName.mutate()}
                                className="mt-2 bg-green-500 text-gray-900 px-4 py-1 rounded hover:scale-105 transition-all ease-in-out"
                                whileHover={{ scale: 1.05 }}
                                transition={{ duration: 0.2 }}
                            >
                                Update
                            </motion.button>
                        </motion.div>

                        <motion.div
                            className="bg-gray-800 p-4 rounded-lg border-4 border-green-500 shadow-lg col-span-2"
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.5, delay: 0.4 }}
                        >
                            <label className="block mb-1 text-green-400 font-bold">Current Password</label>
                            <input
                                type="password"
                                value={currentPassword}
                                onChange={(e) => setCurrentPassword(e.target.value)}
                                className="w-full bg-gray-700 p-2 rounded text-green-400 placeholder-green-600 focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-600"
                            />
                            <label className="block mb-1 mt-2 text-green-400 font-bold">New Password</label>
                            <input
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                className="w-full bg-gray-700 p-2 rounded text-green-400 placeholder-green-600 focus:outline-none focus:ring-2 focus:ring-green-400 transition-all ease-in-out hover:bg-gray-600"
                            />
                            <div className="mt-2 flex space-x-2">
                                <motion.button
                                    onClick={() => updatePassword.mutate()}
                                    className="bg-green-500 text-gray-900 px-4 py-1 rounded hover:scale-105 transition-all ease-in-out"
                                    whileHover={{ scale: 1.05 }}
                                    transition={{ duration: 0.2 }}
                                >
                                    Update Password
                                </motion.button>
                                <motion.button
                                    onClick={() => deleteAccount.mutate()}
                                    className="bg-red-500 text-white px-4 py-1 rounded hover:scale-105 transition-all ease-in-out"
                                    whileHover={{ scale: 1.05 }}
                                    transition={{ duration: 0.2 }}
                                >
                                    Delete Account
                                </motion.button>
                            </div>
                        </motion.div>
                    </div>

                    {/* Audit Logs Below User Data, Symmetrically Aligned */}
                    <div className="mt-4">
                        <div className="bg-gray-800 p-4 rounded border-4 border-green-500">
                            <h2 className="text-xl mb-2 text-green-400 font-bold">Audit Logs</h2>
                            {auditLogs && paginatedLogs.length ? (
                                <>
                                    <ul className="space-y-2">
                                        {paginatedLogs.map((log) => (
                                            <li
                                                key={log.id}
                                                className="bg-gray-700 p-2 rounded overflow-x-hidden cursor-pointer hover:bg-gray-600"
                                                onClick={() => setSelectedLog(log)} // Open modal on click
                                            >
                                                <span>{new Date(log.date).toLocaleString()}</span> - <strong>{log.action}</strong>:
                                                {truncateText(log.details, 100)}
                                                (IP: {truncateText(log.ipv4Address, 15)}, Response: {log.responseTimeMs}ms)
                                            </li>
                                        ))}
                                    </ul>
                                    <div className="flex justify-between mt-4">
                                        <motion.button
                                            onClick={() => setPage((p) => Math.max(1, p - 1))}
                                            disabled={page === 1}
                                            className="bg-green-500 text-gray-900 px-4 py-1 rounded hover:scale-105 transition-all ease-in-out"
                                            whileHover={{ scale: 1.05 }}
                                            transition={{ duration: 0.2 }}
                                        >
                                            Previous
                                        </motion.button>
                                        <span className="text-green-400">Page {page} of {totalPages}</span>
                                        <motion.button
                                            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                                            disabled={page === totalPages}
                                            className="bg-green-500 text-gray-900 px-4 py-1 rounded hover:scale-105 transition-all ease-in-out"
                                            whileHover={{ scale: 1.05 }}
                                            transition={{ duration: 0.2 }}
                                        >
                                            Next
                                        </motion.button>
                                    </div>
                                </>
                            ) : (
                                <p className="text-green-400">No audit logs available.</p>
                            )}
                        </div>

                        <motion.button
                            onClick={() => navigate("/homepage")}
                            className="mt-4 bg-gray-500 text-white px-4 py-1 rounded hover:scale-105 transition-all ease-in-out"
                            whileHover={{ scale: 1.05 }}
                            transition={{ duration: 0.2 }}
                        >
                            Back to Homepage
                        </motion.button>
                    </div>
                </div>
            </div>

            {selectedLog && (
                <div
                    className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
                    onClick={closeModal}
                >
                    <div
                        className="bg-gray-800 p-6 rounded-lg max-w-3xl w-full max-h-[90vh] overflow-y-auto shadow-lg"
                        onClick={(e) => e.stopPropagation()} // Prevent closing on modal click
                    >
                        <h3 className="text-xl font-bold mb-4 text-green-400">Audit Log Details</h3>
                        <p>
                            <strong className="text-green-400">Date:</strong>{" "}
                            {new Date(selectedLog.date).toLocaleString()}
                        </p>
                        <p>
                            <strong className="text-green-400">Action:</strong> {selectedLog.action}
                        </p>
                        <p>
                            <strong className="text-green-400">Details:</strong> {selectedLog.details}
                        </p>
                        <p>
                            <strong className="text-green-400">IP (IPv4):</strong> {selectedLog.ipv4Address}
                        </p>
                        <p>
                            <strong className="text-green-400">IP (IPv6):</strong> {selectedLog.ipv6Address}
                        </p>
                        <p>
                            <strong className="text-green-400">User Agent:</strong> {selectedLog.userAgent}
                        </p>
                        <p>
                            <strong className="text-green-400">Request Timestamp:</strong>{" "}
                            {new Date(selectedLog.requestTimestamp).toLocaleString()}
                        </p>
                        <p>
                            <strong className="text-green-400">Response Time:</strong> {selectedLog.responseTimeMs}ms
                        </p>
                        <motion.button
                            onClick={closeModal}
                            className="mt-4 bg-red-500 text-white px-4 py-2 rounded hover:scale-105 transition-all ease-in-out"
                            whileHover={{ scale: 1.05 }}
                            transition={{ duration: 0.2 }}
                        >
                            Close
                        </motion.button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default UserPage;