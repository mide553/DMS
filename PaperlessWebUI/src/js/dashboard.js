class Dashboard {
    constructor() {
        this.documents = [];
        this.filteredDocuments = [];
        this.selectedFile = null;
        this.init();
    }

    init() {
        this.bindEvents();
        this.loadDocuments();
    }

    bindEvents() {
        const searchInput = document.getElementById('searchInput');
        const searchBtn = document.getElementById('searchBtn');

        if (searchInput && searchBtn) {
            searchInput.addEventListener('input', () => this.filterDocuments());
            searchBtn.addEventListener('click', () => this.filterDocuments());
        }

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

        if (addDocumentModal) {
            addDocumentModal.addEventListener('click', (e) => {
                if (e.target === addDocumentModal) {
                    this.hideAddModal();
                }
            });
        }

        const closeBtn = addDocumentModal?.querySelector('.close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.hideAddModal());
        }

        this.setupFileUpload();
    }

    setupFileUpload() {
        const uploadArea = document.getElementById('uploadArea');
        const fileInput = document.getElementById('fileInput');
        const removeFileBtn = document.getElementById('removeFile');

        if (uploadArea && fileInput) {
            uploadArea.addEventListener('click', () => fileInput.click());

            uploadArea.addEventListener('dragover', (e) => {
                e.preventDefault();
                uploadArea.classList.add('drag-over');
            });

            uploadArea.addEventListener('dragleave', () => {
                uploadArea.classList.remove('drag-over');
            });

            uploadArea.addEventListener('drop', (e) => {
                e.preventDefault();
                uploadArea.classList.remove('drag-over');

                const files = e.dataTransfer.files;
                if (files.length > 0) {
                    this.handleFileSelect(files[0]);
                }
            });

            fileInput.addEventListener('change', (e) => {
                const files = e.target.files;
                if (files.length > 0) {
                    this.handleFileSelect(files[0]);
                }
            });
        }

        if (removeFileBtn) {
            removeFileBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.removeFile();
            });
        }
    }

    handleFileSelect(file) {
        const maxSize = 5 * 1024 * 1024; // 5MB
        const allowedTypes = ['application/pdf'];

        utils.hideError('uploadError');

        if (file.size > maxSize) {
            utils.showError('uploadError', 'File size must be less than 5MB');
            return;
        }

        if (!allowedTypes.includes(file.type)) {
            utils.showError('uploadError', 'Only PDF files are supported.');
            return;
        }

        this.selectedFile = file;
        this.showFilePreview(file);
    }

    showFilePreview(file) {
        const uploadPlaceholder = document.getElementById('uploadPlaceholder');
        const filePreview = document.getElementById('filePreview');
        const fileName = document.getElementById('fileName');
        const fileSize = document.getElementById('fileSize');
        const uploadBtn = document.getElementById('uploadBtn');

        if (uploadPlaceholder && filePreview && fileName && fileSize) {
            uploadPlaceholder.style.display = 'none';
            filePreview.style.display = 'block';
            fileName.textContent = file.name;
            fileSize.textContent = utils.formatFileSize(file.size);

            if (uploadBtn) {
                uploadBtn.disabled = false;
            }
        }
    }

    removeFile() {
        this.selectedFile = null;

        const uploadPlaceholder = document.getElementById('uploadPlaceholder');
        const filePreview = document.getElementById('filePreview');
        const fileInput = document.getElementById('fileInput');
        const uploadBtn = document.getElementById('uploadBtn');

        if (uploadPlaceholder && filePreview) {
            uploadPlaceholder.style.display = 'block';
            filePreview.style.display = 'none';
        }

        if (fileInput) {
            fileInput.value = '';
        }

        if (uploadBtn) {
            uploadBtn.disabled = true;
        }

        utils.hideError('uploadError');
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
            this.filteredDocuments = this.documents.filter(doc => {
                const fileName = (doc.fileName || doc.FileName || doc.name || '').toLowerCase();
                let fileType = (doc.filetype || doc.FileType || '').toLowerCase();

                if (!fileType && fileName.includes('.')) {
                    fileType = fileName.split('.').pop().toLowerCase();
                }

                return fileName.includes(searchTerm) || fileType.includes(searchTerm);
            });
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
        // Handle both backend (PascalCase) and frontend (camelCase) property names
        const fileName = document.fileName || document.FileName || document.name || 'Unknown';
        const fileSize = document.byteSize || document.ByteSize || 0;
        const createdDate = document.createdAt || document.lastModified || document.LastModified || new Date().toISOString();

        // Extract file type from filename if not provided
        let fileType = document.filetype || document.FileType || '';
        if (!fileType && fileName.includes('.')) {
            fileType = fileName.split('.').pop();
        }

        const formattedSize = utils.formatFileSize(fileSize);
        const formattedDate = utils.formatDate(createdDate);

        return `
            <div class="document-card" data-document-id="${document.id}">
                <h3>${fileName}</h3>
                <div class="document-meta">
                    <span><span class="label">Type:</span> ${fileType.toUpperCase()}</span> <br />
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
            this.removeFile();
        }
    }

    hideAddModal() {
        const modal = document.getElementById('addDocumentModal');
        if (modal) {
            modal.style.display = 'none';
            this.removeFile();
        }
    }

    async handleAddDocument(event) {
        event.preventDefault();

        if (!this.selectedFile) {
            utils.showError('uploadError', 'Please select a file to upload');
            return;
        }

        const formData = new FormData();
        formData.append('file', this.selectedFile);

        try {
            utils.hideError('uploadError');
            const uploadBtn = document.getElementById('uploadBtn');
            if (uploadBtn) {
                uploadBtn.disabled = true;
                uploadBtn.textContent = 'Uploading...';
            }

            const response = await fetch('/api/documents/upload', {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                const errorText = await response.text();
                const error = new Error(errorText || 'Upload failed');
                utils.showError('uploadError', error.message);
                const uploadBtn = document.getElementById('uploadBtn');
                if (uploadBtn) {
                    uploadBtn.disabled = false;
                    uploadBtn.textContent = 'Upload';
                }
                return;
            }

            this.hideAddModal();
            this.loadDocuments();

            const successMessage = document.createElement('div');
            successMessage.className = 'success';
            successMessage.textContent = 'Document uploaded successfully and queued for OCR processing!';
            document.querySelector('main').prepend(successMessage);

            setTimeout(() => successMessage.remove(), 3000);

        } catch (error) {
            utils.showError('uploadError', error.message || 'An error occurred');
            const uploadBtn = document.getElementById('uploadBtn');
            if (uploadBtn) {
                uploadBtn.disabled = false;
                uploadBtn.textContent = 'Upload';
            }
        }
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new Dashboard();
});