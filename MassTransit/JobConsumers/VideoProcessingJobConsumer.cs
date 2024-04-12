using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using VideoProcessingService.MassTransit.Events;
using VideoProcessingService.Models;
using VideoProcessingService.Services;

namespace VideoProcessingService.MassTransit.JobConsumers
{
    /// <summary>
    /// Consumer for VideoProcessingEvent
    /// </summary>
    public class VideoProcessingJobConsumer : IConsumer<VideoProcessingEvent>
    {
        private static Serilog.ILogger Logger => Serilog.Log.ForContext<VideoProcessingJobConsumer>();

        private readonly IPublishEndpoint _publishEndpoint;
        private readonly MinioService _minioService;

        private readonly string _codec;

        /// <summary>
        /// Constructor
        /// </summary>
        public VideoProcessingJobConsumer(IConfiguration configuration, IPublishEndpoint publishEndpoint, MinioService minioService)
        {
            _publishEndpoint = publishEndpoint;
            _minioService = minioService;

            _codec = configuration.GetSection("FFMpeg:Codec").Get<string>()!;
        }

        /// <summary>
        /// Consume
        /// </summary>
        public async Task Consume(ConsumeContext<VideoProcessingEvent> context)
        {
            var job = context.Message;

            Logger.Information($"Start: {job.Quality}");

            var quality = (VideoSize)(int)job.Quality;

            var options = new Action<FFMpegArgumentOptions>(options =>
            {
                options.WithVideoCodec(_codec);
                options.WithConstantRateFactor(21);
                options.WithVideoFilters(filterOptions => filterOptions.Scale(quality));
                options.WithFastStart();
            });

            var outputFile = $"./temp{job.FileExtension}";

            var result = FFMpegArguments.FromUrlInput(new Uri(job.FileUrl))
                .OutputToFile(outputFile, true, options)
                .ProcessSynchronously();

            Logger.Information($"State: {result}");

            var resultFileName = await _minioService.UploadContent(outputFile, job.FileExtension, job.ContentType);

            Logger.Information($"Upload: {resultFileName}");

            var content = new Content()
            {
                Quality = job.Quality,
                Path = resultFileName
            };

            object? answerEvent = null;

            switch (job.DataType)
            {
                case DataTypeEnum.Movie:
                    answerEvent = new MovieAddContentEvent()
                    {
                        MovieId = job.ContentId,
                        Content = content
                    };
                    break;
                case DataTypeEnum.Serial:
                    answerEvent = new SerialAddContentEvent()
                    {
                        EpisodeId = job.ContentId,
                        Content = content
                    };
                    break;
                case DataTypeEnum.Anime:
                    answerEvent = new AnimeAddContentEvent()
                    {
                        ContentId = job.ContentId,
                        IsEpisode = job.IsEpisode,
                        Content = content
                    };
                    break;
            }

            await _publishEndpoint.Publish(answerEvent!);
        }
    }
}
