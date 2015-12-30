using System;
using System.IO;

namespace BEncode
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 3) usage();

                BEncode b = new BEncode();
                switch (args[0])
                {
                    case "encode":
                        b.loadXML(args[1]);
                        if (File.Exists(args[2])) Console.WriteLine("Output file already exists.");
                        else b.save(args[2]);
                        break;
                    case "decode":
                        b.load(args[1]);
                        var xmldata = b.getXML();
                        if (File.Exists(args[2])) Console.WriteLine("Output file already exists.");
                        else File.WriteAllBytes(args[2], xmldata);
                        break;
                    default:
                        usage();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static void usage()
        {
            var name = System.AppDomain.CurrentDomain.FriendlyName;
            Console.WriteLine("Usage: " + name.Substring(0, name.Length - 4) + " <command> <input-file> <output-file>");
            Console.WriteLine();
            Console.WriteLine("Commands:\ndecode — convert BEncode to XML\nencode — convert XML to BEncode");
            Environment.Exit(0);
        }
    }
}