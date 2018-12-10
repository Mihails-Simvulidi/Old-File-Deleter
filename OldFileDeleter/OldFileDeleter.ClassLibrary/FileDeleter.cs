using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace OldFileDeleter.ClassLibrary
{
    public class FileDeleter
    {
        private readonly long AvailableFreeSpaceTarget;
        private readonly DateTime DeleteEmptyDirectoriesCreatedBeforeUtc;
        private readonly DirectoryInfo[] Directories;
        private readonly DateTime DeleteFilesModifiedBeforeUtc;
        private readonly ILogger Logger;

        public FileDeleter(ILogger logger)
        {
            DateTime nowUtc = DateTime.UtcNow;
            Logger = logger;

            AppSettingReader<Settings> appSettingReader = new AppSettingReader<Settings>(Settings.Default);

            AvailableFreeSpaceTarget = appSettingReader.GetAppSetting(s => s.AvailableFreeSpaceTargetGB)
                .Select(v => (long)Math.Round(v * 1_000_000_000, MidpointRounding.AwayFromZero));

            DeleteEmptyDirectoriesCreatedBeforeUtc = appSettingReader.GetAppSetting(s => s.DeleteEmptyDirectoriesOlderThanHours)
                .Select(v => nowUtc.AddHours(-v));

            DeleteFilesModifiedBeforeUtc = appSettingReader.GetAppSetting(s => s.OnlyDeleteFilesOlderThanDays)
                .Select(v => nowUtc.AddDays(-v));

            Directories = appSettingReader.GetAppSetting(s => s.Directories)
                .IsNotNull()
                .Select(v => v.Cast<string>()
                    .Select(d => new DirectoryInfo(d))
                    .ToArray()
                );
        }

        public void CleanUpAllDrives()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                CleanUpDrive(drive);
            }
        }

        private void CleanUpDrive(DriveInfo drive)
        {
            try
            {
                Logger.LogInformation($"Cleaning up drive \"{drive.Name}\"...");

                DirectoryInfo[] driveDirectories = Directories
                    .Where(d => d.Root.FullName == drive.RootDirectory.FullName)
                    .ToArray();

                if (driveDirectories.Length == 0)
                {
                    Logger.LogInformation("No directories to clean up.");
                    return;
                }

                LogDirectories(driveDirectories);

                long availableFreeSpace = drive.AvailableFreeSpace;
                Logger.LogInformation($"Available free space: {availableFreeSpace:N0} bytes, target: {AvailableFreeSpaceTarget:N0} bytes.");

                if (availableFreeSpace >= AvailableFreeSpaceTarget)
                {
                    Logger.LogInformation($"There is enough free space - nothing to do.");
                    return;
                }

                DirectoryInMemory[] directoriesInMemory = driveDirectories
                    .Select(d => new DirectoryInMemory(d, Logger))
                    .ToArray();

                DeleteFiles(directoriesInMemory, ref availableFreeSpace);

                Logger.LogInformation($"Deleting empty subdirectories created before {DeleteEmptyDirectoriesCreatedBeforeUtc.ToLocalTime()}...");

                foreach (DirectoryInMemory directoryInMemory in directoriesInMemory)
                {
                    directoryInMemory.DeleteEmptySubDirectories(DeleteEmptyDirectoriesCreatedBeforeUtc);
                }

                Logger.LogInformation($"Drive \"{drive.Name}\" cleanup complete. Available free space: {availableFreeSpace:N0}.");
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Could not clean up drive \"{drive.Name}\"", e);
            }
        }

        private void LogDirectories(DirectoryInfo[] driveDirectories)
        {
            var message = "Directories to clean up:";

            foreach (DirectoryInfo directoryInfo in driveDirectories)
            {
                message += Environment.NewLine + directoryInfo.FullName;
            }

            Logger.LogInformation(message);
        }

        private void DeleteFiles(DirectoryInMemory[] driveDirectories, ref long availableFreeSpace)
        {
            Logger.LogInformation($"Deleting files modified before {DeleteFilesModifiedBeforeUtc.ToLocalTime()}...");

            IOrderedEnumerable<FileInMemory> filesToDelete = driveDirectories
                .SelectMany(d => d.GetAllFiles())
                .Where(f => f.LastWriteTimeUtc < DeleteFilesModifiedBeforeUtc)
                .OrderBy(f => f.LastWriteTimeUtc);

            foreach (FileInMemory file in filesToDelete)
            {
                try
                {
                    Logger.LogInformation($"Deleting file \"{file.FullName}\", date modified: {file.LastWriteTime}, size: {file.Length:N0} bytes...");
                    file.Delete();
                    availableFreeSpace += file.Length;
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Could not delete file \"{file.FullName}\"", e);
                }

                if (availableFreeSpace >= AvailableFreeSpaceTarget)
                {
                    return;
                }
            }
        }
    }
}
