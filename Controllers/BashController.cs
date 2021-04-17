#region using
using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using static BashAPI.Program;
using Xtensions.Std;
using System.Linq;
#endregion using

namespace BashAPI.Controllers
{
#if DEBUG
    /// <summary>
    /// This controller has only one POST method to bridge bash command
    /// </summary>
#endif
    [ApiController]
    [Route("[controller]")]
    public class BashController : ControllerBase
    {
        #region variables
        /// <summary>
        /// input file name maximum length (characters)
        /// </summary>
        public int FILENAME_MAX_LEN { get; set; } = 40;
        /// <summary>
        /// returned file name maximum length (characters)
        /// </summary>
        public int OUTPUT_MAX_LEN { get; set; } = 40;
        /// <summary>
        /// input file maximum size (in bytes)
        /// </summary>
        public int SIZE_MAX_INPUT { get; set; } = 5 * 1024 * 1024;
        /// <summary>
        /// returned file maximim size (in bytes)
        /// </summary>
        public int SIZE_MAX_OUTPUT { get; set; } = 100 * 1024 * 1024;

        private readonly ILogger<BashController> _logger;
        private readonly string _bashCommand;
#if DEBUG
#else
        private readonly string _bashCommandVersion;
        private readonly string _featuredDescription;
        private readonly int _featuredTimeoutSeconds;
        private readonly string _featuredCommand;
        private readonly string _featuredCommandVersion;
        private readonly string _featuredOutputName;
#endif
        private readonly IConfiguration _config;
        #endregion variables

