using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a single node within a Cisco CML lab.
    /// </summary>
    public class CiscoNodeModel
    {
        /// <summary>
        /// Unique identifier of the node.
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// Display name (label) of the node.
        /// </summary>
        [JsonPropertyName("label")]
        public required string Name { get; set; }

        /// <summary>
        /// Current state of the node (e.g., "started", "stopped").
        /// </summary>
        [JsonPropertyName("state")]
        public required string State { get; set; }

        /// <summary>
        /// Number of virtual CPUs assigned to the node.
        /// </summary>
        [JsonPropertyName("cpus")]
        public int? Cpu { get; set; }

        /// <summary>
        /// CPU usage limit for the node (if defined).
        /// </summary>
        [JsonPropertyName("cpu_limit")]
        public int? CpuLimit { get; set; }

        /// <summary>
        /// Amount of RAM (in MB) allocated to the node.
        /// </summary>
        [JsonPropertyName("ram")]
        public int? Memory { get; set; }

        /// <summary>
        /// Size of the data volume associated with the node (in MB).
        /// </summary>
        [JsonPropertyName("data_volume")]
        public int? DataVolume { get; set; }
    }

}
