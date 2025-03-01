// src/pages/Account.tsx
import React, { useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { useQuery } from 'react-query';

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
    if (!response.ok) throw new Error(await response.text() || 'Failed to fetch audit logs');
    return response.json();
};

const Account: React.FC = () => {
    const { user, token } = useAuth();
    const navigate = useNavigate();

    const { data: userData, error: userError } = useQuery<UserData>(
        'user',
        () => fetchUser(token!),
        { enabled: !!token }
    );
    const { data: auditLogs, error: logsError } = useQuery<AuditLog[]>(
        'auditLogs',
        () => fetchAuditLogs(token!),
        { enabled: !!token }
    );

    useEffect(() => {
        if (!token || !user) navigate('/login');
    }, [token, user, navigate]);

    if (userError || logsError) return <div className="min-h-screen bg-gray-900 text-red-400 font-mono p-4">Error loading account</div>;

    return (
        <div className="min-h-screen bg-gray-900 text-green-400 font-mono p-4">
            <div className="max-w-4xl mx-auto pt-20">
                <h1 className="text-2xl font-bold mb-4">Account</h1>
                <div className="bg-gray-800 p-4 rounded mb-4">
                    <h2 className="text-xl mb-2">User Details</h2>
                    {userData ? (
                        <>
                            <p>Username: {userData.userName}</p>
                            <p>Email: {userData.email}</p>
                            <p>Name: {userData.firstName} {userData.lastName}</p>
                            <p>Roles: {userData.roles.join(', ')}</p>
                            <button
                                onClick={() => navigate('/homepage')}
                                className="mt-2 bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600"
                            >
                                Back to Homepage
                            </button>
                        </>
                    ) : (
                        <p>Loading user data...</p>
                    )}
                </div>
                <div className="bg-gray-800 p-4 rounded">
                    <h2 className="text-xl mb-2">Audit Logs</h2>
                    {auditLogs?.length ? (
                        <ul className="space-y-2">
                            {auditLogs.map(log => (
                                <li key={log.id} className="bg-gray-700 p-2 rounded">
                                    <span>{new Date(log.date).toLocaleString()}</span> - <strong>{log.action}</strong>: {log.details}
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p>No audit logs available.</p>
                    )}
                </div>
            </div>
        </div>
    );
};

export default Account;