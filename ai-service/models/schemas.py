from pydantic import BaseModel
from typing import List


class ScoreRequest(BaseModel):
    resume_text: str
    job_description: str


class ScoreResponse(BaseModel):
    score: int                    # 0-100
    feedback: str
    matched_skills: List[str]
    missing_skills: List[str]
    suggestions: List[str]
