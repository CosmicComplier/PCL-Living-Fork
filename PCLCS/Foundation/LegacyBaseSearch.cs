using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS;

public static class LegacyBaseSearch {
    public static double Similarity(string? source, string? query) {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query)) return 0;
        int qp = 0; double lenSum = 0;
        StringBuilder str = new StringBuilder(source!.Length).Append(source.Lower().Replace(" ", ""));
        query = query!.Lower().Replace(" ", "");
        int sourceLength = str.Length, queryLength = query.Length;
        if (queryLength == 0) return 0;
        while (qp < queryLength) {
            int sp = 0, lenMax = 0, spMax = 0, currentSourceLength = str.Length;
            while (sp < currentSourceLength) {
                int len = 0;
                while (checked(qp + len) < queryLength && checked(sp + len) < currentSourceLength && str[checked(sp + len)] == query[checked(qp + len)]) len = checked(len + 1);
                if (len > lenMax) { lenMax = len; spMax = sp; }
                sp = checked(sp + (len > 0 ? len : 1));
            }
            if (lenMax > 0) {
                str.Remove(spMax, lenMax);
                double incWeight = Math.Pow(1.4, checked(3 + lenMax)) - 3.6;
                incWeight *= 1 + 0.3 * Math.Max(0, checked(3 - Math.Abs(checked(qp - spMax))));
                lenSum += incWeight;
            }
            qp = checked(qp + (lenMax > 0 ? lenMax : 1));
        }
        return lenSum / queryLength * (3 / Math.Sqrt(checked(sourceLength + 15))) * (queryLength <= 2 ? checked(3 - queryLength) : 1);
    }
    public static double Weighted<TSource>(List<TSource> source, string query) where TSource : ILegacyBaseSearchSource {
        double totalWeight = 0, sum = 0;
        foreach (TSource pair in source) {
            if (pair.Aliases.Any()) sum += pair.Aliases.Max(alias => Similarity(alias, query)) * pair.Weight;
            totalWeight += pair.Weight;
        }
        return sum / totalWeight;
    }
    public static string FormatEntry<T>(double similarity, T item) => Conversions.ToString(Math.Round(similarity, 3)) + " - " + item!.ToString();
    public static List<TEntry> Search<TEntry, TSource>(List<TEntry> entries, string query, int maxBlurCount = 5, double minBlurSimilarity = 0.1)
        where TEntry : ILegacyBaseSearchEntry<TSource> where TSource : ILegacyBaseSearchSource {
        List<TEntry> results = new();
        if (!entries.Any()) return results;
        string[] parts = query.Split(" ", true);
        List<TEntry> candidates = new();
        foreach (TEntry entry in entries) {
            entry.Similarity = Weighted(entry.Sources, query);
            entry.AbsoluteRight = parts.All(part => entry.Sources.Any(source => source.Aliases.Any(alias => alias.Replace(" ", "").ContainsIgnoreCase(part))));
            if (entry.AbsoluteRight || entry.Similarity >= minBlurSimilarity) candidates.Add(entry);
        }
        candidates = LegacyBaseOperations.SortByComparison(candidates, (left, right) => left.AbsoluteRight != right.AbsoluteRight ? left.AbsoluteRight : left.Similarity > right.Similarity);
        int blurCount = 0;
        foreach (TEntry entry in candidates) {
            if (entry.AbsoluteRight) results.Add(entry);
            else { if (blurCount == maxBlurCount) break; results.Add(entry); blurCount = checked(blurCount + 1); }
        }
        return results;
    }
}
