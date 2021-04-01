using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using static System.Environment;

namespace AIP_VFR
{
    partial class Program
    {
        private static readonly List<AipConfigItem> Configurations = new List<AipConfigItem>
        {
            new AipConfigItem
            {
                Name = "VFR",
                Directory = "AD 4",
                Url = @"https://www.ais.pansa.pl/vfr"
            },
            new AipConfigItem
            {
                Name = "AIP",
                Directory = "AD 2",
                Url = @"https://www.ais.pansa.pl/aip"
            }
        };

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var workingDir = args.FirstOrDefault()?.TrimEnd(new char[] { '\\' });
            var directory = PrepareDirectory(workingDir);

            foreach (var config in Configurations)
            {
                DownloadAirportPdfs(config, directory);
            }
        }

        private static void DownloadAirportPdfs(AipConfigItem config, string directory)
        {
            var allItems = GetConfigurations(config.Url);
            var airportTreeId = allItems.Single(x => x.Name == config.Directory).Id;
            var airports = allItems.Where(x =>
                x.ParentId == airportTreeId &&
                !x.Name.StartsWith("LOTNISKA") &&
                !x.Name.StartsWith("AD"));

            Console.WriteLine($"{NewLine}Znaleziono {airports.Count()} lotnisk dla {config.Name} - rozpoczęcie pobierania{NewLine}");

            foreach (var airport in airports)
            {
                var airportDir = $@"{directory}\{airport.Name}";
                var files = allItems
                    .Where(x => x.ParentId == airport.Id)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url));

                SaveAirportFiles(config, airport, airportDir, files);
                SaveDescription(airportDir, files);

                Console.WriteLine($"Pobrano wszystkie pliki oraz info.txt dla {airport.Name}{NewLine}");
            }
        }

        private static void SaveAirportFiles(AipConfigItem config, AipInputItem airport, 
            string airportDir, IEnumerable<AipInputItem> files)
        {
            var client = new WebClient();

            Directory.CreateDirectory(airportDir);

            foreach (var file in files)
            {
                var urlToDownload = $"{config.Url}/{file.Url}";

                Console.WriteLine($"Pobieranie {airport.Name} > {file.Filename}...");
                client.DownloadFile(urlToDownload, $@"{airportDir}\{file.Filename}");
            }
        }

        private static void SaveDescription(string airportDir, IEnumerable<AipInputItem> files)
        {
            var maxWidth = files.Max(x => x.Filename.Length);
            var descriptions = files
                .OrderBy(x => x.Filename)
                .Select(x => $"{x.Filename.PadRight(maxWidth)} > {x.Description}");

            File.WriteAllLines($@"{airportDir}\info.txt", descriptions);
        }

        private static string PrepareDirectory(string dir)
        {
            var directory = $@"{dir}\Lotniska AIP VFR {DateTime.Now:yyyy-MM-dd}".Trim(new char[] { '\\' });
            var workingDirectory = directory;
            var i = 1;

            while (Directory.Exists(workingDirectory))
            {
                workingDirectory = $"{directory}_{i}";
                i++;
            }

            Console.WriteLine($"Folder w użyciu: {workingDirectory}");

            Directory.CreateDirectory(workingDirectory);

            return workingDirectory;
        }

        private static IEnumerable<AipInputItem> GetConfigurations(string url)
        {
            using var client = new WebClient
            {
                Encoding = Encoding.GetEncoding("ISO-8859-2")
            };

            var html = client.DownloadString($"{url}/left.htm");
            var matches = new Regex(@"d\.add\((.*)\)\;").Matches(html);
            var rawData = matches.Select(x => x.Value
                        .Replace("d.add(", "")
                        .Replace(");", "")
                        .Split(',')
                        .Select(x => x.Trim(new char[] { '\'' })).ToArray());

            return rawData.Select(x => new AipInputItem
            {
                Id = int.Parse(x[0]),
                ParentId = int.Parse(x[1]),
                Name = x[2],
                Url = x[3],
                Description = x[4]?.Split("\\n")?.FirstOrDefault()
            });
        }
    }
}
