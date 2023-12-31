﻿namespace ReportPackageUsage
{
    using Newtonsoft.Json.Linq;

    public class PackageJson
    {
        public List<NpmDependency> AllDependencies { get; } = new();

        public PackageJson(string jsonContent)
        {
            var root = JObject.Parse(jsonContent);

            var devDependencies = root.Descendants().Where(t => t.Path.Contains("Dependencies", StringComparison.OrdinalIgnoreCase));
            foreach (var depGroup in devDependencies)
            {
                foreach (var dep in depGroup)
                {
                    foreach (var leaf in dep.Children())
                    {
                        var jp = leaf as JProperty;
                        if (jp == null)
                        {
                            continue;
                        }

                        var npmd = new NpmDependency
                        {
                            Name = jp.Name,
                            Versions = jp.Value.ToString()
                        };
                        AllDependencies.Add(npmd);
                    }
                }
            }
        }
    }
}