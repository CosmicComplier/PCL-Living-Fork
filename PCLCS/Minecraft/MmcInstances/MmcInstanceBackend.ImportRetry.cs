namespace PCLCS;

// Original LF host implementation; repository LICENCE applies.
public sealed partial class MmcInstanceBackend<TNetFile> where TNetFile : notnull {
    /// <summary>A fresh identity and different destination; never resumes or deletes an unproven/partial old payload.</summary>
    public async Task<MmcInstanceBackendResult> RetryInitialImportAsync(MmcImportRecoveryEntry preview, MmcInstanceImportRequest request,
        CancellationToken cancellationToken = default, IProgress<MmcInstanceBackendProgress>? progress = null) {
        MmcOwnedImportReservation? old = null;
        try {
            ArgumentNullException.ThrowIfNull(preview); ArgumentNullException.ThrowIfNull(request);
            if (!preview.CanRetry || !MmcDurableFiles.IsHash(preview.PackageSha256) || preview.PreviewFingerprint is null) throw new IOException("此残留没有可核对的原包摘要。");
            string target = MmcCanonicalPaths.ForAdmission(MmcInstancePaths.ValidateVersionRoot(request.VersionRoot));
            if (new[] { preview.TemporaryRoot, preview.FinalRoot }.Where(path => path is not null).Any(path => string.Equals(target, Path.TrimEndingDirectorySeparator(Path.GetFullPath(path!)), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
                throw new IOException("重新导入必须使用不同名称，保留旧现场和其原目标。");
            if (!string.IsNullOrEmpty(preview.OperationDirectory)) {
                var operation = MmcImportOperation.Read(preview.OperationDirectory); old = MmcOwnedImportReservation.Reopen(operation);
                if (MmcImportRecovery.Fingerprint(operation) != preview.PreviewFingerprint) throw new IOException("原导入残留已变化，请重新预览。");
            } else {
                var fresh = MmcImportRecovery.Read(preview.MinecraftRoot).Entries.SingleOrDefault(entry => entry.TemporaryRoot == preview.TemporaryRoot && entry.State == MmcImportRecoveryState.LegacyUnproven);
                if (fresh is null || fresh.PreviewFingerprint != preview.PreviewFingerprint || fresh.PackageSha256 != preview.PackageSha256) throw new IOException("旧开发版残留已变化。");
            }
            using var source = await MmcArchiveSourceReader.ReadAsync(request.ArchivePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (source.PackageSha256 != preview.PackageSha256) throw new IOException("必须选择与原预留 SHA256 完全相同的 MMC 原包。");
            return await ImportPreparedAsync(source, request, cancellationToken, progress).ConfigureAwait(false);
        } catch (OperationCanceledException error) { return new(MmcInstanceBackendStatus.Cancelled, Failure: error.Message); }
        catch (Exception error) when (IsExpectedFailure(error)) { return new(MmcInstanceBackendStatus.Rejected, Failure: error.Message); }
        finally { old?.Dispose(); }
    }
}
