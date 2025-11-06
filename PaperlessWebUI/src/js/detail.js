// Document Detail functionality
class DocumentDetail {
    constructor() {
        this.documentId = null;
        this.currentDocument = null;
        this.init();
    }

    init() {
        this.documentId = utils.getUrlParameter('id');

        if (!this.documentId) {
            this.showError('No document ID provided');
            return;
        }

        this.bindEvents();
        this.loadDocument();
    }

    bindEvents() {
        // Edit document modal
        const editDocumentBtn = document.getElementById('editDocumentBtn');
        const editDocumentModal = document.getElementById('editDocumentModal');
        const cancelEditBtn = document.getElementById('cancelEdit');
        const editDocumentForm = document.getElementById('editDocumentForm');

        if (editDocumentBtn) {
            editDocumentBtn.addEventListener('click', () => this.showEditModal());
        }

        if (cancelEditBtn) {
            cancelEditBtn.addEventListener('click', () => this.hideEditModal());
        }

        if (editDocumentForm) {
            editDocumentForm.addEventListener('submit', (e) => this.handleEditDocument(e));
        }

        // Delete document modal
        const deleteDocumentBtn = document.getElementById('deleteDocumentBtn');
        const deleteConfirmModal = document.getElementById('deleteConfirmModal');
        const cancelDeleteBtn = document.getElementById('cancelDelete');
        const confirmDeleteBtn = document.getElementById('confirmDelete');

        if (deleteDocumentBtn) {
            deleteDocumentBtn.addEventListener('click', () => this.showDeleteModal());
        }

        if (cancelDeleteBtn) {
            cancelDeleteBtn.addEventListener('click', () => this.hideDeleteModal());
        }

        if (confirmDeleteBtn) {
            confirmDeleteBtn.addEventListener('click', () => this.handleDeleteDocument());
        }

        // Close modals when clicking outside
        if (editDocumentModal) {
            editDocumentModal.addEventListener('click', (e) => {
                if (e.target === editDocumentModal) {
                    this.hideEditModal();
                }
            });
        }

        if (deleteConfirmModal) {
            deleteConfirmModal.addEventListener('click', (e) => {
                if (e.target === deleteConfirmModal) {
                    this.hideDeleteModal();
                }
            });
        }

        // Close modals with X button
        document.querySelectorAll('.close').forEach(closeBtn => {
            closeBtn.addEventListener('click', (e) => {
                const modal = e.target.closest('.modal');
                if (modal) {
                    modal.style.display = 'none';
                }
            });
        });
    }

    async loadDocument() {
        try {
            utils.showLoading('loading');
            utils.hideError('errorMessage');

            this.currentDocument = await documentService.getDocumentById(this.documentId);
            this.renderDocument();

        } catch (error) {
            this.showError(error.message);
        } finally {
            utils.hideLoading('loading');
        }
    }

    renderDocument() {
        if (!this.currentDocument) return;

        const doc = this.currentDocument;
        const documentContent = document.getElementById('documentContent');

        if (documentContent) {
            documentContent.style.display = 'block';
        }

        // Handle both backend (PascalCase) and frontend (camelCase) property names
        const fileName = doc.fileName || doc.FileName || doc.name || 'Unknown';
        const fileSize = doc.byteSize || doc.ByteSize || 0;
        const lastModified = doc.lastModified || doc.LastModified || doc.createdAt || new Date().toISOString();
        const summary = doc.summary || doc.Summary || 'No summary available';

        // Extract file type from filename
        let fileType = doc.filetype || doc.FileType || '';
        if (!fileType && fileName.includes('.')) {
            fileType = fileName.split('.').pop();
        }

        // Update document title and details
        this.updateElement('documentTitle', fileName);
        this.updateElement('documentFiletype', fileType ? fileType.toUpperCase() : 'N/A');
        this.updateElement('documentByteSize', utils.formatFileSize(fileSize));
        this.updateElement('documentCreatedAt', utils.formatDate(lastModified));
        this.updateElement('documentUpdatedAt', utils.formatDate(lastModified));
        this.updateElement('documentSummary', summary);
    }

    updateElement(elementId, content) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = content;
        }
    }

    showEditModal() {
        if (!this.currentDocument) return;

        const modal = document.getElementById('editDocumentModal');
        if (modal) {
            const doc = this.currentDocument;
            const fileName = doc.fileName || doc.FileName || doc.name || '';

            const editName = document.getElementById('editDocumentName');
            if (editName) editName.value = fileName;

            modal.style.display = 'block';
            editName?.focus();
        }
    }

    hideEditModal() {
        const modal = document.getElementById('editDocumentModal');
        if (modal) {
            modal.style.display = 'none';
        }
    }

    async handleEditDocument(event) {
        event.preventDefault();

        const documentData = {
            fileName: document.getElementById('editDocumentName')?.value,
            byteSize: this.currentDocument.byteSize || this.currentDocument.ByteSize || 0,
            summary: this.currentDocument.summary || this.currentDocument.Summary || '',
            lastModified: new Date().toISOString()
        };

        if (!documentData.fileName) {
            this.showError('Please enter a file name.');
            return;
        }

        try {
            utils.hideError('errorMessage');

            this.currentDocument = await documentService.updateDocument(this.documentId, documentData);
            this.hideEditModal();
            this.renderDocument();

            this.showSuccess('Document updated successfully!');

        } catch (error) {
            this.showError(error.message);
        }
    }

    showDeleteModal() {
        if (!this.currentDocument) return;

        const modal = document.getElementById('deleteConfirmModal');
        const deleteDocumentName = document.getElementById('deleteDocumentName');

        if (modal && deleteDocumentName) {
            deleteDocumentName.textContent = this.currentDocument.fileName || this.currentDocument.FileName || this.currentDocument.name || 'Unknown';
            modal.style.display = 'block';
        }
    }

    hideDeleteModal() {
        const modal = document.getElementById('deleteConfirmModal');
        if (modal) {
            modal.style.display = 'none';
        }
    }

    async handleDeleteDocument() {
        try {
            utils.hideError('errorMessage');

            await documentService.deleteDocument(this.documentId);

            // Redirect to dashboard after successful deletion
            window.location.href = 'index.html';

        } catch (error) {
            this.showError(error.message);
            this.hideDeleteModal();
        }
    }

    showError(message) {
        utils.showError('errorMessage', message);

        // Hide document content if there's an error loading
        const documentContent = document.getElementById('documentContent');
        if (documentContent) {
            documentContent.style.display = 'none';
        }
    }

    showSuccess(message) {
        // Create and show success message
        let successElement = document.getElementById('successMessage');
        if (!successElement) {
            successElement = document.createElement('div');
            successElement.id = 'successMessage';
            successElement.className = 'success';
            document.querySelector('main').prepend(successElement);
        }

        successElement.textContent = message;
        successElement.style.display = 'block';

        // Hide after 3 seconds
        setTimeout(() => {
            if (successElement) {
                successElement.style.display = 'none';
            }
        }, 3000);
    }
}

// Initialize document detail when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new DocumentDetail();
});