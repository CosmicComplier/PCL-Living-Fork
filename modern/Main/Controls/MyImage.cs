using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using MeloongCore;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyImage : MyImageView
    {

        public static new readonly DependencyProperty EnableCacheProperty = MyImageView.EnableCacheProperty;
        public static new readonly DependencyProperty SourceProperty = MyImageView.SourceProperty;
        protected override int NewUuid()
        {
            return ModBase.GetUuid();
        }
        protected override string TempRoot
        {
            get
            {
                return ModBase.PathTemp;
            }
        }
        protected override ImageSource ReadBitmap(string Address)
        {
            return (ImageSource)new MyBitmap(Address);
        }
        protected override void DeleteFile(string Path)
        {
            FileUtils.Delete(Path);
        }
        protected override FileInfo FileInformation(string Path)
        {
            return FileUtils.GetInfo(Path);
        }
        protected override void UiPost(Action Work)
        {
            ModBase.RunInUi(Work);
        }
        protected override void Warn(Exception ErrorValue, string Message)
        {
            Logger.Warn(ErrorValue, Message);
        }
        protected override void Error(Exception ErrorValue, string Message)
        {
            Logger.Error(ErrorValue, Message, LogBehavior.Toast);
        }
        protected override void Download(string[] Addresses, string Path)
        {
            ModNet.NetDownloadByLoader(Addresses, Path, SimulateBrowserHeaders: true);
        }
        protected override void StartWorker(Action Work, string Name, ThreadPriority Priority)
        {
            ModBase.RunInNewThread(Work, Name, Priority);
        }
    }
}