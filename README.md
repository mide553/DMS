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

**Current Access Points:**
- **Web UI:** http://localhost:8080 (Login required)
- **REST API:** http://localhost:5000/api/documents (Auth required)
- **Database:** PostgreSQL on localhost:5432
- **Swagger:** http://localhost:5000/swagger/index.html
- **RabbitMQ:** http://localhost:15672/

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


4. **Access the RabbitMQ:**
   - Open http://localhost:15672/ in your browser
   - Enter credentials


## Project Architecture
<img width="1021" height="671" alt="image" src="https://github.com/user-attachments/assets/6e794cc4-5d17-4050-8b26-3a0a62ccabf8" />


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

### 3. Real-time Document Search and Management
**Actor**: Authenticated Knowledge Worker
**Current Implementation**:
- Access web dashboard showing only user's own documents in grid layout
- Use real-time search to filter personal documents by name or file type
- Click on documents to view detailed information on separate detail page
- Edit document metadata through modal forms
- Delete documents with confirmation dialogs
- All changes immediately reflected in PostgreSQL database
- User cannot access or modify documents belonging to other users

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

