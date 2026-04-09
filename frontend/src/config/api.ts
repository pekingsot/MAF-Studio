const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

export const getApiUrl = (path: string) => `${API_BASE_URL}${path}`;

export default API_BASE_URL;
