using System;
using PCLCS;

namespace PCL
{
    internal sealed class ToolboxDownloadSession : LegacyToolboxDownloadSession
    {
        private ToolboxDownloadSession(string Target) : base(Target)
        {
        }
        internal static new Uri ParseUrl(string Value)
        {
            return LegacyToolboxDownloadSession.ParseUrl(Value);
        }
        internal static new string ValidateName(string Value)
        {
            return LegacyToolboxDownloadSession.ValidateName(Value);
        }
        internal static new string SuggestName(string Value)
        {
            return LegacyToolboxDownloadSession.SuggestName(Value);
        }
        internal static new string ResolveTarget(string Folder, string Name)
        {
            return LegacyToolboxDownloadSession.ResolveTarget(Folder, Name);
        }
        internal static new ToolboxDownloadSession Create(string Folder, string Name)
        {
            return Create(Folder, Name, Target => new ToolboxDownloadSession(Target));
        }
        internal new void Commit(bool AllowOverwrite, Func<bool> ShouldCancel)
        {
            base.Commit(AllowOverwrite, ShouldCancel);
        }
        internal new void OnCanceledMergeFinished()
        {
            base.OnCanceledMergeFinished();
        }
        internal new void Complete(bool Success)
        {
            base.Complete(Success);
        }
        internal new string RecoveryPath
        {
            get
            {
                return base.RecoveryPath;
            }
        }
        internal new string UnrevertedPath
        {
            get
            {
                return base.UnrevertedPath;
            }
        }
    }
}