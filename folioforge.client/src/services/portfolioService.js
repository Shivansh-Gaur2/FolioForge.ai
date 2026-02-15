import apiClient from "../api/client";

export const PortfolioService = {
    getById: async (id) => {
        if(!id) throw new Error("Portfolio ID is required");
        return await apiClient.get(`/portfolios/${id}`);
    }
}