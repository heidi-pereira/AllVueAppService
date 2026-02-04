#nullable enable

using System.Diagnostics;
using System.Threading.Tasks;

namespace BrandVue.SourceData.Utils
{
    public class NpmTaskHelper
    {
        public static void RunTask(string task)
        {
            RunNpmTask(task);
        }

        public static async Task RunTaskAsync(string task)
        {
            await Task.Run(() => RunNpmTask(task));
        }

        /// <summary>
        /// This method will only run in debug mode and will kick off the npm process to build the api.
        /// </summary>
        [Conditional("DEBUG")]
        private static void RunNpmTask(string task)
        {
            try
            {
                LogMessage($""" "npm run {task}" running...""");
                using var npmCmdProcess = new Process();
                npmCmdProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c npm run {task}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var stdOutComplete = new TaskCompletionSource<object?>();
                var stdErrComplete = new TaskCompletionSource<object?>();

                npmCmdProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        LogMessage($""" "npm run {task}" output...""");
                        LogMessage(e.Data);
                    }
                    else
                        stdOutComplete.SetResult(null);
                };
                npmCmdProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        LogMessage($""" "npm run {task}" error...""");
                        LogMessage(e.Data);
                    }
                    else
                        stdErrComplete.SetResult(null);
                };

                npmCmdProcess.Start();
                npmCmdProcess.BeginOutputReadLine();
                npmCmdProcess.BeginErrorReadLine();
                Task.Run(async () => await Task.WhenAll(npmCmdProcess.WaitForExitAsync(), stdOutComplete.Task, stdErrComplete.Task)).Wait();
            }
            catch (Exception ex)
            {
                LogMessage($""" "npm run {task}" exception...""");
                LogMessage(ex.Message);
            }
        }

        /// <summary>
        /// Log to console and the debug output.  The console output seemed to have limited capacity and if there
        /// was too much output from other places then if would lose the beginning, which is where this npm output
        /// tends to be.  So, this method logs to the debug output as well.
        /// </summary>
        /// <param name="message"></param>
        private static void LogMessage(string message)
        {
            var trimmedMessage = message.Trim();
            Console.WriteLine(trimmedMessage);
            Debug.WriteLine(trimmedMessage);
        }
    }
}
