// API Configuration
const API_BASE_URL = '/api';

// API Client class for handling all HTTP requests
class ApiClient {
    constructor(baseUrl = API_BASE_URL) {
        this.baseUrl = baseUrl;
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;

        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
            },
        };

        const finalOptions = {
            ...defaultOptions,
            ...options,
            headers: {
                ...defaultOptions.headers,
                ...options.headers,
            },
        };

        const response = await fetch(url, finalOptions);

        if (!response.ok) {
            console.error('API request failed with status:', response.status);
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // Handle empty responses (like DELETE)
        if (response.status === 204) {
            return null;
        }

        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        }

        return await response.text();
    }

    // GET
    async get(endpoint) {
        return this.request(endpoint, { method: 'GET' });
    }

    // POST
    async post(endpoint, data) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(data),
        });
    }

    // PUT
    async put(endpoint, data) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    }

    // DELETE
    async delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    }
}

// Document API service
class DocumentService {
    constructor() {
        this.apiClient = new ApiClient();
    }

    // Get all documents
    async getAllDocuments() {
        try {
            return await this.apiClient.get('/documents');
        } catch (error) {
            console.error('Failed to fetch documents:', error);
            throw new Error('Failed to load documents. Please try again.');
        }
    }

    // Get document by ID
    async getDocumentById(id) {
        try {
            return await this.apiClient.get(`/documents/${id}`);
        } catch (error) {
            console.error(`Failed to fetch document ${id}:`, error);
            if (error.message.includes('404')) {
                throw new Error('Document not found.');
            }
            throw new Error('Failed to load document. Please try again.');
        }
    }

    // Update document
    async updateDocument(id, documentData) {
        try {
            const document = {
                fileName: documentData.fileName,
                byteSize: parseInt(documentData.byteSize),
                summary: documentData.summary || '',
                lastModified: documentData.lastModified || new Date().toISOString()
            };
            return await this.apiClient.put(`/documents/${id}`, document);
        } catch (error) {
            console.error(`Failed to update document ${id}:`, error);
            if (error.message.includes('404')) {
                throw new Error('Document not found.');
            }
            throw new Error('Failed to update document. Please check your input and try again.');
        }
    }

    // Delete document
    async deleteDocument(id) {
        try {
            return await this.apiClient.delete(`/documents/${id}`);
        } catch (error) {
            console.error(`Failed to delete document ${id}:`, error);
            if (error.message.includes('404')) {
                throw new Error('Document not found.');
            }
            throw new Error('Failed to delete document. Please try again.');
        }
    }
}

// Utility functions
const utils = {
    // Format file size in human readable format
    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    },

    // Format date in human readable format
    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    },


    // Show error message
    showError(elementId, message) {
        const errorElement = document.getElementById(elementId);
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = 'block';
        }
    },

    // Hide error message
    hideError(elementId) {
        const errorElement = document.getElementById(elementId);
        if (errorElement) {
            errorElement.style.display = 'none';
        }
    },

    // Show loading state
    showLoading(elementId) {
        const loadingElement = document.getElementById(elementId);
        if (loadingElement) {
            loadingElement.style.display = 'block';
        }
    },

    // Hide loading state
    hideLoading(elementId) {
        const loadingElement = document.getElementById(elementId);
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
    },

    // Get URL parameters
    getUrlParameter(name) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }
};

// Global document service instance
const documentService = new DocumentService();