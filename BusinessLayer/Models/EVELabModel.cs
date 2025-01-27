using BusinessLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.Models
{
    public class EVELabModel : ILabModel
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        [JsonPropertyName("description")]
        public required string Description { get; set; }
        [JsonPropertyName("filename")]
        public required string Filename { get; set; }
        [JsonPropertyName("mtime")]
        public DateTime Last_modified { get; set; }
    }
}
