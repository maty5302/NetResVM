using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.Models
{
    public class EVENodeModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("cpu")]
        public int NumberOfCPU {  get; set; }
        [JsonPropertyName("ram")]
        public int Memory {  get; set; }
        [JsonPropertyName("ethernet")]
        public int NumberOfEthernet { get; set; }
        [JsonPropertyName("url")]
        public required string UrlConnect { get; set; }

    }
}
