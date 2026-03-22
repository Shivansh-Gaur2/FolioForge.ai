import apiClient from '../api/client';

export const BillingService = {
    /**
     * Get all available plans.
     * GET /api/billing/plans
     */
    getPlans: async () => {
        return await apiClient.get('/billing/plans');
    },

    /**
     * Get current user's billing/subscription status.
     * GET /api/billing/status
     */
    getStatus: async () => {
        return await apiClient.get('/billing/status');
    },

    /**
     * Create a Razorpay Subscription on the backend.
     * POST /api/billing/create-subscription
     */
    createSubscription: async ({ planId, billingInterval }) => {
        return await apiClient.post('/billing/create-subscription', {
            planId,
            billingInterval,
        });
    },

    /**
     * Verify a Razorpay payment after checkout modal success.
     * POST /api/billing/verify-payment
     */
    verifyPayment: async ({ razorpayPaymentId, razorpaySubscriptionId, razorpaySignature, planId }) => {
        return await apiClient.post('/billing/verify-payment', {
            razorpayPaymentId,
            razorpaySubscriptionId,
            razorpaySignature,
            planId,
        });
    },

    /**
     * Open the Razorpay checkout modal.
     * @param {Object} options - { subscriptionId, razorpayKeyId, userEmail, userName, planId, onSuccess, onError }
     */
    openCheckout: ({ subscriptionId, razorpayKeyId, userEmail, userName, planId, onSuccess, onError }) => {
        const options = {
            key: razorpayKeyId,
            subscription_id: subscriptionId,
            name: 'FolioForge',
            description: 'Pro Plan Subscription',
            prefill: {
                email: userEmail,
                name: userName,
            },
            theme: {
                color: '#6366f1',
            },
            handler: async (response) => {
                // response = { razorpay_payment_id, razorpay_subscription_id, razorpay_signature }
                try {
                    const result = await BillingService.verifyPayment({
                        razorpayPaymentId: response.razorpay_payment_id,
                        razorpaySubscriptionId: response.razorpay_subscription_id,
                        razorpaySignature: response.razorpay_signature,
                        planId,
                    });
                    onSuccess?.(result);
                } catch (err) {
                    onError?.(err);
                }
            },
            modal: {
                ondismiss: () => {
                    onError?.(new Error('Payment cancelled'));
                },
            },
        };

        const rzp = new window.Razorpay(options);
        rzp.open();
    },
};
