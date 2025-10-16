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

### Sprint 3: Queuing
- Add RabbitMQ service to docker-compose.yml
- Integrate queues into the REST server
- On document upload, send message to RabbitMQ -> handled by empty OCR worker
- Implement exception handling and logging

**Current Access Points:**
- **Web UI:** http://localhost:8080
- **REST API:** http://localhost:5000/api/documents
- **Database:** PostgreSQL on localhost:5432
- **Swagger:** http://localhost:5000/swagger/index.html
- **RabbitMQ:** http://localhost:15672/

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


4. **Access the RabbitMQ:**
   - Open http://localhost:15672/ in your browser
   - Enter credentials


## Project Architecture
<img width="1021" height="671" alt="image" src="https://github.com/user-attachments/assets/6e794cc4-5d17-4050-8b26-3a0a62ccabf8" />


## Use Cases

### 1. Document Upload with Queue Processing
**Actor**: Office Employee
**Current Implementation**:
- User uploads document metadata through web interface (http://localhost:8080)
- System stores document information in PostgreSQL database
- Document appears immediately on the dashboard
- RabbitMQ automatically queues document for background OCR processing
- OCR worker processes documents asynchronously (currently stub implementation)

### 2. Real-time Document Search and Management
**Actor**: Knowledge Worker
**Current Implementation**:
- Access web dashboard showing all documents in grid layout
- Use real-time search to filter documents by name or file type
- Click on documents to view detailed information on separate detail page
- Edit document metadata through modal forms
- Delete documents with confirmation dialogs
- All changes immediately reflected in PostgreSQL database

### 3. REST API Integration for External Systems
**Actor**: Developer
**Current Implementation**:
- Programmatic access via REST API (http://localhost:5000/api/documents)
- Full CRUD operations: GET, POST, PUT, DELETE
- JSON-based data exchange with proper HTTP status codes
- Swagger documentation available (http://localhost:5000/swagger/index.html)
