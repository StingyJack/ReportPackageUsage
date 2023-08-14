namespace ReportPackageUsage
{
    using System.Diagnostics;
    using System.Xml.Linq;
    using Newtonsoft.Json;

    public static class Program
    {
        private static ILog Log { get; set; }

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("The first argument must be a directory path (including root/drive) where all the VS projects you want to scan are at.");
            }

            var loggingCancellationTokenSource = new CancellationTokenSource();
            Log = new Clogrr($"{nameof(ReportPackageUsage)}.{DateTime.Now:yyyy.MM.dd.HH.mm.ss}.log", Environment.CurrentDirectory, loggingCancellationTokenSource.Token);

            var folderPath = args[0].Trim();
            var overallTime = Stopwatch.StartNew();


            if (Directory.Exists(folderPath) == false)
            {
                throw new DirectoryNotFoundException($"The directory path given does not exist '{folderPath}'");
            }

            Log.Info($"Using local folder {folderPath} to search for usages");

            var repoFolders = Directory.GetDirectories(folderPath, "*.*", SearchOption.TopDirectoryOnly).ToList();
            var searchCriteria = $"namedFolder.{folderPath.Replace("\\", "_").Replace(":", string.Empty)}";

            Log.Info("Gathering referenced dependencies");
            var usedPackages = new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var repoFolder in repoFolders)
            {
                usedPackages = GetNugetReferences(repoFolder, ref usedPackages);
                usedPackages = GetNpmReferences(repoFolder, ref usedPackages);
            } //repoFolder


            var outputFile = $"UsedPackageReport.{FsUtils.GetFsSafeIdentifier(searchCriteria)}.{DateTime.Now:yyyy-MM-dd}.json";
            var outputFilePath = Path.Join(Environment.CurrentDirectory, outputFile);
            var sortedUsedPackages = usedPackages.OrderBy(k => k.Key);
            var outputResult = new List<PackageInfo>();
            foreach (var (_, value) in sortedUsedPackages)
            {
                value.Consumers = value.Consumers.OrderBy(s => s).ToList();
                outputResult.Add(value);
            }

            var jsonnedData = JsonConvert.SerializeObject(outputResult, Formatting.Indented);
            File.WriteAllText(outputFilePath, jsonnedData);
            overallTime.Stop();
            Log.Info($"Results written to {outputFilePath}. Overall time was {overallTime.Elapsed}");
            loggingCancellationTokenSource.Cancel();
        }

        private static Dictionary<string, PackageInfo> GetNugetReferences(string repoFolder, ref Dictionary<string, PackageInfo> usedPackages)
        {
            var csProjFiles = Directory.GetFiles(repoFolder, "*.csproj", SearchOption.AllDirectories);
            var vbProjFiles = Directory.GetFiles(repoFolder, "*.vbproj", SearchOption.AllDirectories);
            var packageConfigs = Directory.GetFiles(repoFolder, "packages.config", SearchOption.AllDirectories);
            var nugetReferencingFiles = new List<string>();
            nugetReferencingFiles.AddRange(csProjFiles);
            nugetReferencingFiles.AddRange(vbProjFiles);
            nugetReferencingFiles.AddRange(packageConfigs);

            foreach (var nugetReferencingFile in nugetReferencingFiles)
            {
                var repoName = new DirectoryInfo(repoFolder).Name;
                var relativeRepoPath = GetRelativeRepoPath(nugetReferencingFile, repoFolder);
                var consumingProjectPath = $"{repoName}\\{relativeRepoPath}";

                try
                {
                    var xdoc = XDocument.Load(nugetReferencingFile);
                    var prNodes = xdoc.Descendants("PackageReference");
                    foreach (var prNode in prNodes)
                    {
                        if (prNode.Attribute("Include") == null)
                        {
                            Log.Info($"package ref element from project {consumingProjectPath} has \n\t {prNode}");
                            continue;
                        }

                        var id = prNode.Attribute("Include")!.Value;
                        var version = prNode.Attribute("Version")?.Value ?? "*";
                        AddOrUpdateUsedPackages(id, version, ref usedPackages, consumingProjectPath, "nuget");
                    } //prNode

                    var pkgNodes = xdoc.Descendants("package");
                    foreach (var pkgNode in pkgNodes)
                    {
                        var id = pkgNode.Attribute("id")!.Value;
                        var version = pkgNode.Attribute("version")!.Value;
                        AddOrUpdateUsedPackages(id, version, ref usedPackages, consumingProjectPath, "nuget");
                    } //pkgNode
                }
                catch (Exception e)
                {
                    Log.Err($"Error while reading file {nugetReferencingFile}. {e}");
                }
            } //targtFile

            return usedPackages;
        }

        private static Dictionary<string, PackageInfo> GetNpmReferences(string repoFolder, ref Dictionary<string, PackageInfo> usedPackages)
        {
            var allPackageJsonFiles = Directory.GetFiles(repoFolder, "package.json", SearchOption.AllDirectories);
            var packageJsonFiles = allPackageJsonFiles.Where(p => p.Contains("node_modules", StringComparison.OrdinalIgnoreCase) == false);


            foreach (var packageJsonFile in packageJsonFiles)
            {
                var repoName = new DirectoryInfo(repoFolder).Name;
                var relativeRepoPath = GetRelativeRepoPath(packageJsonFile, repoFolder);
                var consumingProjectPath = $"{repoName}\\{relativeRepoPath}";

                try
                {
                    var content = File.ReadAllText(packageJsonFile);
                    var pj = new PackageJson(content);
                    foreach (var dep in pj.AllDependencies)
                    {
                        var id = dep.Name;
                        var version = dep.Versions;
                        AddOrUpdateUsedPackages(id, version, ref usedPackages, consumingProjectPath, "npm");
                    }
                }
                catch (Exception e)
                {
                    Log.Err($"Error while reading file {packageJsonFile}. {e}");
                }
            } //targtFile

            return usedPackages;
        }

        private static void AddOrUpdateUsedPackages(string id, string version, ref Dictionary<string, PackageInfo> usedPackages, string consumingProjectPath, string packageType)
        {
            var pi = new PackageInfo { Id = id, VersionString = version, PackageType = packageType };
            if (usedPackages.ContainsKey(pi.UniqueId))
            {
                usedPackages[pi.UniqueId].Consumers.Add(consumingProjectPath);
            }
            else
            {
                pi.Consumers.Add(consumingProjectPath);
                usedPackages.Add(pi.UniqueId, pi);
            }
        }

        private static string GetRelativeRepoPath(string fullFileName, string repoFolder)
        {
            var returnValue = Path.TrimEndingDirectorySeparator(new FileInfo(fullFileName).FullName
                    .Replace(repoFolder, string.Empty, StringComparison.OrdinalIgnoreCase))
                .TrimStart('\\');
            return returnValue;
        }
    }
}