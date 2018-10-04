using Fclp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BomInator
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = ParseArguments(args);
            if (arguments.ListEncodings)
            {
                ListEncodings();
            }

            IEnumerable<FileInfo> files = FindFiles(arguments.Directory, arguments.FileNamePattern);
            var bom = Encoding.UTF8.GetPreamble();
            foreach (var file in files)
            {
                using (FileStream fs = file.OpenRead())
                {
                    var b = new byte[bom.Length];
                    var x = fs.Read(b, 0, bom.Length);

                    foreach (byte b1 in bom)
                    {
                        Console.Write($"{Convert.ToInt16(b1),4}");
                    }
                    Console.Write(" | ");
                    foreach (byte b1 in b)
                    {
                        Console.Write($"{Convert.ToInt16(b1),4}");
                    }
                    Console.Write(" | ");

                    if (bom.SequenceEqual(b))
                    {
                        Console.WriteLine($"match{file.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"NO match{file.Name}");
                    }
                }
            }
            Console.ReadLine();
        }

        private static void ListEncodings()
        {
            var encodings = Encoding.GetEncodings();
            Console.WriteLine("List of all available encodings:");
            foreach (var encoding in encodings.OrderBy(e => e.Name))
            {
                Console.WriteLine($"{encoding.CodePage}\t{encoding.Name,-10}\t{encoding.DisplayName}");
            }
            Environment.Exit(0);
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
                    "Comma separated list of Pattern to match Filenames. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.");
            parser
                .MakeCaseInsensitive().Setup(a => a.OriginalEncoding)
                .As('e', "originalEncoding")
                .WithDescription("");
            ICommandLineParserResult result = parser.Parse(args);

            if (result.HelpCalled || result.HasErrors)
            {
                parser.HelpOption.ShowHelp(parser.Options);
                Environment.Exit(1);
            }
            var arguments = parser.Object;

            return arguments;
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
}