        /// <summary>
        /// Linux Bash Controller
        /// </summary>
        /// <param name="logger">logger</param>
        /// <param name="config">configuration</param>
        public BashController(ILogger<BashController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            #region host check
            var bconf = config.GetSection("bash"); //config["bash:command"]
            _bashCommand = bconf["command"]?.Trim();
            if (string.IsNullOrWhiteSpace(_bashCommand)) _bashCommand = "/bin/bash";

#if DEBUG
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                    throw new ShowableException("This API runs only on Unix Platform.");
#else
            try
            {
                _bashCommandVersion = RunBash(CancellationToken.None, _bashCommand, "--version", 1).Result;
                _logger.LogDebug(@$"bash command: {_bashCommand} --version
  Version={(_bashCommandVersion?.Length == 0 ? "not found" : _bashCommandVersion)}");
                if (!_bashCommandVersion?.ToLower()?.StartsWith("gnu bash") ?? false)
                    throw new Exception("non gnu bash");
            } catch(Exception e)
            {
                throw new ShowableException($"bash command not available: {_bashCommand}");
            }
            if (!int.TryParse(bconf["featuredTimeoutSeconds"]?.Trim(), out _featuredTimeoutSeconds))
                _featuredTimeoutSeconds = 3 * 60;
            if (_featuredTimeoutSeconds <= 0)
                throw new Exception("missing appsettings bash.featuredTimeoutSeconds");

            _featuredDescription = bconf["featuredDescription"]?.Trim();
            if (string.IsNullOrWhiteSpace(_featuredDescription))
                throw new Exception("missing appsettings bash.Description");

            _featuredCommand = bconf["featuredCommand"]?.Trim();
            if (string.IsNullOrWhiteSpace(_featuredCommand))
                throw new Exception("missing appsettings bash.featuredCommand");
            var _featuredExecutable = _featuredCommand?.Split(' ')?.FirstOrDefault();
            _featuredCommandVersion = RunBash(CancellationToken.None, _bashCommand, $"-c \"{ _featuredExecutable} --version\"", 1).Result;
            if (string.IsNullOrWhiteSpace(_featuredCommandVersion))
                throw new Exception($"featured program not available: {_featuredExecutable}\n{_featuredCommandVersion}");

            _featuredOutputName = bconf["featuredOutputName"]?.Trim();
            if (string.IsNullOrWhiteSpace(_featuredOutputName))
                throw new Exception("missing appsettings bash.featuredOutputName");

            _logger.LogDebug(@$"featured program: {_featuredCommand}
  Description={_featuredDescription}
  Version={ _featuredCommandVersion}
  TimeoutSeconds={_featuredTimeoutSeconds}
  OutputName={_featuredOutputName}");
#endif

            #endregion host check
        }

        #region Execute doc
#if DEBUG
        /// <summary>
        /// Execute command via bash with arguments
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <param name="file">input file or zip file</param>
        /// <param name="command">command and arguments (must include "input" and "output")</param>
        /// <param name="outputname">output file name</param>
        /// <param name="timeoutseconds">seconds before processing gets interrupted (-1 for default value 180)</param>
        /// <param name="inputname">(optional)zip file entryname to use as input file</param>
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
#else
        /// <summary>
        /// Execute featured application
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <param name="file">input file or zip file</param>
        /// <param name="inputname">(optional)zip file entryname to use as input file</param>
        /// <returns>output file</returns>
        /// <response code="200">file processing succeeded.</response>
        /// <response code="400">file processing failed.</response>
#endif
        #endregion Execute doc
#if DEBUG
        [Route("/exec")]
        [HttpPost]
        public async Task<IActionResult> Execute(
            CancellationToken cancel, IFormFile file,
            string outputname = "test.gz",
            string command = "gzip -c 'input' > 'output'",
            int timeoutseconds = -1,
            string inputname = ""
            ) => await Execute_(
                cancel, file,
                outputname, command, timeoutseconds,
                inputname);
#else
        [Route("/feat")]
        [HttpPost]
        public async Task<IActionResult> Featured(
            CancellationToken cancel, IFormFile file,
            string inputname = null
            )=> await Run_(
                cancel, file,
                _featuredOutputName, _featuredCommand, _featuredTimeoutSeconds,
                inputname);
#endif

#if DEBUG
        [Route("/feat")]
        [HttpPost]
        public async Task<IActionResult> Featured(
            CancellationToken cancel, IFormFile file, string inputname = null)
        {
            var bconf = _config.GetSection("bash");

            if (!int.TryParse(bconf["featuredTimeoutSeconds"]?.Trim(), out var _featuredTimeoutSeconds)
                || _featuredTimeoutSeconds <= 0) _featuredTimeoutSeconds = 3 * 60;

            var _featuredDescription = bconf["featuredDescription"]?.Trim();
            if (string.IsNullOrWhiteSpace(_featuredDescription))
                throw new Exception("missing appsettings bash.Description");

            var _featuredCommand = bconf["featuredCommand"]?.Trim() ?? "openssl base64 -in 'input' -out 'output'";
          
            var _featuredOutputName = bconf["featuredOutputName"]?.Trim() ?? "output.enc";

            return await Execute_(cancel, file,
                _featuredOutputName, _featuredCommand, _featuredTimeoutSeconds, inputname);
        }
#endif


        private async Task<IActionResult> Execute_(
            CancellationToken cancel, IFormFile file, string outputname,
            string command, int timeoutseconds, string inputname)
        {
            if (timeoutseconds < 0) timeoutseconds = 3 * 60;
            string input = null, workdir = null, output = null;

            #region cleanup
            Action<bool, bool> del = (i, o) =>
            {
                try
                {
                    if (i && !o && input != null && System.IO.File.Exists(input)) System.IO.File.Delete(input);
                }
                catch (Exception e)
                {
                    _logger.LogError($"cannot delete input: {input}\n{e.Message}");
                }
                try
                {
                    if (o && !i && output != null && System.IO.File.Exists(output)) System.IO.File.Delete(output);
                }
                catch (Exception e)
                {
                    _logger.LogError($"cannot delete output: {output}\n{e.Message}");
                }
                try
                {
                    if (i && o && workdir != null && Directory.Exists(workdir)) Directory.Delete(workdir, true);
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
                if (outputname.Length > OUTPUT_MAX_LEN)
                    return ValidationProblem($"invalid output name >{FILENAME_MAX_LEN}");

                input = Path.GetTempFileName();
                workdir = input + ".dir";
                System.IO.File.Delete(input);
                if (System.IO.File.Exists(input))
                    throw new Exception($"temp file delete failed: {input}");
                Directory.CreateDirectory(workdir);
                if (!Directory.Exists(workdir))
                    throw new Exception($"input directory creation failed: {workdir}");
                input = Path.Combine(workdir, "input");

                output = Path.Combine(workdir, "output");
                {
                    using var sinput = new FileStream(input, FileMode.Create, FileAccess.Write, FileShare.None);
                    await file.CopyToAsync(sinput, cancel);
                }
                if (!System.IO.File.Exists(input) || new FileInfo(input).Length != file.Length)
                    throw new Exception($"input copy failed: {input}");
                else
                    _logger.LogInformation($"INPUT={input}");

                if (file.FileName.ToLower().EndsWith(".zip"))
                {
                    try
                    {
                        var zip = ZipFile.Open(input, ZipArchiveMode.Read);
                        zip.ExtractToDirectory(workdir);
                    }
                    catch (Exception e)
                    {
                        _logger.LogInformation($"INPUT={input} invalid zip file\n{e.Message}");
                        throw new ShowableException("invalid zip file.");
                    }
                    if (string.IsNullOrEmpty(inputname))
                        inputname = Path.GetFileNameWithoutExtension(file.FileName);
                    input = Path.Combine(workdir, inputname);
                    if (!System.IO.File.Exists(input) || new FileInfo(input).Length == 0)
                        throw new Exception($"entry not found: {inputname}");
                }
                #endregion input

                #region process
                if (command.Contains("$input"))
                    command = command.Replace("$input", $"{input.EscapeBashFileName()}");
                else if (command.Contains("$(input)"))
                    command = command.Replace("$(input)", $"{input.EscapeBashFileName()}");
                else
                    return ValidationProblem("invalid args (must contains $input)");

                if (command.Contains("$output"))
                    command = command.Replace("$output", $"{output.EscapeBashFileName()}");
                else if (command.Contains("$(output)"))
                    command = command.Replace("$(output)", $"{output.EscapeBashFileName()}");

                command = command.Replace("\"", "\\\"");

                var ca = $"-c \"{command}\"";

                _logger.LogInformation($"OUTPUT={output}");

                var ps = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _bashCommand,
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
                    throw new ShowableException($"process exited with error code ({ps.ExitCode})\n{stdout}");
                //del(true, false);
                #endregion process

                #region output
                if (!System.IO.File.Exists(output) || new FileInfo(output).Length == 0)
                    throw new Exception($"output not found: {output}");
                _logger.LogInformation($"OUTPUT={output} size={new FileInfo(output).Length}");
                var soutput = new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return File(soutput, "application/octet-stream", outputname);
                #endregion output
            }

            #region error
            catch (Exception e)
            {
                _logger.LogError($"{e.Message}");
                //del(true, true);
                if (e is ShowableException)
                    return BadRequest(e.Message);
                else
                    return BadRequest();
            }
            finally
            {
                del(true, true);
            }
            #endregion error
        }






        /// <summary>
        /// Return Environment string Develoment / Staging / Production
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/envir")]
        [HttpGet]
        public async Task<string> envir(CancellationToken cancel)
        {
            var ps = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _bashCommand,
                    Arguments = $"-c \"echo $ASPNETCORE_ENVIRONMENT\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            _logger.LogInformation($"RUN {ps.StartInfo.FileName} {ps.StartInfo.Arguments}");
            ps.Start();
            string stdout = null;
            var run = Task.Run(async () => { stdout = await ps.StandardOutput.ReadToEndAsync(); });
            var cont = Task.Delay(1000, cancel);
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
                throw new ShowableException($"process exited with error code ({ps.ExitCode})\n{stdout}");
            return stdout.TrimEnd();
        }


    }
}
