namespace ReportPackageUsage
{
    using Newtonsoft.Json;

    public class PackageInfo
    {
        [JsonProperty(Order = 5)] public List<string> Consumers { get; set; } = new();

        [JsonProperty(Order = 2)] public string Id { get; set; }

        [JsonProperty(Order = 1)] public string UniqueId => $"{Id.ToLower()}|{VersionString.ToLower()}|{PackageType}";

        [JsonProperty(Order = 3)] public string VersionString { get; set; }

        [JsonProperty(Order = 4)] public string PackageType { get; set; }
    }
}