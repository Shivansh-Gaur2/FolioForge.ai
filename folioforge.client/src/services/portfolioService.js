import apiClient from "../api/client";

export const PortfolioService = {
    /**
     * List all portfolios for the current authenticated user.
     * GET /api/portfolios/mine
     */
    listMine: async () => {
        return await apiClient.get('/portfolios/mine');
    },

    /**
     * Get a single portfolio by ID (with sections).
     * GET /api/portfolios/:id
     */
    getById: async (id) => {
        if (!id) throw new Error("Portfolio ID is required");
        return await apiClient.get(`/portfolios/${id}`);
    },

    /**
     * Create a new portfolio.
     * POST /api/portfolios  { title, slug }
     * Returns { id }
     */
    create: async ({ title, slug }) => {
        return await apiClient.post('/portfolios', { title, slug });
    },

    /**
     * Delete a portfolio by ID.
     * DELETE /api/portfolios/:id
     */
    delete: async (id) => {
        if (!id) throw new Error("Portfolio ID is required");
        return await apiClient.delete(`/portfolios/${id}`);
    },

    /**
     * Upload a resume PDF for AI processing.
     * POST /api/portfolios/:id/upload-resume  (multipart/form-data)
     * Returns 202 { message, portfolioId }
     */
    uploadResume: async (portfolioId, file) => {
        const formData = new FormData();
        formData.append('file', file);

        return await apiClient.post(
            `/portfolios/${portfolioId}/upload-resume`,
            formData,
            {
                headers: { 'Content-Type': 'multipart/form-data' },
                timeout: 60000, // PDF uploads may take longer
            }
        );
    },
};