using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

public class DllInfo
{
    public string FilePath { get; set; }
    public string Version { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        string folderPath = @"D:\Everbridge\Connections\VCC"; // Specify your folder path here
        List<DllInfo> dllInfos = GetDllInfos(folderPath);

        string csvPath = @"D:\Everbridge\Connections\VCC\dll_info.csv"; // Specify the export CSV path here
        ExportToCsv(dllInfos, csvPath);

        Console.WriteLine("DLL information exported successfully.");
    }

    public static List<DllInfo> GetDllInfos(string folderPath)
    {
        var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);
        List<DllInfo> dllInfos = new List<DllInfo>();

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(dllFile);
                var version = assemblyName.Version?.ToString() ?? "Unknown";
                dllInfos.Add(new DllInfo { FilePath = dllFile, Version = version });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting version for {dllFile}: {ex.Message}");
                dllInfos.Add(new DllInfo { FilePath = dllFile, Version = "Error" });
            }
        }

        return dllInfos;
    }

    public static void ExportToCsv(List<DllInfo> dllInfos, string csvPath)
    {
        using (var writer = new StreamWriter(csvPath))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.WriteRecords(dllInfos);
        }
    }
}
