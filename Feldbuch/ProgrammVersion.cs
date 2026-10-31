using System.Reflection;

namespace Feldbuch;

/// <summary>Versions- und Buildangaben aus den Metadaten der Programmdatei.</summary>
public static class ProgrammVersion
{
    private static readonly Assembly Programm = typeof(ProgrammVersion).Assembly;

    /// <summary>Version ohne angehängten Commit-Hash, z. B. "1.9.9".</summary>
    public static string Version =>
        (Programm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?")
            .Split('+')[0];

    /// <summary>Stand des Buildzählers beim Kompilieren (siehe BuildNummer.txt).</summary>
    public static string BuildNummer =>
        Programm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildNummer")?.Value ?? "?";
}
