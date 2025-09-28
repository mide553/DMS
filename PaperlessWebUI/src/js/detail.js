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

        // Update document title and details
        this.updateElement('documentTitle', `${doc.name}`);
        this.updateElement('documentName', doc.name);
        this.updateElement('documentFiletype', doc.filetype.toUpperCase());
        this.updateElement('documentByteSize', utils.formatFileSize(doc.byteSize));
        this.updateElement('documentCreatedAt', utils.formatDate(doc.createdAt));
        this.updateElement('documentUpdatedAt', utils.formatDate(doc.updatedAt));
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
            // Pre-fill form with current document data
            document.getElementById('editDocumentName').value = this.currentDocument.name;
            document.getElementById('editDocumentFiletype').value = this.currentDocument.filetype;
            document.getElementById('editDocumentByteSize').value = this.currentDocument.byteSize;

            modal.style.display = 'block';
            document.getElementById('editDocumentName')?.focus();
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
            name: document.getElementById('editDocumentName')?.value,
            filetype: document.getElementById('editDocumentFiletype')?.value,
            byteSize: document.getElementById('editDocumentByteSize')?.value,
            createdAt: this.currentDocument.createdAt
        };

        // Validation
        if (!documentData.name || !documentData.filetype || !documentData.byteSize) {
            this.showError('Please fill in all required fields.');
            return;
        }

        if (parseInt(documentData.byteSize) < 0) {
            this.showError('File size must be a positive number.');
            return;
        }

        try {
            utils.hideError('errorMessage');

            const updatedDocument = await documentService.updateDocument(this.documentId, documentData);
            this.currentDocument = updatedDocument;
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
            deleteDocumentName.textContent = this.currentDocument.name;
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