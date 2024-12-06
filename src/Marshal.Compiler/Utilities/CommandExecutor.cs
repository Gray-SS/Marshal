using System.Diagnostics;

namespace Marshal.Compiler.Utilities;

public class CommandExecutor
{
    public static bool ExecuteCommand(string query)
    {
        try
        {
            // Créer une nouvelle instance de ProcessStartInfo
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd.exe" : "bash",
                Arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/c {query}" : $"-c \"{query}\"",
                RedirectStandardOutput = true, // Capture la sortie standard
                RedirectStandardError = true,  // Capture la sortie d'erreur
                UseShellExecute = false,       // Nécessaire pour rediriger la sortie
                CreateNoWindow = true          // Ne pas créer de fenêtre visible
            };

            using Process process = new Process { StartInfo = psi };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("Erreur :");
                Console.WriteLine(error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception : {ex.Message}");
            return false;
        }
    }
}
