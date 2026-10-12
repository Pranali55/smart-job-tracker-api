using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace JobTracker.API.Services;

public class S3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3Service(IConfiguration config)
    {
        var region = RegionEndpoint.GetBySystemName(config["AWS:Region"] ?? "ap-south-1");
        // Uses AWS credentials from environment variables or IAM role:
        // AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY (or EC2 instance role)
        _s3Client = new AmazonS3Client(region);
        _bucketName = config["AWS:BucketName"]!;
    }

    public async Task<string> UploadResumeAsync(IFormFile file, int userId)
    {
        var extension = Path.GetExtension(file.FileName);
        var key = $"resumes/{userId}/{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
            Metadata = { ["user-id"] = userId.ToString() }
        };

        await _s3Client.PutObjectAsync(request);
        return key;
    }

    public string GetPreSignedUrl(string key, int expiryMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public async Task DeleteResumeAsync(string key)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, key);
    }
}
