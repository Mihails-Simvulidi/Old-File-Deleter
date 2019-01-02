using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OldFileDeleter.ClassLibrary;
using Serilog;
using System;

namespace OldFileDeleter.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File($"{AppDomain.CurrentDomain.BaseDirectory}logs\\{AppDomain.CurrentDomain.FriendlyName}-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            using (ServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging(l => l.AddSerilog(dispose: true))
                .BuildServiceProvider())
            {
                ILogger<Program> logger = serviceProvider.GetService<ILoggerFactory>()
                    .CreateLogger<Program>();

                try
                {
                    logger.LogInformation("Starting application.");
                    FileDeleter fileDeleter = new FileDeleter(logger);
                    fileDeleter.CleanUpAllDrives();
                    logger.LogInformation("Application completed succesfully.");
                }
                catch (Exception e)
                {
                    logger.LogCritical("Application failed", e);
                }
            }

#if DEBUG
            Console.WriteLine("Press \"Enter\" to exit.");
            Console.ReadLine();
#endif
        }
    }
}
