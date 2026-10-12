# AWS Setup Guide — Smart Job Tracker

## 1. Create S3 Bucket

```bash
aws s3api create-bucket \
  --bucket job-tracker-resumes \
  --region ap-south-1 \
  --create-bucket-configuration LocationConstraint=ap-south-1
```

Block public access (resumes are private, accessed via pre-signed URLs):
```bash
aws s3api put-public-access-block \
  --bucket job-tracker-resumes \
  --public-access-block-configuration \
    BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true
```

---

## 2. Create IAM Role for Lambda

Create `lambda-trust-policy.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": { "Service": "lambda.amazonaws.com" },
    "Action": "sts:AssumeRole"
  }]
}
```

```bash
aws iam create-role \
  --role-name lambda-job-tracker-role \
  --assume-role-policy-document file://lambda-trust-policy.json

aws iam attach-role-policy \
  --role-name lambda-job-tracker-role \
  --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole

aws iam attach-role-policy \
  --role-name lambda-job-tracker-role \
  --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess
```

---

## 3. Deploy Lambda Function

```bash
cd lambda-scorer
zip lambda.zip handler.py

aws lambda create-function \
  --function-name job-tracker-scorer \
  --runtime python3.11 \
  --role arn:aws:iam::YOUR_ACCOUNT_ID:role/lambda-job-tracker-role \
  --handler handler.lambda_handler \
  --zip-file fileb://lambda.zip \
  --timeout 30 \
  --environment Variables="{
    AI_SERVICE_URL=https://your-fastapi-ec2-or-url.com,
    S3_BUCKET=job-tracker-resumes
  }"
```

Update function code after changes:
```bash
zip lambda.zip handler.py
aws lambda update-function-code \
  --function-name job-tracker-scorer \
  --zip-file fileb://lambda.zip
```

---

## 4. Add S3 Trigger to Lambda

```bash
aws lambda add-permission \
  --function-name job-tracker-scorer \
  --statement-id s3-trigger \
  --action lambda:InvokeFunction \
  --principal s3.amazonaws.com \
  --source-arn arn:aws:s3:::job-tracker-resumes

aws s3api put-bucket-notification-configuration \
  --bucket job-tracker-resumes \
  --notification-configuration '{
    "LambdaFunctionConfigurations": [{
      "LambdaFunctionArn": "arn:aws:lambda:ap-south-1:YOUR_ACCOUNT_ID:function:job-tracker-scorer",
      "Events": ["s3:ObjectCreated:*"],
      "Filter": {
        "Key": { "FilterRules": [{"Name": "prefix", "Value": "resumes/"}] }
      }
    }]
  }'
```

---

## 5. EC2 for .NET API + FastAPI

Launch EC2 (t3.small or t3.medium recommended):
```bash
aws ec2 run-instances \
  --image-id ami-0f5ee92e2d63afc18 \
  --instance-type t3.small \
  --key-name your-key-pair \
  --security-group-ids sg-xxxxxxxx \
  --region ap-south-1
```

On the EC2 instance:
```bash
# Install Docker
sudo apt update && sudo apt install docker.io docker-compose -y
sudo systemctl start docker

# Clone your repo and run
git clone https://github.com/YOUR_USERNAME/smart-job-tracker.git
cd smart-job-tracker
OPENAI_API_KEY=sk-... docker-compose up -d
```

---

## 6. IAM User for .NET API (S3 access)

```bash
aws iam create-user --user-name job-tracker-api

aws iam attach-user-policy \
  --user-name job-tracker-api \
  --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess

aws iam create-access-key --user-name job-tracker-api
# Save the AccessKeyId and SecretAccessKey
# Set as environment variables on your EC2:
# AWS_ACCESS_KEY_ID=...
# AWS_SECRET_ACCESS_KEY=...
```

---

## Architecture Summary

```
User → .NET API (EC2) → S3 (resume upload)
                              ↓ S3 event
                         Lambda (scorer)
                              ↓ HTTP
                         FastAPI AI service (EC2)
                              ↓ OpenAI API
                         Score JSON → S3 (scores/)
```
