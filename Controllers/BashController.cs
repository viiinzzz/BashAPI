using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BashApi.Controllers
{
    /// <summary>
    /// This controller has only one POST method to bridge bash command
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class BashController : ControllerBase
    {

        private readonly ILogger<BashController> _logger;

        public BashController(ILogger<BashController> logger)
        {
            _logger = logger;
        }

        public int FILENAME_MAX_LEN { get; set; } = 40;
        public int OUTPUT_MAX_LEN { get; set; } = 40;
        public int SIZE_MAX_INPUT { get; set; } = 5 * 1024 * 1024;
        public int SIZE_MAX_OUTPUT { get; set; } = 100 * 1024 * 1024;


        /// <summary>
        /// Execute command via bash with arguments
        /// </summary>
        /// <param name="file">input file</param>
        /// <param name="command">command and arguments (must include "input" and "output")</param>
        /// <param name="outputname">output file name</param>
        /// <param name="timeoutseconds">seconds before processing gets interrupted</param>
        /// <returns>output file</returns>
        /// <response code="200">file processing succeeded.</response>
        /// <response code="400">file processing failed.</response>
        ///<remarks>
        ///  <h2>Example</h2>
        ///     <ul>
        ///     <li>file: test.txt</li>
        ///     <li>command: gzip -c "input" > "output"</li>
        ///     <li>outputname: test.gz</li>
        ///     <li>timeoutseconds: 180</li>
        ///     </ul>
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> Post(CancellationToken cancel,
            IFormFile file,
            string command = "gzip -c \"input\" > \"output\"",
            string outputname = "test.gz",
            int timeoutseconds = 3*60)
        {
            string input = null, output = null;

            #region cleanup
            Action<bool, bool> del = (i, o) =>
            {
                try
                {
                    if (i && input != null && System.IO.File.Exists(input)) System.IO.File.Delete(input);
                }
                catch (Exception e)
                {
                    _logger.LogError($"cannot delete input: {input}\n{e.Message}");
                }
                try
                {
                    if (o && output != null && System.IO.File.Exists(output)) System.IO.File.Delete(output);
                }
                catch (Exception e)
                {
                    _logger.LogError($"cannot delete output: {output}\n{e.Message}");
                }
            };
            #endregion cleanup

            try
            {
                #region input
                _logger.LogInformation($"POST received file name={file?.FileName} size={file?.Length}");

                if (file == null || file?.Length == 0)
                    return ValidationProblem("invalid file size (0)");
                if (file.Length > SIZE_MAX_INPUT)
                    return ValidationProblem($"invalid file size >{SIZE_MAX_INPUT}");
                if (string.IsNullOrWhiteSpace(file.FileName))
                    return ValidationProblem("invalid file name ''");
                if (file.FileName.Length > FILENAME_MAX_LEN)
                    return ValidationProblem($"invalid file name >{FILENAME_MAX_LEN}");
                if (string.IsNullOrWhiteSpace(outputname))
                    return ValidationProblem("invalid output name ''");
                if (outputname.Length > FILENAME_MAX_LEN)
                    return ValidationProblem($"invalid output name >{FILENAME_MAX_LEN}");

                input = Path.GetTempFileName();
                output = Path.GetTempFileName();
                {
                    using var sinput = new FileStream(input, FileMode.Create, FileAccess.Write, FileShare.None);
                    await file.CopyToAsync(sinput, cancel);
                }
                if (!System.IO.File.Exists(input) || new FileInfo(input).Length != file.Length)
                    throw new Exception($"input copy failed: {input}");
                else
                    _logger.LogInformation($"INPUT={input}");

                #endregion input

                #region process
                if (!command.Contains("\"input\""))
                    return ValidationProblem("invalid args (must contains \"input\")");
                if (!command.Contains("\"output\""))
                    return ValidationProblem("invalid args (must contains \"output\")");

                var command2 = command
                    .Replace("\"input\"", $"\"{input}\"")
                    .Replace("\"output\"", $"\"{output}\"")
                    .Replace("\"", "\\\"");

                var ca = $"-c \"{command2}\"";

                _logger.LogInformation($"OUTPUT={output}");

                var ps = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = ca,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                _logger.LogInformation($"RUN {ps.StartInfo.FileName} {ps.StartInfo.Arguments}");
                ps.Start();
                string stdout = null;
                var run = Task.Run(async () => { stdout = await ps.StandardOutput.ReadToEndAsync(); });
                var cont = Task.Delay(timeoutseconds * 1000, cancel);
                await Task.WhenAny(run, cont);
                if (cancel.IsCancellationRequested)
                    _logger.LogInformation($"RUN cancelled.");
                if (cont.IsCompleted)
                    _logger.LogInformation($"RUN timeout.");
                if (cancel.IsCancellationRequested || cont.IsCompleted) ps.Kill();
                ps.WaitForExit();
                _logger.LogInformation($"RUN {(ps.ExitCode != 0 ? "in" : "")}complete.");
                _logger.LogInformation($"RESULT {stdout}");
                if (ps.ExitCode != 0)
                    throw new Exception($"process exited with error code ({ps.ExitCode})");
                del(true, false);
                #endregion process

                #region output
                if (!System.IO.File.Exists(output) || new FileInfo(output).Length == 0)
                    throw new Exception($"no output found.");
                _logger.LogInformation($"OUTPUT={output} size={new FileInfo(output).Length}");
                var soutput = new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return File(soutput, "application/octet-stream", outputname);
                #endregion output
            }

            #region error
            catch (Exception e)
            {
                _logger.LogError($"{e.Message}");
                del(true, true);
                return BadRequest();
            }
            #endregion error
        }
    }
}
