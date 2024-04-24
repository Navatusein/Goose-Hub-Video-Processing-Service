using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace VideoProcessingService.Services
{
    /// <summary>
    /// Service for work with Minio
    /// </summary>
    public class MinioService
    {
        private static Serilog.ILogger Logger => Serilog.Log.ForContext<MinioService>();

        private readonly IMinioClient _minioClient;

        private readonly string _contentBucket;

        /// <summary>
        /// Constructor
        /// </summary>
        public MinioService(IConfiguration config)
        {
            var endpoint = config.GetSection("MinIO:Endpoint").Get<string>();
            var useSsl = config.GetSection("MinIO:UseSSL").Get<bool>();
            var region = config.GetSection("MinIO:Region").Get<string>();
            var accessKey = config.GetSection("MinIO:AccessKey").Get<string>();
            var secretKey = config.GetSection("MinIO:SecretKey").Get<string>();

            _contentBucket = config.GetSection("MinIO:ContentBucket").Get<string>()!;

            Logger.Information($"Config: {endpoint}");

            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithRegion(region)
                .WithSSL(useSsl)
                .Build();
        }

        /// <summary>
        /// Upload file to MinIO
        /// </summary>
        public async Task<string> UploadContent(string filePath, string extension, string contentType)
        {
            string objectName = Guid.NewGuid().ToString() + extension;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var putObjectArgs = new PutObjectArgs()
               .WithBucket(_contentBucket)
               .WithObject(objectName)
               .WithStreamData(stream)
               .WithObjectSize(stream.Length)
               .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(true);

            return objectName;
        }
    }
}
