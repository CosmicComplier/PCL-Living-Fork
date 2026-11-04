using System;
using System.Collections.Generic;
using System.Threading;
using MeloongCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using PCLCS;

namespace PCL
{
    public class ResourceSearcher
    {
        public class SearchRequest : ResourceSearchRequestData<ResourceProject, SearchResult>
        {
            public SearchRequest(ResourceTypes Type, SearchResult Storage, int TargetResultCount) : base(Type, Storage, TargetResultCount)
            {
            }
            public override bool Equals(object Obj)
            {
                return base.Equals(Obj as SearchRequest);
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
            public static bool operator ==(SearchRequest Left, SearchRequest Right)
            {
                return EqualityComparer<SearchRequest>.Default.Equals(Left, Right);
            }
            public static bool operator !=(SearchRequest Left, SearchRequest Right)
            {
                return !(Left == Right);
            }
        }
        public class SearchResult : ResourceSearchResultData<ResourceProject>
        {
        }
        private static readonly ResourceSearchEngine<ResourceProject, SearchResult> Engine = new ResourceSearchEngine<ResourceProject, SearchResult>(new ResourceSearchHost<ResourceProject>()
        {
            CreateProject = new Func<JObject, ResourceProject>(Data => new ResourceProject(Data)),
            Request = new Func<string, JToken>(Url => ModDownload.DlModRequest(Url)),
            StartThread = new Func<Action, string, Thread>((Work, Name) => ModBase.RunInNewThread(() => Work(), Name)),
            SimplifyChinese = new Func<string, string>(Value => Strings.StrConv(Value, VbStrConv.SimplifiedChinese)),
            WikiEntries = new Func<List<WikiEntry>>(() => WikiEntry.All.Value),
            Info = new Action<string>(Message => Logger.Info(Message)),
            Warning = new Action<string>(Message => Logger.Warn(Message)),
            Warn = new Action<Exception, string>((ErrorValue, Message) => Logger.Warn(ErrorValue, Message))
        });
        public static ModLoader.LoaderTask<SearchRequest, int> GetLoader(Func<SearchRequest> InputDelegate)
        {
            return new ModLoader.LoaderTask<SearchRequest, int>("社区资源获取", Run, InputDelegate) { ReloadTimeout = 60 * 1000 };
        }
        private static void Run(ModLoader.LoaderTask<SearchRequest, int> Task)
        {
            Engine.Run(Task.Input, new ResourceSearchTask()
            {
                IsCanceled = new Func<bool>(() => Task.IsCanceled),
                Progress = new Func<double>(() => Task.Progress),
                SetProgress = new Action<double>(Value => Task.Progress = Value)
            });
        }
    }
}