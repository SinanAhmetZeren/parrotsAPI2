// using Azure.Storage.Blobs;
// using Azure.Storage.Blobs.Models;
using Amazon.S3;
using Amazon.S3.Model;
/*
namespace ParrotsAPI2.Services.Blob
{
    public class BlobService : IBlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        public BlobService(string connectionString, string containerName)
        {
            _connectionString = connectionString;
            _containerName = containerName;
        }
        public async Task<string> UploadAsync(Stream fileStream, string fileName, string? contentType = null)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);

            var blobHttpHeaders = new BlobHttpHeaders();
            if (!string.IsNullOrEmpty(contentType))
            {
                blobHttpHeaders.ContentType = contentType;
            }

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return blobClient.Uri.ToString();
        }
        public async Task<bool> DeleteAsync(string fileName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            return await blobClient.DeleteIfExistsAsync();
        }
    }
}
*/

namespace ParrotsAPI2.Services.Blob
{
    public class BlobService : IBlobService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _serviceUrl;

        public BlobService(IConfiguration config)
        {
            var accessKey = config["S3:AccessKey"];
            var secretKey = config["S3:SecretKey"];
            _serviceUrl = config["S3:ServiceUrl"];
            _bucketName = config["S3:BucketName"];

            var s3Config = new AmazonS3Config
            {
                ServiceURL = _serviceUrl,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string? contentType = null)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request);

            return $"{_serviceUrl}/{_bucketName}/{fileName}";
        }

        public async Task<bool> DeleteAsync(string fileName)
        {
            var response = await _s3Client.DeleteObjectAsync(_bucketName, fileName);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
    }
}