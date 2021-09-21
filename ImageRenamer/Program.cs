using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Iptc;

namespace ImageRenamer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Must have two arguments: <path> <ByName|ByDate>");
                return;
            }
            var dirPath = args[0];
            var byName = args[1].ToLower().Contains("byname");
            DirectoryInfo info = new DirectoryInfo(dirPath);
            var files = info.GetFiles().ToList();
            var digits = files.Count.GetDigits();
            var filesWithDateTaken = byName ? OrderByName(files) : OrderByDateTaken(files);
            RenameFiles(filesWithDateTaken, true, digits);
            RenameFiles(filesWithDateTaken, false, digits);
        }

        private static int GetDigits(this int n) => (int)Math.Floor(Math.Log10(n) + 1);
        private static List<FileInfo> OrderByName(IEnumerable<FileInfo> files) => files.OrderBy(f => f.Name).ToList();

        private static List<FileInfo> OrderByDateTaken(IEnumerable<FileInfo> files) => files
                .Select(f => (FileInfo: f, DateTaken: ReadDateTaken(f.FullName)))
                .OrderBy(f => f.DateTaken)
                .Select(f => f.FileInfo)
                .ToList();
        

        private static void RenameFiles(List<FileInfo> files, bool renameToTemp, int padding)
        {
            var i = 0;
            foreach (var file in files)
            {
                i++;
            
                var newName = i.ToString().PadLeft(padding, '0') + ".jpg";
                var fromPath = renameToTemp ? file.FullName : Path.Join(file.DirectoryName, "temp_" + newName);
                if (renameToTemp)
                {
                    newName = "temp_" + newName;
                }
                var newPath = Path.Join(file.DirectoryName, newName);
                File.Move(fromPath, newPath);
            }
        }

        private static DateTime ReadDateTaken(string imagePath)
        {
            var directories = ImageMetadataReader.ReadMetadata(imagePath);
            var subIfdDirectory = directories.OfType<IptcDirectory>().First();
            var dateCreated = subIfdDirectory.GetDateTime(IptcDirectory.TagDigitalDateCreated);
            var timeCreated = subIfdDirectory.GetString(IptcDirectory.TagDigitalTimeCreated);
            int hours = int.Parse(timeCreated.Substring(0, 2));
            int minutes = int.Parse(timeCreated.Substring(2, 2));
            int seconds = int.Parse(timeCreated.Substring(4, 2));
            return dateCreated.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
        }
    }
}