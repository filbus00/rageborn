using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ARPG
{
    /// <summary>
    /// Reads and writes one save file with the rules from Docs/07-technical.md: an atomic write (a temporary file,
    /// then a rename, so a crash mid-write leaves the old save whole), the last <see cref="BackupCount"/> versions
    /// kept as backups, and on a bad read the backups tried in order. Takes its directory as a parameter, so tests
    /// run against a temporary one.
    /// </summary>
    public sealed class SaveStore
    {
        public const int BackupCount = 3;

        public SaveStore(string directory, string fileName = "character.json")
        {
            Directory = directory;
            MainPath = Path.Combine(directory, fileName);
        }

        public string Directory { get; }

        public string MainPath { get; }

        string TempPath => MainPath + ".tmp";

        /// <summary>Backup 1 is the newest.</summary>
        public string BackupPath(int number) => $"{MainPath}.bak{number}";

        /// <summary>The main file, then each backup, newest first.</summary>
        IEnumerable<string> Candidates()
        {
            yield return MainPath;
            for (var i = 1; i <= BackupCount; i++)
                yield return BackupPath(i);
        }

        public bool AnyExists()
        {
            foreach (var path in Candidates())
                if (File.Exists(path))
                    return true;
            return false;
        }

        public void Write(string contents)
        {
            System.IO.Directory.CreateDirectory(Directory);

            // Flush to the disk before the rename, or a power loss could leave a renamed but empty file.
            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(contents);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            if (!File.Exists(MainPath))
            {
                File.Move(TempPath, MainPath);
                return;
            }

            // Shift the backups down one, dropping the oldest, then swap the new file in and the old main to backup 1.
            if (File.Exists(BackupPath(BackupCount)))
                File.Delete(BackupPath(BackupCount));
            for (var i = BackupCount - 1; i >= 1; i--)
                if (File.Exists(BackupPath(i)))
                    File.Move(BackupPath(i), BackupPath(i + 1));
            File.Replace(TempPath, MainPath, BackupPath(1));
        }

        public readonly struct LoadResult
        {
            public LoadResult(SaveData data, string path, int backupNumber, IReadOnlyList<string> failures)
            {
                Data = data;
                Path = path;
                BackupNumber = backupNumber;
                Failures = failures;
            }

            /// <summary>The loaded save, or null when no file could be read.</summary>
            public SaveData Data { get; }

            /// <summary>The file it came from.</summary>
            public string Path { get; }

            /// <summary>0 for the main file, otherwise which backup it had to fall back to.</summary>
            public int BackupNumber { get; }

            /// <summary>Why each file tried before it was rejected.</summary>
            public IReadOnlyList<string> Failures { get; }

            public bool Loaded => Data != null;
        }

        /// <summary>
        /// Loads the main file, or failing that the newest backup that reads cleanly. When nothing exists the result is
        /// empty with no failures: a new character.
        /// </summary>
        public LoadResult Load()
        {
            var failures = new List<string>();
            return TryLoad<SaveData>(SaveCodec.TryParse, out var data, out var path, out var number, failures)
                ? new LoadResult(data, path, number, failures)
                : new LoadResult(null, null, 0, failures);
        }

        public delegate bool Parser<T>(string contents, out T value, out string error);

        /// <summary>
        /// The same fallback for any file kept by these rules (the account file's settings too): the main file, then
        /// each backup, newest first, until one parses. <paramref name="failures"/> gets why each one tried was
        /// rejected; nothing existing is not a failure.
        /// </summary>
        public bool TryLoad<T>(Parser<T> parse, out T value, out string path, out int backupNumber, List<string> failures)
        {
            var number = 0;
            foreach (var candidate in Candidates())
            {
                if (File.Exists(candidate))
                {
                    string error;
                    try
                    {
                        if (parse(File.ReadAllText(candidate, Encoding.UTF8), out value, out error))
                        {
                            path = candidate;
                            backupNumber = number;
                            return true;
                        }
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        error = e.Message;
                    }
                    failures?.Add($"{Path.GetFileName(candidate)}: {error}");
                }
                number++;
            }
            value = default;
            path = null;
            backupNumber = 0;
            return false;
        }

        /// <summary>
        /// Moves every save file aside, renamed with a suffix, so a new game does not write over saves that could not
        /// be read but might still be recovered by hand (for example one from a newer build).
        /// </summary>
        public void SetAsideAll(string suffix)
        {
            foreach (var path in Candidates())
                if (File.Exists(path))
                    File.Move(path, $"{path}.{suffix}");
        }

        public void DeleteAll()
        {
            foreach (var path in Candidates())
                if (File.Exists(path))
                    File.Delete(path);
            if (File.Exists(TempPath))
                File.Delete(TempPath);
        }
    }
}
