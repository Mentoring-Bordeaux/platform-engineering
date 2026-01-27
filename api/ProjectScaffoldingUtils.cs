using System.Diagnostics;
using System.Text.RegularExpressions;

public static class ProjectScaffoldingUtils
{
    public static void GenerateProjectByCli(FrameworkType framework, string targetDir)
    {
        string args = framework switch
        {
            FrameworkType.dotnet => "new webapi -n app",
            FrameworkType.React => " create vite app --template react-ts --yes --no-interactive",
            FrameworkType.Vue => "create vite appi --template vue --yes --no-interactive",
            FrameworkType.Nuxt => "nuxi init app",
            FrameworkType.JavaSpring => "init app --build=maven --java-version=17",
            _ => throw new ArgumentException("Unsupported framework")
        };

        string executable = framework switch
        {
            FrameworkType.dotnet => "dotnet",
            FrameworkType.React or FrameworkType.Vue => "pnpm",
            FrameworkType.Nuxt => "npx",
            FrameworkType.JavaSpring => "spring",
            _ => throw new ArgumentException("Unsupported framework")
        };

        RunCommand(executable, args, targetDir);
    }

    private static void RunCommand(string fileName, string args, string workingDir)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Command failed: {fileName} {args}\n{stderr}");
    }
    public static string ApplyPulumiParameters(
        string content,
        Dictionary<string, string> parameters)
    {
        foreach (var kv in parameters)
        {
            string value = kv.Value.Replace("\"", "\\\"");

            content = Regex.Replace(
                content,
                $@"config\.require\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)",
                $"\"{value}\"");

            content = Regex.Replace(
                content,
                $@"config\.get\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)\s*(\|\|\s*[""'][^""']*[""'])?",
                $"\"{value}\"");

            content = Regex.Replace(
                content,
                $@"config\.getSecret\(\s*[""']{Regex.Escape(kv.Key)}[""']\s*\)",
                $"\"{value}\"");
        }

        return content;
    }
}
