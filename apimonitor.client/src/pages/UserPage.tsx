import React, { useState, useEffect, useRef } from 'react'; // Add useRef here
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { Canvas, useFrame } from '@react-three/fiber';
import { OrbitControls, Sphere } from '@react-three/drei';
import * as THREE from 'three';

// Simplified User Icon with rotation
const UserIcon3D: React.FC = () => {
    const meshRef = useRef<THREE.Mesh>(null!);

    useFrame(() => {
        if (meshRef.current) {
            meshRef.current.rotation.y += 0.01; // Rotate gently
        }
    });

    return (
        <Sphere args={[1, 32, 32]} ref={meshRef}>
            <meshStandardMaterial color="green" emissive="green" emissiveIntensity={0.3} />
        </Sphere>
    );
};

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
    macAddress: string;
    userAgent: string;
    requestTimestamp: string;
    responseTimeMs: number;
}

const fetchUser = async (token: string) => {
    const response = await fetch('http://localhost:5028/api/User', {
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        credentials: 'include',
    });
    if (!response.ok) throw new Error(await response.text() || 'Failed to fetch user');
    return response.json();
};

const fetchAuditLogs = async (token: string) => {
    const response = await fetch('http://localhost:5028/api/AuditLog/user-logs', {
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        credentials: 'include',
    });
    if (!response.ok) return [];
    return response.json();
};

