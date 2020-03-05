using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using Dhhr.Informasjonsmodell;
using Dhhr.SchemaValidator.Services;
using Mono.Options;

namespace Dhhr.SchemaValidator.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var showHelp = false;
            var dryRun = false;
            var listSchemas = false;

            var filePath = default(string);
            var xsdPath = "Resources/Schemas";
            var verbose = false;

            var p = new OptionSet
            {
                {"Help" },
                {"h|help", "Display this message", x => showHelp = true },
                {"d|dry-run", "Test how parameters are parsed", x => dryRun = true },
                {"l|list-schemas", "List supported XSD-schemas", x => listSchemas = true },

                {"Validation" },
                {"f|file=", "(Required) Path to file", x => filePath = x },
                {"x|xsd-path=", "(Optional) Path to XSD-schemas", x => xsdPath = x },
                {"v|verbose", "(Optional) (Resource intensive) Detailed output", x => verbose = true }
            };

            var list = p.Parse(args);

            if (list.Count > 0)
            {
                Console.WriteLine($"Unknown arguments: {string.Join(", ", list)}\n");
                p.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
                return 1;
            }

            if (showHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
                return 0;
            }

            if (dryRun)
            {
                Console.WriteLine("Parsed parameters:");
                Console.WriteLine($"- file: {Path.GetFullPath(filePath)}");
                Console.WriteLine($"- xsd-path: {Path.GetFullPath(xsdPath)}");
                Console.WriteLine($"- verbose: {verbose}");
                Console.WriteLine();
                return 0;
            }

            var schemas = SchemaLoader.LoadDirectory(xsdPath);
            if (listSchemas)
            {
                var namespaces = schemas.Schemas()
                    .OfType<XmlSchema>()
                    .Select(s => s.TargetNamespace)
                    .OrderBy(ns => ns);

                var sb = new StringBuilder();
                sb.AppendLine("Supported xsd-schemas:");
                foreach (var ns in namespaces)
                {
                    sb
                        .Append(" - ")
                        .Append(ns)
                        .AppendLine();
                }

                Console.WriteLine();
                Console.WriteLine(sb);
                return 0;
            }

            if (filePath == default)
            {
                Console.WriteLine("ERROR: A file must be specified");
                return 1;
            }

            // todo more error handling
            // todo better output

            if (verbose)
            {
                bool hasError;
                try
                {
                    var xDoc = XDocument.Load(filePath);
                    hasError = XsdValidator.Validate(xDoc, schemas, Console.Out);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    hasError = true;
                }

                return hasError
                    ? 1
                    : 0;
            }

            try
            {
                XsdValidator.ValidateFile(filePath, schemas, true);
            }
            catch (AggregateException aex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Error in xml:");
                foreach (var iex in aex.InnerExceptions)
                {
                    sb.Append(" - ")
                        .AppendLine(iex.Message);
                }

                Console.WriteLine(sb);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in xml {ex.Message}");
                return 1;
            }

            return 0;
        }
    }
}
