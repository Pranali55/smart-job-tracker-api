from fastapi import APIRouter, HTTPException
from models.schemas import ScoreRequest, ScoreResponse
from services.openai_service import score_resume

router = APIRouter()


@router.post("/score", response_model=ScoreResponse)
def score(req: ScoreRequest):
    """
    Score how well a resume matches a job description.
    Returns a 0-100 score, matched/missing skills, and improvement suggestions.
    """
    if not req.resume_text.strip():
        raise HTTPException(status_code=400, detail="resume_text cannot be empty")
    if not req.job_description.strip():
        raise HTTPException(status_code=400, detail="job_description cannot be empty")

    try:
        return score_resume(req)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Scoring failed: {str(e)}")
