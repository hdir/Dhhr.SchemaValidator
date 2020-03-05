using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Dhhr.Informasjonsmodell
{
    public static class SchemaLoader
    {
        private static readonly XmlReaderSettings XmlReaderSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };

        public static XmlSchemaSet LoadDirectory(string rootPath)
        {
            var paths = Directory.EnumerateFiles(rootPath, "*.xsd", SearchOption.AllDirectories);
            return Load(paths);
        }

        public static XmlSchemaSet Load(IEnumerable<string> paths)
        {
            var schemaSet = new XmlSchemaSet
            {
                XmlResolver = null,
                CompilationSettings = { EnableUpaCheck = false }
            };

            foreach (var path in paths)
            {
                schemaSet.Add(GetSchema(path));
            }

            schemaSet.Compile();
            return schemaSet;
        }

        private static XmlSchema GetSchema(string filename)
        {
            var path = Path.GetFullPath(filename);
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = XmlReader.Create(fileStream, XmlReaderSettings, path))
            {
                return XmlSchema.Read(reader, null);
            }
        }
    }
}
