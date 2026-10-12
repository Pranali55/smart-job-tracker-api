# Smart Job Application Tracker

A full-stack AI-powered job application tracking system combining:
- **ASP.NET Core Web API** — backend, auth, job/application management
- **Python FastAPI** — AI microservice (resume-to-job match scoring via OpenAI)
- **AWS Lambda** — serverless scorer triggered on S3 resume upload
- **AWS S3** — resume file storage

---

## Architecture

```
User/Frontend
     |
     v
ASP.NET Core Web API (port 5000)
     |-- JWT Auth
     |-- Jobs CRUD
     |-- Applications CRUD
     |-- Resume Upload → AWS S3
     |
     v
AWS S3 (resume storage)
     |
     v (S3 event trigger)
AWS Lambda (lambda-scorer/)
     |
     v
FastAPI AI Service (port 8000)
     |-- POST /score  →  OpenAI API
     |-- Returns match score + feedback
```

---

## Project Structure

```
smart-job-tracker/
├── README.md
├── docker-compose.yml
│
├── backend-dotnet/                  # ASP.NET Core Web API
│   ├── JobTracker.API.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── JobsController.cs
│   │   ├── ApplicationsController.cs
│   │   └── ResumeController.cs
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Job.cs
│   │   └── Application.cs
│   ├── DTOs/
│   │   ├── AuthDTOs.cs
│   │   ├── JobDTOs.cs
│   │   └── ApplicationDTOs.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── S3Service.cs
│   │   └── AiScorerService.cs
│   └── Middleware/
│       └── JwtMiddleware.cs
│
├── ai-service/                      # Python FastAPI AI microservice
│   ├── main.py
│   ├── requirements.txt
│   ├── Dockerfile
│   ├── routers/
│   │   └── score.py
│   ├── models/
│   │   └── schemas.py
│   └── services/
│       └── openai_service.py
│
├── lambda-scorer/                   # AWS Lambda function
│   ├── handler.py
│   └── requirements.txt
│
└── infrastructure/                  # AWS setup notes
    └── aws-setup.md
```

---

## Quick Start

### 1. Run with Docker Compose
```bash
docker-compose up --build
```

### 2. Manual setup

**Backend (.NET):**
```bash
cd backend-dotnet
dotnet restore
dotnet ef database update
dotnet run
# API running at http://localhost:5000
```

**AI Service (Python):**
```bash
cd ai-service
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
# AI service running at http://localhost:8000
```

---

## Environment Variables

### backend-dotnet/appsettings.json
```json
{
  "Jwt": { "Secret": "your-secret-key-min-32-chars" },
  "AWS": { "BucketName": "job-tracker-resumes", "Region": "ap-south-1" },
  "AiService": { "BaseUrl": "http://localhost:8000" }
}
```

### ai-service/.env
```
OPENAI_API_KEY=sk-...
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/auth/register | Register user |
| POST | /api/auth/login | Login, get JWT |
| GET | /api/jobs | List all jobs |
| POST | /api/jobs | Add a job |
| GET | /api/applications | List applications |
| POST | /api/applications | Apply to a job |
| POST | /api/resume/upload | Upload resume to S3 |
| POST | /api/resume/score | Get AI match score |
