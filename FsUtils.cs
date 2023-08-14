namespace ReportPackageUsage
{
    using System;
    using System.IO;

    public class FsUtils
    {
        private ILog Log { get; }

        public FsUtils(ILog log)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public static string GetFsSafeIdentifier(string searchCriteria)
        {
            return searchCriteria.Equals("*") ? "all" : searchCriteria;
        }

        public string GetRepoDownloadFolder(string[] args)
        {
            var repoDlFolder = Path.GetTempPath();
            if (args.Length > 1)
            {
                if (Directory.Exists(args[1]))
                {
                    repoDlFolder = args[1];
                }
                else
                {
                    Log.Err($"A repo downloads parent folder was given but that path doesnt exist - '{args[1]} ");
                }
            }

            return repoDlFolder;
        }

        public static void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory) == false)
            {
                return;
            }

            foreach (var subdirectory in Directory.EnumerateDirectories(directory))
            {
                DeleteDirectory(subdirectory);
            }

            foreach (var fileName in Directory.EnumerateFiles(directory))
            {
                var fileInfo = new FileInfo(fileName)
                {
                    Attributes = FileAttributes.Normal
                };
                fileInfo.Delete();
            }

            Directory.Delete(directory);
        }
        
    }
}