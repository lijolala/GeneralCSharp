using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

class Program
{
    static async Task Main()
    {
        string rootFolder = @"D:\Everbridge\Connections\VCC"; // Update with your folder path
        var dllFiles = Directory.GetFiles(rootFolder, "*.dll", SearchOption.AllDirectories);

        List<DllInfo> dllInfos = new List<DllInfo>();

        foreach (var dll in dllFiles)
        {
            string version = "Unknown";
            try
            {
                version = AssemblyName.GetAssemblyName(dll).Version?.ToString() ?? "Unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting version for {dll}: {ex.Message}");
            }
            var latestVersion = await GetLatestNuGetVersionAsync(Path.GetFileNameWithoutExtension(dll));

            dllInfos.Add(new DllInfo
            {
                FilePath = dll,
                CurrentVersion = version?.ToString(),
                LatestVersion = latestVersion
            });

            Console.WriteLine($"{dll}: Current Version: {version}, Latest NuGet Version: {latestVersion}");
        }

        ExportToCsv(dllInfos, @"D:\Everbridge\Connections\VCC\dll_versions.csv");
    }

    static async Task<string> GetLatestNuGetVersionAsync(string packageId)
    {
        var logger = NullLogger.Instance;
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        var versions = await resource.GetAllVersionsAsync(packageId, cache, logger, System.Threading.CancellationToken.None);

        return versions?.Max()?.ToString();
    }

    static void ExportToCsv(List<DllInfo> dllInfos, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("FilePath,CurrentVersion,LatestVersion");

            foreach (var dllInfo in dllInfos)
            {
                writer.WriteLine($"{dllInfo.FilePath},{dllInfo.CurrentVersion},{dllInfo.LatestVersion}");
            }
        }
    }
}

class DllInfo
{
    public string FilePath { get; set; }
    public string CurrentVersion { get; set; }
    public string LatestVersion { get; set; }
}
