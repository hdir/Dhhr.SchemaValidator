using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Dhhr.SchemaValidator.Services
{
    public static class XsdValidator
    {
        public static void ValidateFile(string filePath, XmlSchemaSet schemaSet, bool detailed = false)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                Validate(fileStream, schemaSet, detailed);
            }
        }

        public static void Validate(Stream xml, XmlSchemaSet schemaSet, bool detailed = false)
        {
            var settings = new XmlReaderSettings
            {
                Schemas = schemaSet,
                ValidationType = ValidationType.Schema,
                DtdProcessing = DtdProcessing.Ignore,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
                    | XmlSchemaValidationFlags.ProcessIdentityConstraints
                    | XmlSchemaValidationFlags.AllowXmlAttributes
            };

            var errors = new List<Exception>();
            settings.ValidationEventHandler += (sender, e) =>
            {
                if (!detailed)
                {
                    throw e.Exception;
                }

                errors.Add(e.Exception);
            };

            using (var reader = XmlReader.Create(xml, settings))
            {
                while (reader.Read())
                {
                    // reading everything will validate for us
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(errors);
            }
        }

        public static bool Validate(XDocument xDoc, XmlSchemaSet schemaSet, TextWriter output)
        {
            var hasError = false;
            var rules = RuleParser.GetRules(schemaSet, xDoc.Root.GetDefaultNamespace().NamespaceName);

            xDoc.Validate(schemaSet, OnError, true);

            void OnError(object sender, ValidationEventArgs args)
            {
                hasError = true;
                output.WriteLine("Error");
                output.WriteLine($"- Message: {args.Exception.Message}");
                if (sender is XObject xo)
                {
                    output.WriteLine($"- Position: {XPath(xo)}");
                }
                else
                {
                    output.WriteLine("- Position: (Unknown)");
                }

                output.WriteLine();
            }

            return hasError;

            string XPath(XObject xo)
            {
                var sb = new StringBuilder();
                var current = xo;
                while (!(current is null))
                {
                    if (current is XElement xe)
                    {
                        if (rules.Contains(xe.Name.LocalName))
                        {
                            var selector = new StringBuilder();
                            var pks = rules[xe.Name.LocalName].Where(r => r.IsPrimaryKey).ToList();
                            if (pks.Any())
                            {
                                selector.Append('[');
                                foreach (var key in pks)
                                {
                                    try
                                    {
                                        var value = xe.Attribute(key.PropertyName);
                                        if (value is null)
                                        {
                                            continue;
                                        }

                                        if (selector.Length > 1)
                                        {
                                            selector.Append(" and ");
                                        }

                                        selector.Append(value);
                                    }
                                    catch
                                    {
                                    }
                                }

                                selector.Append(']');

                                if (selector.Length > 2)
                                {
                                    sb.Insert(0, selector);
                                }
                            }
                        }

                        sb.Insert(0, $"/{xe.Name.LocalName}");
                    }
                    else if (current is XAttribute xa)
                    {
                        sb.Insert(0, $"/@{xa.Name.LocalName}");
                    }

                    current = current.Parent;
                }

                return sb.ToString();
            }
        }

        private class Rule
        {
            public string ElementName { get; set; }
            public string PropertyName { get; set; }
            public bool IsPrimaryKey { get; set; }
        }

        private static class RuleParser
        {
            internal static ILookup<string, Rule> GetRules(XmlSchemaSet schemaSet, string namespaceName)
            {
                // There must not be more than one entry per TargetNamespace. The line below will intentionally throw if this is not the case.
                var annotationsSchema = schemaSet
                    .Schemas()
                    .Cast<XmlSchema>()
                    .Single(s => s.TargetNamespace == namespaceName && s.SourceUri.Contains("Annotations"));

                return ReadRulesFromSchema(annotationsSchema)
                    .ToLookup(r => r.ElementName);
            }

            private static List<Rule> ReadRulesFromSchema(XmlSchema s)
            {
                return s.Items
                    .OfType<XmlSchemaAnnotation>()
                    .SelectMany(xsa => xsa.Items.OfType<XmlSchemaAppInfo>())
                    .Select(x => CreateRule(x.Markup.Where(m => !m.NodeType.HasFlag(XmlNodeType.Comment))))
                    .ToList();
            }

            private static Rule CreateRule(IEnumerable<XmlNode> x)
            {
                var rad = x.ToDictionary(
                    m => m.Attributes["name"].Value.ToLowerInvariant(),
                    m => m.Attributes["value"]?.Value ?? m.ChildNodes.Item(0)?.Value);

                return new Rule
                {
                    ElementName = PrioritizedGetValueFromDictionary(rad, "classshortname", "classname").Trim(),
                    PropertyName = PrioritizedGetValueFromDictionary(rad, "attributeshortname", "attributename").Trim(),
                    IsPrimaryKey = string.Equals(rad.TryGetValue("primarykey", out var primarykey) ? primarykey : null, "True", StringComparison.CurrentCultureIgnoreCase)
                };
            }

            private static string PrioritizedGetValueFromDictionary(Dictionary<string, string> radDictionary, string keyForPreferredValue, string keyForAlternativeValue)
            {
                if (radDictionary.TryGetValue(keyForPreferredValue, out var preferred) && !string.IsNullOrWhiteSpace(preferred))
                {
                    return preferred;
                }

                if (radDictionary.TryGetValue(keyForAlternativeValue, out var alternative) && !string.IsNullOrWhiteSpace(alternative))
                {
                    return alternative;
                }

                throw new InvalidOperationException("Could not find a value for preferred key or alternative key in the Rules Dictionary");
            }
        }
    }

    public class Error
    {
        public Error(object sender, ValidationEventArgs args)
        {
            Sender = sender;
            Args = args;
        }

        public object Sender { get; }
        public ValidationEventArgs Args { get; }
    }
}
