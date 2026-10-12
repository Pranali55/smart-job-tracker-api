"""
AWS Lambda function — triggered by S3 PutObject events on the resumes/ prefix.

Flow:
  1. S3 upload event fires (user uploads resume via .NET API)
  2. Lambda reads the S3 object key and fetches metadata
  3. Calls FastAPI AI scorer with resume text + job description
  4. Writes score result back to a separate S3 key (scores/{userId}/{filename}.json)

Deploy:
  zip lambda.zip handler.py
  aws lambda create-function \
    --function-name job-tracker-scorer \
    --runtime python3.11 \
    --role arn:aws:iam::YOUR_ACCOUNT:role/lambda-s3-role \
    --handler handler.lambda_handler \
    --zip-file fileb://lambda.zip \
    --environment Variables="{AI_SERVICE_URL=https://your-fastapi-url,S3_BUCKET=job-tracker-resumes}"
"""

import json
import os
import urllib.request
import urllib.parse
import boto3

s3 = boto3.client("s3")
AI_SERVICE_URL = os.environ.get("AI_SERVICE_URL", "http://localhost:8000")
S3_BUCKET = os.environ.get("S3_BUCKET", "job-tracker-resumes")


def lambda_handler(event, context):
    print("Event received:", json.dumps(event))

    for record in event.get("Records", []):
        bucket = record["s3"]["bucket"]["name"]
        key = urllib.parse.unquote_plus(record["s3"]["object"]["key"])

        print(f"Processing: s3://{bucket}/{key}")

        # Only process resume uploads
        if not key.startswith("resumes/"):
            print("Skipping non-resume key:", key)
            continue

        try:
            result = process_resume(bucket, key)
            print("Scoring result:", json.dumps(result))

            # Save score JSON to S3 alongside the resume
            score_key = key.replace("resumes/", "scores/") + ".json"
            s3.put_object(
                Bucket=bucket,
                Key=score_key,
                Body=json.dumps(result),
                ContentType="application/json",
            )
            print(f"Score saved to s3://{bucket}/{score_key}")

        except Exception as e:
            print(f"Error processing {key}: {e}")
            raise

    return {"statusCode": 200, "body": "Done"}


def process_resume(bucket: str, key: str) -> dict:
    # Fetch metadata stored on the S3 object (set by .NET API on upload)
    head = s3.head_object(Bucket=bucket, Key=key)
    metadata = head.get("Metadata", {})
    job_description = metadata.get("job-description", "")
    user_id = metadata.get("user-id", "unknown")

    # Read the resume file content (assumes text/plain or extracted text)
    obj = s3.get_object(Bucket=bucket, Key=key)
    resume_text = obj["Body"].read().decode("utf-8", errors="ignore")

    if not resume_text.strip() or not job_description.strip():
        return {
            "score": 0,
            "feedback": "Could not score — missing resume text or job description.",
            "matched_skills": [],
            "missing_skills": [],
            "suggestions": [],
            "user_id": user_id,
            "s3_key": key,
        }

    # Call FastAPI scorer
    payload = json.dumps({
        "resume_text": resume_text,
        "job_description": job_description,
    }).encode("utf-8")

    req = urllib.request.Request(
        f"{AI_SERVICE_URL}/score",
        data=payload,
        headers={"Content-Type": "application/json"},
        method="POST",
    )

    with urllib.request.urlopen(req, timeout=25) as resp:
        score_data = json.loads(resp.read().decode())

    score_data["user_id"] = user_id
    score_data["s3_key"] = key
    return score_data
