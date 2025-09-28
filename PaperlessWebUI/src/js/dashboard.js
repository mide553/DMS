class Dashboard {
    constructor() {
        this.documents = [];
        this.filteredDocuments = [];
        this.init();
    }

    init() {
        this.bindEvents();
        this.loadDocuments();
    }

    bindEvents() {
        // Search functionality
        const searchInput = document.getElementById('searchInput');
        const searchBtn = document.getElementById('searchBtn');

        if (searchInput && searchBtn) {
            searchInput.addEventListener('input', () => this.filterDocuments());
            searchBtn.addEventListener('click', () => this.filterDocuments());
        }

        // Add document modal
        const addDocumentBtn = document.getElementById('addDocumentBtn');
        const addFirstDocument = document.getElementById('addFirstDocument');
        const addDocumentModal = document.getElementById('addDocumentModal');
        const cancelAddBtn = document.getElementById('cancelAdd');
        const addDocumentForm = document.getElementById('addDocumentForm');

        if (addDocumentBtn) {
            addDocumentBtn.addEventListener('click', () => this.showAddModal());
        }

        if (addFirstDocument) {
            addFirstDocument.addEventListener('click', (e) => {
                e.preventDefault();
                this.showAddModal();
            });
        }

        if (cancelAddBtn) {
            cancelAddBtn.addEventListener('click', () => this.hideAddModal());
        }

        if (addDocumentForm) {
            addDocumentForm.addEventListener('submit', (e) => this.handleAddDocument(e));
        }

        // Close modal when clicking outside
        if (addDocumentModal) {
            addDocumentModal.addEventListener('click', (e) => {
                if (e.target === addDocumentModal) {
                    this.hideAddModal();
                }
            });
        }

        // Close modal with X button
        const closeBtn = addDocumentModal?.querySelector('.close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.hideAddModal());
        }
    }

    async loadDocuments() {
        try {
            utils.showLoading('loading');
            utils.hideError('errorMessage');

            this.documents = await documentService.getAllDocuments();
            this.filteredDocuments = [...this.documents];
            this.renderDocuments();

        } catch (error) {
            utils.showError('errorMessage', error.message);
        } finally {
            utils.hideLoading('loading');
        }
    }

    filterDocuments() {
        const searchTerm = document.getElementById('searchInput')?.value.toLowerCase() || '';

        if (!searchTerm) {
            this.filteredDocuments = [...this.documents];
        } else {
            this.filteredDocuments = this.documents.filter(doc =>
                doc.name.toLowerCase().includes(searchTerm) ||
                doc.filetype.toLowerCase().includes(searchTerm)
            );
        }

        this.renderDocuments();
    }

    renderDocuments() {
        const documentsGrid = document.getElementById('documentsGrid');
        const noDocuments = document.getElementById('noDocuments');

        if (!documentsGrid || !noDocuments) return;

        if (this.filteredDocuments.length === 0) {
            documentsGrid.style.display = 'none';
            noDocuments.style.display = 'block';
            return;
        }

        noDocuments.style.display = 'none';
        documentsGrid.style.display = 'grid';

        documentsGrid.innerHTML = this.filteredDocuments.map(doc => this.createDocumentCard(doc)).join('');

        // Click event listeners to cards
        documentsGrid.querySelectorAll('.document-card').forEach(card => {
            card.addEventListener('click', () => {
                const documentId = card.dataset.documentId;
                this.navigateToDetail(documentId);
            });
        });
    }

    createDocumentCard(document) {
        const formattedSize = utils.formatFileSize(document.byteSize);
        const formattedDate = utils.formatDate(document.createdAt);

        return `
            <div class="document-card" data-document-id="${document.id}">
                <h3>${document.name}</h3>
                <div class="document-meta">
                    <span><span class="label">Type:</span> ${document.filetype.toUpperCase()}</span> <br />
                    <span><span class="label">Size:</span> ${formattedSize}</span> <br />
                    <span><span class="label">Created:</span> ${formattedDate}</span>
                </div>
            </div>
        `;
    }

    navigateToDetail(documentId) {
        window.location.href = `detail.html?id=${documentId}`;
    }

    showAddModal() {
        const modal = document.getElementById('addDocumentModal');
        if (modal) {
            modal.style.display = 'block';
            document.getElementById('documentName')?.focus();
        }
    }

    hideAddModal() {
        const modal = document.getElementById('addDocumentModal');
        if (modal) {
            modal.style.display = 'none';
            this.resetAddForm();
        }
    }

    resetAddForm() {
        const form = document.getElementById('addDocumentForm');
        if (form) {
            form.reset();
        }
    }

    async handleAddDocument(event) {
        event.preventDefault();

        const formData = new FormData(event.target);
        const documentData = {
            name: document.getElementById('documentName')?.value,
            filetype: document.getElementById('documentFiletype')?.value,
            byteSize: document.getElementById('documentByteSize')?.value
        };

        // Validation
        if (!documentData.name || !documentData.filetype || !documentData.byteSize) {
            utils.showError('errorMessage', 'Please fill in all required fields.');
            return;
        }

        if (parseInt(documentData.byteSize) < 0) {
            utils.showError('errorMessage', 'File size must be a positive number.');
            return;
        }

        try {
            utils.hideError('errorMessage');

            await documentService.createDocument(documentData);
            this.hideAddModal();
            this.loadDocuments();

            const successMessage = document.createElement('div');
            successMessage.className = 'success';
            successMessage.textContent = 'Document added successfully!';
            document.querySelector('main').prepend(successMessage);

            setTimeout(() => {
                successMessage.remove();
            }, 3000);

        } catch (error) {
            utils.showError('errorMessage', error.message);
        }
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new Dashboard();
});