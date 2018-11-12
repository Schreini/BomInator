using Fclp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BomInator
{
    class Program
    {
        private static CommandLineArgments Args;
        private static readonly byte[][] AnsiBytes = new byte[][] { new byte[] { 228 }, new byte[] { 246 }, new byte[] { 252 }, new byte[] { 223 } };
        private static readonly byte[] ValidAnsiBytes = new byte[] {10, 13, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126};
        private static readonly byte[][] Utf8Bytes = new byte[][]
        {
            new byte[] {195,164},
            new byte[] {195,182},
            new byte[] {195,188},
            new byte[] {195,159}
        };

        //-d=g:\BomInator\TestFiles -p=*.cs
        static void Main(string[] args)
        {
            IBomInatorService bomService = new BomInatorService();

            Args = ParseArguments(args);
            IEnumerable<FileInfo> files = FindFiles(Args.Directory, Args.FileNamePattern);

            if (Args.Analyze)
            {
                Analyze(files);
                Exit();
            }

            byte[] fileBytes = new byte[0];

            var bomFiles = files.Select(f => new BomFile() { FileInfo = f });

            foreach (var file in bomFiles)
            {
                if (file.Length > fileBytes.Length) fileBytes = new byte[file.Length];
                using (FileStream fs = file.OpenRead())
                {
                    fs.Read(fileBytes, 0, Convert.ToInt32(file.Length));

                    if (!bomService.NeedsBom(fileBytes))
                    {
                        PrintVerbose($"Has BOM    {file.FullName}");
                        continue;
                    }

                    if (FindBytes(fileBytes, file.Length, AnsiBytes))
                    {
                        file.FoundEncodings.Add("iso-8859-1");
                        
                        var invalidBytes = ValidateAllAnsiBytes(fileBytes, file.Length, ValidAnsiBytes);
                        var bytes = invalidBytes.ToList();
                        if (bytes.Any())
                        {
                            IEnumerable<string> enumerable = bytes.Select(b => b + ":" + Convert.ToChar(b));

                            file.FoundEncodings.Add("invalid: " + string.Join("; ", enumerable) );
                        }
                    }

                    else if (FindBytes(fileBytes, file.Length, Utf8Bytes))
                    {
                        file.FoundEncodings.Add("utf-8");
                    }
                }

                if (file.FoundEncodings.Count > 1)
                {
                    Console.WriteLine("multiencodings " + file.FullName);
                    foreach (var fileFoundEncoding in file.FoundEncodings)
                    {
                        Console.WriteLine(fileFoundEncoding);
                    }
                }
                else
                {
                    PrintVerbose($"{file.FoundEncodings.FirstOrDefault(),-10} {file.FullName}");
                    CreateBackup(file);

                    var inputEncoding = Encoding.GetEncoding(file.FoundEncodings.FirstOrDefault() ?? "iso-8859-1");
                    char[] chars = inputEncoding.GetChars(fileBytes, 0, Convert.ToInt32(file.Length));

                    using(FileStream fs = file.FileInfo.OpenWrite())
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        sw.Write(chars);
                    }

                }
            }

            Console.ReadLine();
        }

        private static void Analyze(IEnumerable<FileInfo> files)
        {
            var analyzer = new EncodingAnalyzer();
            foreach (var fileInfo in files)
            {
                using (var s = fileInfo.OpenRead())
                {
                    byte[] bytes = new byte[s.Length];
                    s.Read(bytes);
                    var encoding = analyzer.Analyze(bytes);
                    if (Args.Ignore == null || encoding.EncodingName != Args.Ignore)
                    {
                        Console.WriteLine($"{encoding.EncodingName,-30} {fileInfo.FullName}");
                    }
                }
            }
        }

        private static IEnumerable<byte> ValidateAllAnsiBytes(byte[] fileBytes, int fileLength, byte[] validAnsiBytes)
        {
            var invalidBytes = new List<byte>();

            for(int i = 0; i < fileLength; i++)
            {
                if (validAnsiBytes.Contains(fileBytes[i]))
                    continue;
                foreach (var ansiByte in AnsiBytes)
                {
                    if (!ansiByte.Contains(fileBytes[i]))
                        invalidBytes.Add(fileBytes[i]);
                }
            }

            return invalidBytes;
        }

        private static void CreateBackup(BomFile file)
        {
            if (!Args.Backup) return;

            var mover = new FileInfo(file.FullName);
            mover.MoveTo(file.FullName + ".bak");
        }

        private static void PrintVerbose(string message)
        {
            if (!Args.Verbose) return;
            
            Console.WriteLine(message);
        }

        private static bool FindBytes(byte[] source, long len, IEnumerable<byte[]> bytesToFind)
        {
            foreach (var pattern in bytesToFind)
            {
                for (int i = 0; i < len; i++)
                {
                    if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ListEncodings()
        {
            var encodings = Encoding.GetEncodings();
            Console.WriteLine("List of all available encodings:");
            foreach (var encoding in encodings.OrderBy(e => e.Name))
            {
                Console.WriteLine($"{encoding.CodePage}\t{encoding.Name,-10}\t{encoding.DisplayName}");
            }
        }

        private static CommandLineArgments ParseArguments(string[] args)
        {
            var parser = new FluentCommandLineParser<CommandLineArgments>();
            parser
                .SetupHelp("?", "h", "help")
                .Callback(help =>
                {
                    Console.WriteLine(help);
                    Exit();
                });
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.ListEncodings)
                .As('l', "listEncodings")
                .WithDescription("Print a list of all possible encodings");
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.Directory)
                .As('d', "directory")
                .WithDescription("Directory to be scanned")
                .Required();
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.FileNamePattern)
                .As('p', "FileNamePattern")
                .WithDescription(
                    "Comma separated list of Pattern to match Filenames. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.")
                .Required();
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.OriginalEncoding)
                .As('e', "originalEncoding")
                .WithDescription("");
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.Verbose)
                .As("verbose")
                .WithDescription("Print verbose output.");
            parser
                .MakeCaseInsensitive()
                .Setup(a=> a.Backup)
                .As('b', "backup")
                .WithDescription("Create Backup Files.");
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.Analyze)
                .As('a', "analyze")
                .WithDescription("Only analyze files and print encodings per file.");
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.Ignore)
                .As('i', "ignore")
                .WithDescription("Only analyze files and print encodings per file.");

           ICommandLineParserResult result = parser.Parse(args);

            var arguments = parser.Object;
            if (arguments.ListEncodings)
            {
                ListEncodings();
                Exit();
            }

            if (result.HasErrors)
            {
                parser.HelpOption.ShowHelp(parser.Options);
                Exit();
            }

            return arguments;
        }

        private static void Exit()
        {
            Console.ReadLine();
            Environment.Exit(1);
        }

        private static IEnumerable<FileInfo> FindFiles(string directory, string pattern)
        {
            var splits = pattern.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var dir = new DirectoryInfo(directory);
            foreach (var singlePattern in splits)
            {
                var files = dir.EnumerateFiles(singlePattern, SearchOption.AllDirectories).OrderBy(f=>f.FullName);
                foreach (var fileInfo in files)
                {
                    yield return fileInfo;
                }
            }
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class CommandLineArgments
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public bool ListEncodings { get; set; }
        public string Directory { get; set; }
        public string FileNamePattern { get; set; }
        public string OriginalEncoding { get; set; }
        public bool Validate { get; set; }
        public bool Verbose { get; set; }
        public bool Backup { get; set; }
        public bool Analyze { get; set; }
        public string Ignore { get; set; }

        // ReSharper restore UnusedAutoPropertyAccessor.Global
    }

    internal class BomFile
    {
        public BomFile()
        {
            FoundEncodings = new List<string>();
        }

        public FileInfo FileInfo { get; set; }
        public IList<string> FoundEncodings { get; }
        public int Length => Convert.ToInt32(FileInfo.Length);
        public long LongLength => FileInfo.Length;
        public string FullName => FileInfo.FullName;

        public FileStream OpenRead()
        {
            return FileInfo.OpenRead();
        }
    }
}
