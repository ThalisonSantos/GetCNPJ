using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace PackageValidation
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    var paths = Directory.GetFiles(args[0], "GetCNPJ.dll", SearchOption.AllDirectories);
                    ConsumerChecks.Require(paths.Length == 5, "Cinco assemblies obrigatórios");
                    foreach (var path in paths)
                    {
                        using var input = File.OpenRead(path);
                        using var pe = new PEReader(input);
                        var metadata = pe.GetMetadataReader();
                        var assembly = metadata.GetAssemblyDefinition();
                        ConsumerChecks.Require(metadata.GetString(assembly.Name) == "GetCNPJ" && assembly.Version == new Version(1, 0, 0, 0), "Identidade PE");
                        string? informational = null;
                        foreach (var handle in assembly.GetCustomAttributes())
                        {
                            var attribute = metadata.GetCustomAttribute(handle);
                            if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
                            var member = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                            if (member.Parent.Kind != HandleKind.TypeReference) continue;
                            var type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
                            if (metadata.GetString(type.Name) != "AssemblyInformationalVersionAttribute") continue;
                            var blob = metadata.GetBlobReader(attribute.Value);
                            ConsumerChecks.Require(blob.ReadUInt16() == 1, "Prolog atributo");
                            informational = blob.ReadSerializedString();
                        }
                        ConsumerChecks.Require(informational == "1.1.0+" + ConsumerChecks.Commit, "InformationalVersion PE");
                        Console.WriteLine(path + ": " + informational);
                    }
                    return 0;
                }
                await ConsumerChecks.RunAsync();
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
