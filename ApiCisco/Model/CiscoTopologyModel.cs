using System.Text.Json.Serialization;

namespace ApiCisco.Model
{
    /// <summary>
    /// Definition of the Cisco topology model.for deserialization of the Cisco topology JSON data.
    /// This model represents the structure of the topology, including nodes, links, and their associated properties.
    /// </summary>
    public class CiscoTopologyModel
    {
        [JsonPropertyName("nodes")]
        public List<CiscoTopologyNodeModel> Nodes { get; set; } = new();

        [JsonPropertyName("links")]
        public List<CiscoTopologyLinkModel> Links { get; set; } = new();
    }

    public class CiscoTopologyNodeModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("node_definition")]
        public string NodeDefinition { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("interfaces")]
        public List<CiscoTopologyInterfaceModel> Interfaces { get; set; } = new();
    }

    public class CiscoTopologyInterfaceModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class CiscoTopologyLinkModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("interface_a")]
        public string? InterfaceA { get; set; }

        [JsonPropertyName("interface_b")]
        public string? InterfaceB { get; set; }

        [JsonPropertyName("n1")]
        public string? NodeA { get; set; }

        [JsonPropertyName("n2")]
        public string? NodeB { get; set; }

        [JsonPropertyName("i1")]
        public string? I1 { get; set; }

        [JsonPropertyName("i2")]
        public string? I2 { get; set; }
    }
}
