using System;
using System.Collections.Generic;
using System.Diagnostics;
using MeloongCore;
using Newtonsoft.Json.Linq;
using PCLCS;

namespace PCL
{
    internal class PersonalOptionsSession : PersonalOptionsSessionData
    {
    }

    internal static class PersonalGameSettings
    {
        private readonly static PersonalOptionsFlow Flow = new PersonalOptionsFlow(LocalGameData.Service, LocalGameData.Gate, () => ModWatcher.HasRunningMinecraft, VersionPath => Adapt(new ModMinecraft.McInstance(VersionPath)), Selected, ModBase.IniClearCache, (First, Second) => PathUtils.IsSame(First, Second), (ErrorValue, Message) => Logger.Warn(ErrorValue, Message), () => new PersonalOptionsSession());

        internal static bool Enabled(ModMinecraft.McInstance Instance)
        {
            return Flow.Enabled(Adapt(Instance));
        }
        private static string CanonicalKey(string Key)
        {
            return PersonalOptionsRules.CanonicalKey(Key);
        }
        private static bool Selected(string Key)
        {
            if (Key.StartsWith("key_", StringComparison.Ordinal))
                return Settings.Get<bool>("LaunchPersonalOptionsKeys") && PersonalOptionsRules.VanillaBinding(Key);
            if (PersonalOptionsRules.IsMouseKey(Key))
                return Settings.Get<bool>("LaunchPersonalOptionsMouse");
            if (PersonalOptionsRules.IsViewKey(Key))
                return Settings.Get<bool>("LaunchPersonalOptionsView");
            if (PersonalOptionsRules.IsAccessibilityKey(Key))
                return Settings.Get<bool>("LaunchPersonalOptionsAccessibility");
            return Key.StartsWith("soundCategory_", StringComparison.Ordinal) && Settings.Get<bool>("LaunchPersonalOptionsAudio");
        }
        internal static Dictionary<string, string> Parse(string Text)
        {
            return PersonalOptionsRules.Parse(Text);
        }
        private static byte[] ReadOptions(string FileName)
        {
            return PersonalOptionsFiles.Read(FileName);
        }
        private static string Decode(byte[] Bytes)
        {
            return PersonalOptionsRules.Decode(Bytes);
        }
        private static bool Modern(ModMinecraft.McInstance Instance)
        {
            return Flow.Modern(Adapt(Instance));
        }
        private static string CanonicalValue(string Key, string Value)
        {
            return PersonalOptionsRules.CanonicalValue(Key, Value);
        }
        private static string ForVersion(string Key, string Value, bool IsModern)
        {
            return PersonalOptionsRules.ForVersion(Key, Value, IsModern);
        }
        internal static string MergeText(string Text, JObject Values, bool IsModern, ISet<string> Keys, bool FovDegrees = true)
        {
            return PersonalOptionsRules.MergeText(Text, Values, IsModern, Keys, Selected, FovDegrees);
        }
        internal static bool HasProfile()
        {
            return Flow.HasProfile();
        }
        internal static void ImportFrom(ModMinecraft.McInstance Instance)
        {
            Flow.ImportFrom(Adapt(Instance));
        }
        internal static PersonalOptionsSession Prepare(ModMinecraft.McInstance Instance)
        {
            return (PersonalOptionsSession)Flow.Prepare(Adapt(Instance));
        }
        internal static void Track(PersonalOptionsSession Session, Process Game)
        {
            Flow.Track(Session, Game);
        }
        internal static string NormalizeBinding(string Value)
        {
            return PersonalOptionsRules.NormalizeBinding(Value);
        }
        internal static string BindingForVersion(string Value, bool IsModern)
        {
            return PersonalOptionsRules.BindingForVersion(Value, IsModern);
        }
        internal static bool VanillaBinding(string Key)
        {
            return PersonalOptionsRules.VanillaBinding(Key);
        }
        internal static void CaptureEditedBinding(ModMinecraft.McInstance Instance, string Key, string Value)
        {
            Flow.CaptureEditedBinding(Adapt(Instance), Key, Value);
        }
        internal static void Finish(PersonalOptionsSession Session)
        {
            Flow.Finish(Session);
        }
        private static void Capture(PersonalOptionsSession Session)
        {
            Flow.Capture(Session);
        }
        internal static void RecoverExitedSessions()
        {
            Flow.RecoverExitedSessions();
        }
        private static bool HasActiveSession(string GameFolder)
        {
            return Flow.HasActiveSession(GameFolder);
        }
        private static Dictionary<int, string> CreateLegacyKeys()
        {
            return PersonalOptionsRules.CreateLegacyKeys();
        }
        private static IPersonalOptionsInstance Adapt(ModMinecraft.McInstance Instance)
        {
            return Instance is null ? null : new PersonalOptionsInstance(Instance);
        }
        private sealed class PersonalOptionsInstance : IPersonalOptionsInstance
        {
            private readonly ModMinecraft.McInstance Owner;
            internal PersonalOptionsInstance(ModMinecraft.McInstance Instance)
            {
                Owner = Instance;
            }
            public string VersionPath
            {
                get
                {
                    return Owner.PathVersion;
                }
            }
            public string GameFolder
            {
                get
                {
                    return Owner.PathIndie;
                }
            }
            public bool SyncEnabled
            {
                get
                {
                    return Settings.Get<bool>("LaunchPersonalOptionsSync") && Settings.Get<int>("VersionPersonalOptionsSync", Instance: Owner) == 0;
                }
            }
            public bool VersionValid
            {
                get
                {
                    return Owner.Version.Valid;
                }
            }
            public int VanillaMajor
            {
                get
                {
                    return Owner.Version.Vanilla.Major;
                }
            }
        }
    }
}