using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MacroMate.MacroTree;
using MacroMate.Serialization.V1;

namespace MacroMate.Serialization;

public class SaveManager {
    public FileInfo MacroDataFile {
        get => new FileInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "MacroMate.xml"));
    }

    public FileInfo MacroDataSaveAttemptFile {
        get => new FileInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "MacroMate.saveAttempt.xml"));
    }

    public DirectoryInfo MacroBackupFolder {
        get => new DirectoryInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "Backups"));
    }

    /// The maximum number of timed backup files to keep.
    ///
    /// When writing a new timed backup file the oldest one will be deleted
    /// if we are over this limit.
    public int MaxTimedBackupFiles { get; set; } = 3;
    public int MinutesBetweenTimedBackups { get; set; } = 60;
    private string timedBackupPostabmle = "timedBackup";

    public IEnumerable<FileInfo> GetCurrentTimedBackupFiles() {
        if (!MacroBackupFolder.Exists) { return new List<FileInfo>(); }

        return MacroBackupFolder.EnumerateFiles().Where(file => file.Name.Contains(timedBackupPostabmle));
    }

    public List<FileInfo> ListBackups() {
        if (!MacroBackupFolder.Exists) { return new List<FileInfo>(); }

        return MacroBackupFolder
            .EnumerateFiles("*.xml")
            .OrderByDescending(file => file.LastWriteTime)
            .ToList();
    }

    public void Save(MateNode root) {
        SaveTimedBackup();

        // Write to an empty file then copy it over the existing one.
        //
        // This helps prevent issues when the save fails, since we won't
        // corrup the users existing save file.
        MacroMateSerializerV1.Write(root, MacroDataSaveAttemptFile);
        File.Move(MacroDataSaveAttemptFile.FullName, MacroDataFile.FullName, overwrite: true);
    }

    public void SaveTimedBackup() {
        if (!CanBackup()) { return; }

        var currentTimedBackupFiles = GetCurrentTimedBackupFiles().ToList();
        var lastBackupTime = currentTimedBackupFiles.Count > 0
            ? currentTimedBackupFiles.Max(file => file.CreationTime)
            : DateTime.MinValue;

        var timeSinceLastBackup = DateTime.Now - lastBackupTime;
        if (timeSinceLastBackup > TimeSpan.FromMinutes(MinutesBetweenTimedBackups)) {
            var backupDate = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'0'ss");
            SaveBackup($"{timedBackupPostabmle}-{backupDate}");
        }

        // Reduce the number of timed backup files until we're back to the max
        var oldestTimedBackupFilesBeyondMax = GetCurrentTimedBackupFiles()
            .OrderByDescending(file => file.CreationTime)
            .Skip(MaxTimedBackupFiles);
        foreach (var file in oldestTimedBackupFilesBeyondMax) {
            file.Delete();
        }
    }

    public FileInfo? SaveBackup() {
        var backupDate = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss");
        return SaveBackup(backupDate);
    }

    public FileInfo? SaveBackup(string postamble) {
        if (!CanBackup()) { return null; }

        // Write the backup
        MacroBackupFolder.Create();

        var macroFileName = Path.GetFileNameWithoutExtension(MacroDataFile.FullName);
        var backupFile = Path.Combine(MacroBackupFolder.FullName, $"{macroFileName}-{postamble}.xml");
        File.Copy(MacroDataFile.FullName, backupFile);
        return new FileInfo(backupFile);
    }

    /// We can't backup if there's nothing to backup
    public bool CanBackup() => MacroDataFile.Exists;

    public bool CanLoad() => MacroDataFile.Exists;
    public MateNode? Load() => LoadFrom(MacroDataFile);

    public MateNode? LoadFrom(FileInfo file) {
        if (!file.Exists) { return null; }
        return MacroMateSerializerV1.Read(file);
    }
}