const UserPage: React.FC = () => {
    const { user, token, logout, setUser } = useAuth();
    const navigate = useNavigate();
    const [username, setUsername] = useState('');
    const [firstName, setFirstName] = useState<string>('');
    const [lastName, setLastName] = useState<string>('');
    const [password, setPassword] = useState('');
    const [currentPassword, setCurrentPassword] = useState('');
    const [page, setPage] = useState(1);
    const logsPerPage = 10;

    const { data: userData, refetch: refetchUser } = useQuery({
        queryKey: ['user'],
        queryFn: () => fetchUser(token!),
        enabled: !!token,
    });

    const { data: auditLogs } = useQuery({
        queryKey: ['auditLogs'],
        queryFn: () => fetchAuditLogs(token!),
        enabled: !!token,
    });

    const updateUsername = useMutation({
        mutationFn: async () => {
            const response = await fetch('http://localhost:5028/api/User/username', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ newUsername: username }),
                credentials: 'include',
            });
            if (!response.ok) throw new Error(await response.text() || 'Failed to update username');
            return response.json();
        },
        onSuccess: () => {
            refetchUser();
            if (user) setUser({ ...user, username });
        },
    });

    const updateName = useMutation({
        mutationFn: async () => {
            const response = await fetch('http://localhost:5028/api/User/name', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ firstName, lastName }),
                credentials: 'include',
            });
            if (!response.ok) throw new Error(await response.text() || 'Failed to update name');
            return response.json();
        },
        onSuccess: () => {
            refetchUser();
            if (user) setUser({ ...user, firstName, lastName });
        },
    });

    const updatePassword = useMutation({
        mutationFn: async () => {
            const response = await fetch('http://localhost:5028/api/User/password', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ currentPassword, newPassword: password }),
                credentials: 'include',
            });
            if (!response.ok) throw new Error(await response.text() || 'Failed to update password');
            return response.json();
        },
    });

    const deleteAccount = useMutation({
        mutationFn: async () => {
            const response = await fetch('http://localhost:5028/api/User', {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify({ currentPassword, newPassword: password }),
                credentials: 'include',
            });
            if (!response.ok) throw new Error(await response.text() || 'Failed to delete account');
            return response.json();
        },
        onSuccess: () => logout(),
    });

    useEffect(() => {
        if (userData) {
            setUsername(userData.userName);
            setFirstName(userData.firstName || '');
            setLastName(userData.lastName || '');
        }
    }, [userData]);

    const paginatedLogs = auditLogs?.slice((page - 1) * logsPerPage, page * logsPerPage) || [];
    const totalPages = Math.ceil((auditLogs?.length || 0) / logsPerPage);

    return (
        <div className="min-h-screen bg-gray-900 text-green-400 font-mono p-4">
            <div className="max-w-4xl mx-auto pt-20">
                <motion.div className="flex justify-center mb-6" whileHover={{ scale: 1.1 }}>
                    <Canvas style={{ width: '150px', height: '150px' }}>
                        <ambientLight intensity={0.5} />
                        <pointLight position={[10, 10, 10]} intensity={1.5} />
                        <UserIcon3D />
                        <OrbitControls enablePan={false} enableZoom={false} autoRotate />
                    </Canvas>
                </motion.div>

                <h1 className="text-2xl font-bold mb-4 text-center">Account Settings</h1>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
                    <div className="bg-gray-800 p-4 rounded">
                        <label className="block mb-1">Username</label>
                        <input
                            type="text"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            className="w-full bg-gray-700 p-2 rounded"
                        />
                        <button
                            onClick={() => updateUsername.mutate()}
                            className="mt-2 bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600"
                        >
                            Update
                        </button>
                    </div>
                    <div className="bg-gray-800 p-4 rounded">
                        <label className="block mb-1">Email</label>
                        <input
                            type="text"
                            value={userData?.email || ''}
                            disabled
                            className="w-full bg-gray-700 p-2 rounded opacity-50"
                        />
                    </div>
                    <div className="bg-gray-800 p-4 rounded">
                        <label className="block mb-1">First Name</label>
                        <input
                            type="text"
                            value={firstName}
                            onChange={(e) => setFirstName(e.target.value)}
                            className="w-full bg-gray-700 p-2 rounded"
                        />
                    </div>
                    <div className="bg-gray-800 p-4 rounded">
                        <label className="block mb-1">Last Name</label>
                        <input
                            type="text"
                            value={lastName}
                            onChange={(e) => setLastName(e.target.value)}
                            className="w-full bg-gray-700 p-2 rounded"
                        />
                        <button
                            onClick={() => updateName.mutate()}
                            className="mt-2 bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600"
                        >
                            Update
                        </button>
                    </div>
                    <div className="bg-gray-800 p-4 rounded col-span-2">
                        <label className="block mb-1">Current Password</label>
                        <input
                            type="password"
                            value={currentPassword}
                            onChange={(e) => setCurrentPassword(e.target.value)}
                            className="w-full bg-gray-700 p-2 rounded"
                        />
                        <label className="block mb-1 mt-2">New Password</label>
                        <input
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full bg-gray-700 p-2 rounded"
                        />
                        <button
                            onClick={() => updatePassword.mutate()}
                            className="mt-2 bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600"
                        >
                            Update Password
                        </button>
                        <button
                            onClick={() => deleteAccount.mutate()}
                            className="mt-2 ml-2 bg-red-500 text-white px-4 py-1 rounded hover:bg-red-600"
                        >
                            Delete Account
                        </button>
                    </div>
                </div>

                <div className="bg-gray-800 p-4 rounded">
                    <h2 className="text-xl mb-2">Audit Logs</h2>
                    {paginatedLogs.length ? (
                        <>
                            <ul className="space-y-2">
                                {paginatedLogs.map(log => (
                                    <li key={log.id} className="bg-gray-700 p-2 rounded">
                                        <span>{new Date(log.date).toLocaleString()}</span> - <strong>{log.action}</strong>: {log.details}
                                        (IP: {log.ipv4Address}, Response: {log.responseTimeMs}ms)
                                    </li>
                                ))}
                            </ul>
                            <div className="flex justify-between mt-4">
                                <button
                                    onClick={() => setPage(p => Math.max(1, p - 1))}
                                    disabled={page === 1}
                                    className="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600 disabled:bg-gray-500"
                                >
                                    Previous
                                </button>
                                <span>Page {page} of {totalPages}</span>
                                <button
                                    onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                                    disabled={page === totalPages}
                                    className="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600 disabled:bg-gray-500"
                                >
                                    Next
                                </button>
                            </div>
                        </>
                    ) : (
                        <p>No audit logs available.</p>
                    )}
                </div>

                <button
                    onClick={() => navigate('/homepage')}
                    className="mt-4 bg-gray-500 text-white px-4 py-1 rounded hover:bg-gray-600"
                >
                    Back to Homepage
                </button>
            </div>
        </div>
    );
};

export default UserPage;