using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.Models
{
    public class LabModel
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("lab_title")]
        public required string Name { get; set; }
        [JsonPropertyName("lab_description")]
        public required string Description { get; set; }
        [JsonPropertyName("node_count")]
        public int Node_count { get; set; }
        [JsonPropertyName("link_count")]
        public int Link_count { get; set; }
        [JsonPropertyName("state")]
        public required string State { get; set; }
        [JsonPropertyName("modified")]
        public DateTime Last_modified { get; set; }
    }
}
