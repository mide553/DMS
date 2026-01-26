# Document Management System
Document management system for archiving documents in a FileStore,
with automatic OCR (queue for OC-recognition),
automatic summary generation (using Gen-AI),
tagging and full text search (ElasticSearch).

## Project Status

### Sprint 1: Project-Setup, REST API, DAL
- Project setup and architecture
- REST API implementation
- Data Access Layer (DAL)
- PostgreSQL database integration

### Sprint 2: Web-UI
- Nginx-based web server service
- Dashboard and document detail pages
- Complete web UI with responsive design
- API communication with JavaScript
- Extended docker-compose with web container

### Sprint 3: Queuing
- Add RabbitMQ service to docker-compose.yml
- Integrate queues into the REST server
- On document upload, send message to RabbitMQ
- Implement exception handling and logging

### Sprint 4: Workers, MinIO, OCR
- MinIO object storage integration
- OCR worker implementation with Tesseract
- Document processing pipeline
- File storage and retrieval

### Sprint 5: GenAI
- AI-powered document summarization
- GenAI worker for automated summaries
- Integration with Gemini API
- Background processing of documents

### Sprint 6: ELK, Use Cases
- ElasticSearch integration (planned)
- Logging and monitoring
- Extended use cases documentation
- System architecture diagrams
- **User Authentication & Authorization use case**:
  - JWT-based authentication system
  - User registration and login
  - Protected API endpoints
  - User-specific document management
  - Secure password hashing with SHA256
  - Session management with auto-redirect
  - Swagger UI with JWT authorization support

### Sprint 7: Integration-Test, Batch-Processing, Finalization
- Integration tests for document upload use case
- **Batch Processing Service**:
  - Scheduled service for processing daily XML access logs
  - Reads XML files from external systems
  - Updates document access statistics in database
  - Configurable schedule (default: daily at 01:00 AM)
  - Automatic file archiving after processing
  - Configurable input folder and filename patterns
  - Database schema extended with AccessCount and LastAccessDate fields

**Current Access Points:**
- **Web UI:** http://localhost:8080 (Login required)
- **REST API:** http://localhost:5000/api/documents (Auth required)
- **Swagger:** http://localhost:5000/swagger/index.html (Interactive API documentation with JWT support)
- **PostgreSQL Database:** localhost:5432 (credentials in .env)
- **RabbitMQ Management:** http://localhost:15672 (username/password from .env)
- **MinIO Console:** http://localhost:9001 (Object storage management)
- **ElasticSearch:** http://localhost:9200 (Full-text search engine)
- **Kibana:** http://localhost:5601 (ElasticSearch visualization)

## Quick Start

1. **Start the application:**
   ```bash
   docker-compose up -d
   ```
      **In deployment:**
      ```bash
      docker-compose -f docker-compose.yml up -d
      ```
   
2. **Update Database:**
   ```bash
   cd PaperlessREST
   dotnet ef database update
   ```

3. **Access the Web UI:**
   - Open http://localhost:8080 in your browser
   - **Register** a new account or **login** with existing credentials
   - Use the dashboard to manage your documents
   - Add, view, edit, and delete your documents


3. **API Testing:**
   
   First, register and login to get your JWT token:
   ```bash
   # Register a new user
   curl -X POST http://localhost:5000/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{"username":"testuser","email":"test@example.com","password":"SecurePass123"}'
   
   # Login to get JWT token
   curl -X POST http://localhost:5000/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"username":"testuser","password":"SecurePass123"}'
   ```
   
   Use the returned token for authenticated requests:
   ```bash
   # Get all your documents (replace YOUR_TOKEN_HERE with actual token)
   curl http://localhost:5000/api/documents \
     -H "Authorization: Bearer YOUR_TOKEN_HERE"
   
   # Add a document
   curl -X POST http://localhost:5000/api/documents \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_TOKEN_HERE" \
     -d '{"name":"test.pdf","filetype":"pdf","byteSize":1024,"createdAt":"2025-09-27T10:00:00Z","updatedAt":"2025-09-27T10:00:00Z"}'
   ```
   
   Or use Swagger UI with JWT authorization:
   http://localhost:5000/swagger/index.html
   - Click "Authorize" button
   - Enter: `Bearer YOUR_TOKEN_HERE`
   - Test all endpoints interactively


4. **Access RabbitMQ Management Console:**
   - Open http://localhost:15672/ in your browser
   - Enter credentials from .env file (RABBITMQ_USER / RABBITMQ_PASSWORD)
   - View queues, messages, and worker connections


5. **Access MinIO Object Storage:**
   - Open http://localhost:9001 in your browser
   - Login with credentials from .env file (MINIO_ROOT_USER / MINIO_ROOT_PASSWORD)
   - View uploaded document files in buckets
   - Monitor storage usage and performance


6. **Access ElasticSearch:**
   - ElasticSearch API: http://localhost:9200


7. **Access Kibana Dashboard:**
   - Open http://localhost:5601 in your browser
   - Explore document search analytics


