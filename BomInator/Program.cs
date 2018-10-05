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
            byte[] fileBytes = new byte[0];
            foreach (var file in files)
            {
                if(file.Length > fileBytes.Length) fileBytes = new byte[file.Length];
                using (FileStream fs = file.OpenRead())
                {
                    fs.Read(fileBytes, 0, Convert.ToInt32(file.Length));

                    //foreach (byte b1 in bom)
                    //{
                    //    Console.Write($"{Convert.ToInt16(b1),4}");
                    //}
                    //Console.Write(" | ");
                    //foreach (byte b1 in b)
                    //{
                    //    Console.Write($"{Convert.ToInt16(b1),4}");
                    //}
                    //Console.Write(" | ");
                    var b = new byte[bom.Length];
                    Array.Copy(fileBytes, b, b.Length);
                    if (bom.SequenceEqual(b))
                    {
                        //Console.WriteLine($"match{file.FullName}");
                    }
                    else
                    {
                        //, {}, {}, {})
                        
                        if(FindBytes(fileBytes, file.Length,
                            new byte[][] {new byte[] {228}, new byte[] {246}, new byte[] {252}, new byte[] {223}}))
                            Console.Write("ANSI ");
                        else if (FindBytes(fileBytes, file.Length,
                            new byte[][]
                        {
                            new byte[] {195,164}, 
                            new byte[] {195,182}, 
                            new byte[] {195,188}, 
                            new byte[] {195,159}
                        }))
                            Console.Write("UTF8 ");
                        else 
                            Console.Write("???? ");
                        Console.WriteLine($"{file.FullName}");
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
                    "Comma separated list of Pattern to match Filenames. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.")
                .Required();
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
