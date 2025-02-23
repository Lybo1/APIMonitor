import axiosInstance from "../api/axiosInstance.ts";

const API_URL = '';

export const roleService = {
    async createRole(roleName: string) {
        try {
            const response = await axiosInstance.post(`${API_URL}/roles`, { roleName }); //tweak the path?
            return response.data;
        } catch {
            throw new Error('Unable to create role.')
        }
    },

    async deleteRole(roleName: string) {
        try {
            const response = await axiosInstance.delete(`${API_URL}/roles/${roleName}`);
            return response.data;
        } catch {
            throw new Error('Unable to delete role');
        }
    },

    async getAllRoles() {
        try {
            const response = await axiosInstance.get(`${API_URL}/roles`);
            return response.data;
        } catch {
            throw new Error('Unable to fetch roles');
        }
    },
};