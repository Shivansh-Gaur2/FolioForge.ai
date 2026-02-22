import apiClient from '../api/client';

/**
 * Customization API service.
 * Manages portfolio theme, colors, fonts, layout, and section configuration.
 */
export const CustomizationService = {
    /**
     * Get the customization for a portfolio (returns the portfolio itself with theme + sections).
     * GET /api/portfolios/:id
     */
    get: async (portfolioId) => {
        if (!portfolioId) throw new Error('Portfolio ID is required');
        return await apiClient.get(`/portfolios/${portfolioId}`);
    },

    /**
     * Save the full customization (theme + section order/visibility/variant).
     * PUT /api/portfolios/:id/customization
     */
    save: async (portfolioId, customization) => {
        if (!portfolioId) throw new Error('Portfolio ID is required');
        return await apiClient.put(`/portfolios/${portfolioId}/customization`, customization);
    },
};
