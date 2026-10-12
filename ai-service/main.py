from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from routers import score

app = FastAPI(
    title="Job Tracker AI Service",
    description="AI-powered resume-to-job match scoring using OpenAI",
    version="1.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(score.router)


@app.get("/health")
def health():
    return {"status": "ok", "service": "ai-scorer"}
