import { useState, useEffect, useCallback } from 'react';
import { PortfolioService } from '../services/portfolioService';

/**
 * usePortfolio Hook
 * 
 * Pattern: Custom hook for data fetching with proper lifecycle management.
 * 
 * Features:
 * - Race condition prevention via cleanup
 * - Retry capability
 * - Refetch on ID change
 * - Error state preservation for display
 */
export const usePortfolio = (portfolioId) => {
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const fetchData = useCallback(async (signal) => {
        if (!portfolioId) {
            setLoading(false);
            return;
        }
        
        setLoading(true);
        setError(null);
        
        try {
            const result = await PortfolioService.getById(portfolioId);
            if (!signal?.aborted) {
                setData(result);
            }
        } catch (err) {
            if (!signal?.aborted) {
                setError(err);
            }
        } finally {
            if (!signal?.aborted) {
                setLoading(false);
            }
        }
    }, [portfolioId]);

    useEffect(() => {
        const abortController = new AbortController();
        fetchData(abortController.signal);
        return () => abortController.abort();
    }, [fetchData]);

    // Retry function for error recovery
    const retry = useCallback(() => {
        fetchData();
    }, [fetchData]);

    return { 
        portfolio: data, 
        loading, 
        error,
        retry,
        isError: !!error,
        isSuccess: !loading && !error && !!data,
    };
};