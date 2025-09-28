# Document Management System
Document management system for archiving documents in a FileStore,
with automatic OCR (queue for OC-recognition),
automatic summary generation (using Gen-AI),
tagging and full text search (ElasticSearch).

## Project Status

### Sprint 1: REST API
- Project-Setup
- REST API
- DAL

### Sprint 2: Web UI
- Nginx-based web server service
- Dashboard and document detail pages
- Complete web UI with responsive design
- API communication with JavaScript
- Extended docker-compose with web container

**Current Access Points:**
- **Web UI:** http://localhost:8080
- **REST API:** http://localhost:5000/api/documents
- **Database:** PostgreSQL on localhost:5432

## Quick Start

1. **Start the application:**
   ```bash
   docker-compose up -d
   ```
   
2. **Update Database:**
   ```bash
   cd PaperlessREST
   dotnet ef database update
   ```

2. **Access the Web UI:**
   - Open http://localhost:8080 in your browser
   - Use the dashboard to manage documents
   - Add, view, edit, and delete documents through the web interface

3. **API Testing:**
   ```bash
   # Get all documents
   curl http://localhost:5000/api/documents
   
   # Add a document
   curl -X POST http://localhost:5000/api/documents \
     -H "Content-Type: application/json" \
     -d '{"name":"test.pdf","filetype":"pdf","byteSize":1024,"createdAt":"2025-09-27T10:00:00Z","updatedAt":"2025-09-27T10:00:00Z"}'
   ```
   or using Swagger:
   http://localhost:5000/swagger/index.html

## Project Architecture
<img width="1021" height="671" alt="image" src="https://github.com/user-attachments/assets/6e794cc4-5d17-4050-8b26-3a0a62ccabf8" />

## Use Cases
### 1. Upload document
* Automatically performs OCR
* Is indexed for full-text search in ElasticSearch
* a summary is automatically generated
### 2. Search for a document
* Full-text and fuzzy search in ElasticSearch
### 3. Manage documents
* Update, delete, metadata through Web UI
### 4. Web-based Document Management
* Dashboard view of all documents
* Individual document detail pages
* Search functionality
### 5. Individually defined usecase
* tbd
