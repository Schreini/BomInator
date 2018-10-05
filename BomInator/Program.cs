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
        protected internal static byte[][] AnsiBytes = new byte[][] { new byte[] { 228 }, new byte[] { 246 }, new byte[] { 252 }, new byte[] { 223 } };

        protected internal static byte[][] Utf8Bytes = new byte[][]
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

            var arguments = ParseArguments(args);
            IEnumerable<FileInfo> files = FindFiles(arguments.Directory, arguments.FileNamePattern);

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
                        continue;
                    }

                    if (FindBytes(fileBytes, file.Length, AnsiBytes))
                    {
                        file.FoundEncodings.Add("iso-8859-1");
                    }

                    else if (FindBytes(fileBytes, file.Length, Utf8Bytes))
                    {
                        file.FoundEncodings.Add("utf-8");
                    }
                }

                if (file.FoundEncodings.Count > 1)
                {
                    Console.WriteLine("multiencodings " + file.FullName);
                }
                else
                {
                    var mover = new FileInfo(file.FullName);
                    mover.MoveTo(file.FullName + ".bak");

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
                .Callback(help => Console.WriteLine(help));
            parser
                .MakeCaseInsensitive()
                .Setup(a => a.ListEncodings)
                .As('l', "listEncodings")
                .WithDescription("Print a list of all possible encodings");
            parser
                .MakeCaseInsensitive().Setup(a => a.Directory)
                .As('d', "directory")
                .WithDescription("Directory to be scanned")
                .Required();
            parser
                .MakeCaseInsensitive().Setup(a => a.FileNamePattern)
                .As('p', "FileNamePattern")
                .WithDescription(
                    "Comma separated list of Pattern to match Filenames. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.")
                .Required();
            parser
                .MakeCaseInsensitive().Setup(a => a.OriginalEncoding)
                .As('e', "originalEncoding")
                .WithDescription("");
            ICommandLineParserResult result = parser.Parse(args);

            var arguments = parser.Object;
            if (arguments.ListEncodings)
            {
                ListEncodings();
                Exit();
            }

            if (result.HelpCalled || result.HasErrors)
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
            var dir = new DirectoryInfo(directory);
            var files = dir.EnumerateFiles(pattern, SearchOption.AllDirectories);
            return files;
        }
    }

    internal class CommandLineArgments
    {
        public bool ListEncodings { get; set; }
        public string Directory { get; set; }
        public string FileNamePattern { get; set; }
        public string OriginalEncoding { get; set; }
        public bool Validate { get; set; }
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
