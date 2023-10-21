using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MacroMate.Extensions.Dotnet;
using MacroMate.MacroTree;
using MacroMate.Serialization.V1;

namespace MacroMate.Serialization;

public class SaveManager {
    public FileInfo MacroDataFile {
        get => new FileInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "MacroMate.xml"));
    }

    public FileInfo MacroLastBackupTimeFile {
        get => new FileInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "MacroMateLastBackup.txt"));
    }

    public DirectoryInfo MacroBackupFolder {
        get => new DirectoryInfo(Path.Combine(Env.PluginInterface.ConfigDirectory.FullName, "Backups"));
    }

    public void Save(MateNode root) {
        SaveTimedBackup();
        SaveManagerV1.Write(root, MacroDataFile);
    }

    public FileInfo SaveBackup() {
        var backupDate = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
        return SaveBackup(backupDate);
    }

    public FileInfo SaveBackup(string postamble) {
        // Write the backup
        MacroBackupFolder.Create();

        var macroFileName = Path.GetFileNameWithoutExtension(MacroDataFile.FullName);
        var backupFile = Path.Combine(MacroBackupFolder.FullName, $"{macroFileName}-{postamble}.xml");
        File.Copy(MacroDataFile.FullName, backupFile);
        return new FileInfo(backupFile);
    }

    public void SaveTimedBackup() {
        if (!MacroDataFile.Exists) { return; }

        DateTime lastBackupTime = DateTime.MinValue;
        if (MacroLastBackupTimeFile.Exists) {
            using (var lastBackupTimeFile = MacroLastBackupTimeFile.OpenText()) {
                lastBackupTime = lastBackupTimeFile.ReadToEnd().ToDateTimeOrNull() ?? DateTime.MinValue;
            }
        }

        var timeSinceLastBackup = DateTime.Now - lastBackupTime;
        if (timeSinceLastBackup.TotalHours > 1) {
            var backupFile = SaveBackup();

            // Write the last backup time
            using (var lastBackupTimeFile = MacroLastBackupTimeFile.Create())
            using (var lastBackupTimeWriter = new StreamWriter(lastBackupTimeFile)) {
                lastBackupTimeWriter.Write(backupFile.LastWriteTime);
            }
        }
    }

    public List<FileInfo> ListBackups() {
        return MacroBackupFolder
            .EnumerateFiles("*.xml")
            .OrderByDescending(file => file.LastWriteTime)
            .ToList();
    }

    public MateNode? Load() => LoadFrom(MacroDataFile);

    public MateNode? LoadFrom(FileInfo file) {
        if (!file.Exists) { return null; }
        return SaveManagerV1.Read(file);
    }
}
