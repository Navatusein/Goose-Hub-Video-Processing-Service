using MassTransit;
using System.ComponentModel.DataAnnotations;
using VideoProcessingService.Models;

namespace VideoProcessingService.MassTransit.Events
{
    /// <summary>
    /// Model for SerialAddContentEvent
    /// </summary>
    [EntityName("movie-api-serial-add-content")]
    [MessageUrn("SerialAddContentEvent")]
    public class SerialAddContentEvent
    {
        /// <summary>
        /// Gets or Sets EpisodeId
        /// </summary>
        [Required]
        public string EpisodeId { get; set; } = null!;

        /// <summary>
        /// Gets or Sets IsEpisode
        /// </summary>
        [Required]
        public Content Content { get; set; } = null!;
    }
}
