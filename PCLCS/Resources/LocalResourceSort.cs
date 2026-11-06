using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PCLCS;

public enum LocalResourceSortOrder { Default, AddedNewest, AddedOldest }

/// <summary>File/directory creation time estimates local addition, not installation history.</summary>
public static class LocalResourceSort {
    public static IReadOnlyList<T> Order<T>(IEnumerable<T> source, LocalResourceSortOrder order, Func<T, FileSystemInfo> file) {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (file is null) throw new ArgumentNullException(nameof(file));
        if (order is not (LocalResourceSortOrder.Default or LocalResourceSortOrder.AddedNewest or LocalResourceSortOrder.AddedOldest))
            throw new ArgumentOutOfRangeException(nameof(order));
        // Default also preserves fuzzy-search relevance and performs no file lookup.
        if (order == LocalResourceSortOrder.Default) return source.ToArray();
        Entry<T>[] entries = source.Select((value, index) => Read(value, index, file)).ToArray();
        return ByTime(entries, order == LocalResourceSortOrder.AddedNewest, entry => entry.Time, entry => entry.Name, entry => entry.Path)
            .Select(entry => entry.Value).ToArray();
    }

    public static IReadOnlyList<T> ByTime<T>(IEnumerable<T> source, bool newestFirst, Func<T, DateTime?> timestamp, Func<T, string> name, Func<T, string> path) {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (timestamp is null) throw new ArgumentNullException(nameof(timestamp));
        if (name is null) throw new ArgumentNullException(nameof(name));
        if (path is null) throw new ArgumentNullException(nameof(path));
        // Snapshot each key once. Sorting neither mutates the source nor re-reads metadata during comparisons.
        var entries = source.Select((value, index) => new Entry<T> {
            Value = value, Index = index, Time = timestamp(value), Name = name(value) ?? "", Path = path(value) ?? ""
        }).ToArray();
        IOrderedEnumerable<Entry<T>> knownFirst = entries.OrderBy(entry => !entry.Time.HasValue);
        IOrderedEnumerable<Entry<T>> timed = newestFirst
            ? knownFirst.ThenByDescending(entry => entry.Time)
            : knownFirst.ThenBy(entry => entry.Time);
        return timed.ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Path, StringComparer.Ordinal)
            .ThenBy(entry => entry.Index).Select(entry => entry.Value).ToArray();
    }

    private static Entry<T> Read<T>(T value, int index, Func<T, FileSystemInfo> file) {
        Entry<T> entry = new() { Value = value, Index = index };
        try {
            FileSystemInfo info = file(value);
            if (info is null) return entry;
            entry.Name = info.Name; entry.Path = info.FullName;
            info.Refresh();
            if (info.Exists) {
                DateTime created = info.CreationTimeUtc;
                // The Windows missing-file sentinel can be returned after a concurrent removal.
                if (created > DateTime.FromFileTimeUtc(0)) entry.Time = created;
            }
        } catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return entry;
    }

    private sealed class Entry<T> {
        public T Value = default!;
        public int Index;
        public DateTime? Time;
        public string Name = "", Path = "";
    }
}
