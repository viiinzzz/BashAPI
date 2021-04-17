using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using static Xtensions.Std.Copyright;


namespace BashAPI
{
    /// <summary>
    /// The WebAPI program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The WebAPI entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
#if DEBUG
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                Console.WriteLine("ERROR: This API runs only on Unix Platform.");
#else
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                throw new ShowableException("This API runs only on Unix Platform.");
#endif

            Assembly ass = Assembly.GetExecutingAssembly();
            Console.WriteLine(splash);

            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// The WebAPI host builder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>

            Host.CreateDefaultBuilder(args)

                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                var env = hostingContext.HostingEnvironment;
                config.SetBasePath(
                    //Directory.GetCurrentDirectory());
                    env.ContentRootPath);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile("featured.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })

                .ConfigureLogging(logging => {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                })

                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                });

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();

        public static async Task<string> RunBash(CancellationToken cancel, string bash, string args, int timeoutSeconds = 5)
        {
            var ps = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = bash,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            ps.Start();
            string stdout = null;
            var run = Task.Run(async () => { stdout = await ps.StandardOutput.ReadToEndAsync(); });
            var cont = Task.Delay(timeoutSeconds *1000, cancel);
            await Task.WhenAny(run, cont);
            if (cancel.IsCancellationRequested || cont.IsCompleted)
            {
                ps.Kill();
                return null;
            }
            ps.WaitForExit();
            return ps.ExitCode == 0 ? stdout : null;
        }

    }


    /// <summary>
    /// An exception whose message can be safely shown to external party
    /// </summary>
    public class ShowableException : Exception
    {
        /// <summary>
        /// constructor with a message string
        /// </summary>
        /// <param name="message">string</param>
        public ShowableException(string message) : base(message) { }
    }


}
