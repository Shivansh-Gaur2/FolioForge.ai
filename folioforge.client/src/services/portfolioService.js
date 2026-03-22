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
     * Get a public portfolio by slug (no auth required).
     * GET /api/p/:slug
     */
    getPublicBySlug: async (slug) => {
        if (!slug) throw new Error("Portfolio slug is required");
        return await apiClient.get(`/p/${slug}`);
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
                timeout: 60000,
            }
        );
    },

    /**
     * Publish a portfolio (make it publicly viewable).
     * POST /api/portfolios/:id/publish
     */
    publish: async (id) => {
        return await apiClient.post(`/portfolios/${id}/publish`);
    },

    /**
     * Unpublish a portfolio (make it private).
     * POST /api/portfolios/:id/unpublish
     */
    unpublish: async (id) => {
        return await apiClient.post(`/portfolios/${id}/unpublish`);
    },
};