using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ApiCisco.Model
{
    public class NodeModel
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("label")]
        public required string Name { get; set; }
        [JsonPropertyName("state")]
        public required string State { get; set; }
        [JsonPropertyName("cpus")]
        public int? Cpu { get; set; }
        [JsonPropertyName("cpu_limit")]
        public int? CpuLimit { get; set; }
        [JsonPropertyName("ram")]
        public int? Memory { get; set; }
        [JsonPropertyName("data_volume")]
        public int? DataVolume { get; set; }
    }
}