8. **Testing the Batch Process:**
   - Copy sample XML files to the batch input volume:
   ```bash
   docker cp PaperlessServices/BatchProcessor/access-log-2026-01-12.xml BatchProcessor:/data/input/
   ```
  - Check the BatchProcessor logs:
   ```bash
   docker logs BatchProcessor
   ```
  - Query the database to verify access statistics:
   ```sql
   SELECT "Id", "FileName", "AccessCount", "LastAccessDate" FROM "Documents" WHERE "AccessCount" > 0;
   ```

## Project Architecture
<img width="1021" height="671" alt="Project Architecture" src="https://github.com/user-attachments/assets/6e794cc4-5d17-4050-8b26-3a0a62ccabf8" />

## Responsibility-Layers
<img width="922" height="961" alt="Responsibility-Layers drawio" src="https://github.com/user-attachments/assets/ea7f6872-e7f2-4a79-8460-cef7c64225ce" />


## Use Cases

### 1. User Registration and Authentication
**Actor**: New User / Returning User
**Current Implementation**:
- User visits login page (http://localhost:8080/login.html)
- New users register with username, email, and password
- System securely hashes passwords using SHA256
- Upon registration, user is redirected to login page
- Existing users login with username and password
- System generates JWT token valid for 7 days
- Token is stored in browser's localStorage for session management
- All subsequent API requests include JWT token for authentication

### 2. Document Upload with Queue Processing
**Actor**: Authenticated Office Employee
**Current Implementation**:
- User must be logged in to access the dashboard
- User uploads document through web interface (http://localhost:8080)
- System stores document information in PostgreSQL database with user ownership
- Document appears immediately on the user's personal dashboard
- RabbitMQ automatically queues document for background OCR processing
- OCR worker processes documents asynchronously (currently stub implementation)
- Each user can only see their own uploaded documents

### 3. Document Management and Dashboard
**Actor**: Authenticated Knowledge Worker
**Current Implementation**:
- Access web dashboard showing only user's own documents in grid layout
- Real-time client-side filtering by document name or file type
- Click on documents to view detailed information on separate detail page
- Edit document metadata through modal forms
- Delete documents with confirmation dialogs
- All changes immediately reflected in PostgreSQL database
- Role-based access control - users can only manage their own documents

### 4. REST API Integration for External Systems
**Actor**: Developer
**Current Implementation**:
- Programmatic access via REST API (http://localhost:5000/api/documents)
- Authentication required: Login to receive JWT token
- Token must be included in Authorization header: `Bearer {token}`
- Full CRUD operations: GET, POST, PUT, DELETE (user-specific)
- JSON-based data exchange with proper HTTP status codes
- Swagger documentation available with JWT authorization support (http://localhost:5000/swagger/index.html)
- API enforces user-specific access - users can only manage their own documents

### 5. Automated Batch Processing of Access Logs
**Actor**: System Administrator / External Systems
**Current Implementation**:
- External systems generate daily XML files containing document access statistics
- XML files are placed in configured input folder (default: `/data/input`)
- BatchProcessor service runs on schedule (default: daily at 01:00 AM)
- Service reads all XML files matching pattern `access-log-*.xml`
- Each document's access count is updated in PostgreSQL database
- Processed files are automatically archived with timestamp to prevent reprocessing
- Configuration allows customization of:
  - Schedule (via cron expression)
  - Input folder path
  - Filename pattern
  - Archive folder path
- Database schema includes:
  - `AccessCount`: Cumulative number of document accesses
  - `LastAccessDate`: Timestamp of last access statistics update

### 6. OCR Processing and Text Extraction
**Actor**: System / Background Worker
**Current Implementation**:
- When a document is uploaded, a message is sent to RabbitMQ queue
- OcrWorker service listens to the queue for new document notifications
- Worker retrieves document from MinIO object storage
- Tesseract OCR extracts text content from document images
- Extracted text is indexed in ElasticSearch for full-text search capabilities
- Process runs asynchronously without blocking user interactions
- Supports multiple document formats and languages

### 7. AI-Powered Document Summarization
**Actor**: System / Background Worker
**Current Implementation**:
- After OCR processing, document is queued for AI summarization
- GenAIWorker service consumes messages from RabbitMQ
- Worker sends document text to Gemini API for analysis
- AI generates intelligent summary of document content
- Summary is stored in PostgreSQL database linked to document
- Users can view AI-generated summaries in document details
- Configurable via GEMINI_API_KEY environment variable

### 8. Advanced Full-Text Search with ElasticSearch
**Actor**: Authenticated User
**Current Implementation**:
- ElasticSearch indexes OCR-extracted text and document metadata
- Search inside document content (not just filenames)
- Query across multiple documents for specific words or phrases
- Advanced filtering by file type, date, tags, and other criteria
- Search results ranked by relevance score
- Supports fuzzy matching and complex search queries
- Kibana dashboard available for search analytics and visualization (http://localhost:5601)
- Complements basic dashboard filtering with deep content search


