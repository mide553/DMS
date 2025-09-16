# Document Management System
Document management system for archiving documents in a FileStore,
with automatic OCR (queue for OC-recognition),
automatic summary generation (using Gen-AI),
tagging and full text search (ElasticSearch).

## Project Architecture
<img width="1021" height="671" alt="image" src="https://github.com/user-attachments/assets/6e794cc4-5d17-4050-8b26-3a0a62ccabf8" />

## Use Cases
### 1. Upload document
* Automatically performs OCR
* Is indexed for full-text search in ElasticSearch
* a summary is automatically generated
### 3. Search for a document
* Full-text and fuzzy search in ElasticSearch
### 4. Manage documents
* Update, delete, metadata
### 5. Individually defined usecase
* tbd
