import os
import json
from openai import OpenAI
from models.schemas import ScoreRequest, ScoreResponse

client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))


def score_resume(req: ScoreRequest) -> ScoreResponse:
    prompt = f"""
You are an expert technical recruiter and resume analyst.

Analyze how well the resume matches the job description below.

RESUME:
{req.resume_text}

JOB DESCRIPTION:
{req.job_description}

Respond ONLY with valid JSON in this exact format:
{{
  "score": <integer 0-100>,
  "feedback": "<2-3 sentence overall summary>",
  "matched_skills": ["skill1", "skill2"],
  "missing_skills": ["skill1", "skill2"],
  "suggestions": ["suggestion1", "suggestion2"]
}}

Scoring guide:
- 80-100: Strong match, most skills present
- 60-79: Good match, some gaps
- 40-59: Partial match, significant gaps
- 0-39: Weak match
"""

    response = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.3,
        max_tokens=600,
    )

    raw = response.choices[0].message.content.strip()

    # Strip markdown code fences if present
    if raw.startswith("```"):
        raw = raw.split("```")[1]
        if raw.startswith("json"):
            raw = raw[4:]

    data = json.loads(raw)

    return ScoreResponse(
        score=int(data["score"]),
        feedback=data["feedback"],
        matched_skills=data.get("matched_skills", []),
        missing_skills=data.get("missing_skills", []),
        suggestions=data.get("suggestions", []),
    )
