declare global {
  interface Window {
    trustVaultConfig?: {
      apiBaseUrl?: string;
    };
  }
}

export const API_BASE_URL =
  (window.trustVaultConfig?.apiBaseUrl || 'http://localhost:5000/api').replace(/\/+$/, '');
