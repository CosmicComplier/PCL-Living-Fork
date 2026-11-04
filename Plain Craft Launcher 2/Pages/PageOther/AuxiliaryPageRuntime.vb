Imports PCLCS
Imports PCLLF.Presentation.Controls

' Actual LF model/platform types and live original field access only.
Friend NotInheritable Class AuxiliaryPageRuntime
    Friend Shared ReadOnly Basic As New LegacyAuxiliaryBasicHost With {
        .Common=SetupPageRuntime.Common,
        .LauncherVersion=Function() VersionBaseName,
        .CheckUpdate=AddressOf UpdateCheckByButton
    }
    Friend Shared Function NavigationHost(ReadId As Func(Of Integer),WriteId As Action(Of Integer)) As LegacyAuxiliaryNavigationHost
        Return New LegacyAuxiliaryNavigationHost With {
            .Common=SetupPageRuntime.Common,
            .HiddenForceShow=Function() PageSetupUI.HiddenForceShow,
            .ModpackEnabled=Function() ModModpackMode.Enabled,
            .LegacyLink=AddressOf LegacyLinkEnabled,
            .HiddenRefresh=AddressOf PageSetupUI.HiddenRefresh,
            .ApplyModpackVisibility=AddressOf ModModpackMode.ApplyVisibility,
            .RefreshHelp=AddressOf RefreshHelp,
            .NormalizeSetup=Function(ID) CInt(ModModpackMode.NormalizeSetupPage(CType(ID,FormMain.PageSubType))),
            .LauncherSettingsPage=Function() ModModpackMode.LauncherSettingsPage(),
            .PageId=ReadId,.SetPageId=WriteId,
            .ParseTag=Function(Value) CInt(Val(Value)),
            .FormatNumber=Function(ID) CStr(CType(ID,FormMain.PageSubType)),
            .FormatEnum=Function(ID) CType(ID,FormMain.PageSubType).ToString(),
            .Target=Function() FrmMain.PageRight,
            .SetTarget=Sub(Target) FrmMain.PageRight=DirectCast(Target,MyPageRight),
            .MainRight=Function() FrmMain.PanMainRight,
            .Launch=New LegacyAuxiliaryPageSlot With {.Read=Function() FrmSetupLaunch,.Write=Sub(Page) FrmSetupLaunch=DirectCast(Page,PageSetupLaunch),.Create=Function() New PageSetupLaunch(),.Reset=Sub(Page) DirectCast(Page,PageSetupLaunch).Reset()},
            .UI=New LegacyAuxiliaryPageSlot With {.Read=Function() FrmSetupUI,.Write=Sub(Page) FrmSetupUI=DirectCast(Page,PageSetupUI),.Create=Function() New PageSetupUI(),.Reset=Sub(Page) DirectCast(Page,PageSetupUI).Reset()},
            .Link=LinkSlot(),
            .System=New LegacyAuxiliaryPageSlot With {.Read=Function() FrmSetupSystem,.Write=Sub(Page) FrmSetupSystem=DirectCast(Page,PageSetupSystem),.Create=Function() New PageSetupSystem(),.Reset=Sub(Page) DirectCast(Page,PageSetupSystem).Reset()},
            .Help=New LegacyAuxiliaryPageSlot With {.Read=Function() FrmOtherHelp,.Write=Sub(Page) FrmOtherHelp=DirectCast(Page,PageOtherHelp),.Create=Function() New PageOtherHelp()},
            .About=New LegacyAuxiliaryPageSlot With {.Read=Function() FrmOtherAbout,.Write=Sub(Page) FrmOtherAbout=DirectCast(Page,PageOtherAbout),.Create=Function() New PageOtherAbout()},
            .Test=New LegacyAuxiliaryPageSlot With {.Read=Function() FrmOtherTest,.Write=Sub(Page) FrmOtherTest=DirectCast(Page,PageOtherTest),.Create=Function() New PageOtherTest()}
        }
    End Function
    Private Shared Function LegacyLinkEnabled() As Boolean
#If LEGACY_LINK Then
        Return True
#Else
        Return False
#End If
    End Function
    Private Shared Function LinkSlot() As LegacyAuxiliaryPageSlot
#If LEGACY_LINK Then
        Return New LegacyAuxiliaryPageSlot With {.Read=Function() FrmSetupLink,.Write=Sub(Page) FrmSetupLink=DirectCast(Page,PageSetupLink),.Create=Function() New PageSetupLink(),.Reset=Sub(Page) DirectCast(Page,PageSetupLink).Reset()}
#Else
        Return Nothing
#End If
    End Function
    Friend Shared Sub RefreshHelp()
        Settings.Set("SystemHelpVersion",0)
        FrmOtherHelp.PageLoaderRestart()
        FrmOtherHelp.SearchBox.Text=""
    End Sub
    Friend Shared ReadOnly Help As New LegacyAuxiliaryHelpRuntime(Of HelpEntry,CustomEvent.EventType)(New LegacyAuxiliaryHelpHost(Of HelpEntry,CustomEvent.EventType) With {
        .Common=SetupPageRuntime.Common,
        .Release=Function() BuildType=BuildTypes.Release,
        .Debug=Function() ModeDebug,
        .LoaderFinished=Function() HelpLoader.State=LoadState.Finished,
        .WaitForLoader=Sub() HelpLoader.WaitForExit(GetUuid()),
        .RefreshHelp=AddressOf RefreshHelp,
        .Entries=Function() HelpLoader.Output,
        .Types=Function(Entry) Entry.Types,
        .ShowPublic=Function(Entry) Entry.ShowInPublic,
        .ShowSnapshot=Function(Entry) Entry.ShowInSnapshot,
        .ShowSearch=Function(Entry) Entry.ShowInSearch,
        .IsEvent=Function(Entry) Entry.IsEvent,
        .Title=Function(Entry) Entry.Title,
        .Description=Function(Entry) Entry.Desc,
        .Search=Function(Entry) Entry.Search,
        .RawPath=Function(Entry) Entry.RawPath,
        .Xaml=Function(Entry) Entry.XamlContent,
        .EventType=Function(Entry) Entry.EventType,
        .EventData=Function(Entry) Entry.EventData,
        .RaiseEvent=Sub(Type,Data) CustomEvent.Raise(Type,Data),
        .NewEntry=Function(Path) New HelpEntry(Path),
        .ToItem=Function(Entry) Entry.ToListItem(),
        .NewCard=Function() New MyCard(),
        .InitializeLoader=AddressOf InitializeHelpLoader,
        .NewDetailController=AddressOf NewHelpDetail,
        .NavigateDetail=AddressOf NavigateHelpDetail,
        .ReplaceArguments=Function(Content,Escape) ArgumentReplace(Content,Escape),
        .ParseXaml=Function(Content) DirectCast(GetObjectFromXML(Content),UIElement)
    })
    Private Shared Sub InitializeHelpLoader(Page As MyPageRightView,Loading As MyLoadingView,LoaderPanel As FrameworkElement,ContentPanel As FrameworkElement,Finished As Action(Of List(Of HelpEntry)))
        DirectCast(Page,MyPageRight).PageLoaderInit(DirectCast(Loading,MyLoading),LoaderPanel,ContentPanel,Nothing,HelpLoader,Sub(Loader) Finished(Loader.Output))
    End Sub
    Private Shared Function NewHelpDetail() As LegacyAuxiliaryHelpDetailController(Of HelpEntry,CustomEvent.EventType)
        Dim Page As New PageOtherHelpDetail()
        Return Page.Controller
    End Function
    Private Shared Sub NavigateHelpDetail(Entry As HelpEntry,Controller As LegacyAuxiliaryHelpDetailController(Of HelpEntry,CustomEvent.EventType))
        FrmMain.PageChange(New FormMain.PageStackData With {.Page=FormMain.PageType.HelpDetail,.Additional={Entry,DirectCast(Controller.View.Page(),PageOtherHelpDetail)}})
    End Sub
    Friend Shared ReadOnly Tools As New LegacyToolboxPageRuntime(Of NetFile,LoaderDownload,LoaderTask(Of Integer,Integer),LoaderCombo(Of Integer))(New LegacyToolboxPageHost(Of NetFile,LoaderDownload,LoaderTask(Of Integer,Integer),LoaderCombo(Of Integer)) With {
        .Common=SetupPageRuntime.Common,
        .CurrentPage=Function() If(FrmOtherTest Is Nothing,Nothing,FrmOtherTest.Controller),
        .HasMain=Function() FrmMain IsNot Nothing,
        .ExtraDownload=Function() FrmMain.BtnExtraDownload,
        .HasRunningGame=Function() HasRunningMinecraft,
        .LaunchLoading=Function() McLaunchLoader.State=LoadState.Loading,
        .HasDownloadingTask=AddressOf HasDownloadingTask,
        .HasGameFolders=Function() McFolderList.Any(),
        .HasAdminRole=AddressOf WindowsUtils.HasAdminRole,
        .StartFolderLoader=Sub() McFolderListLoader.Start(),
        .WaitFolderLoader=Sub() McFolderListLoader.WaitForExit(),
        .OptimizeNative=AddressOf NativeMemoryOptimizer.Run,
        .GameFolders=Function() McFolderList.Select(Function(Item) Item.Location),
        .AvailableMemory=Function() CLng(My.Computer.Info.AvailablePhysicalMemory),
        .RunElevated=AddressOf RunAsAdmin,
        .AcquireCleanup=AddressOf PageOtherTest.AcquireCleanup,
        .AcquireMemory=AddressOf PageOtherTest.AcquireMemory,
        .ReadCleanup=AddressOf PageOtherTest.ReadCleanup,
        .ReleaseCleanup=AddressOf PageOtherTest.ReleaseCleanup,
        .ReleaseMemory=AddressOf PageOtherTest.ReleaseMemory,
        .NewThread=Sub(Work,Name) RunInNewThread(Work,Name),
        .RandomOne=Function(Values) RandomOne(Values),
        .SaveFile=Function(Title,Name) RunInUiWait(Function() Dialogs.SaveFile(Title,Name)),
        .SelectFolder=Function() Dialogs.SelectFolder("选择目标文件夹",False).FirstOrDefault(),
        .StartProcess=Sub(Path) StartProcess(Path),
        .ValidateFolder=Function(Path) (New ValidateFolderPath(False)).Validate(Path),
        .AddValidation=Sub(TextBox,Rule) DirectCast(TextBox,MyTextBox).ValidateRules.Add(New ValidateFunc(Rule)),
        .AddFolderValidation=Sub(TextBox) DirectCast(TextBox,MyTextBox).ValidateRules.Add(New ValidateFolderPath(False)),
        .CreateSession=Function(Folder,Name) ToolboxDownloadSession.Create(Folder,Name),
        .NewFile=Function(Url,Path,Minimum,Browser) New NetFile({Url},Path,New FileChecker With {.MinSize=Minimum},SimulateBrowserHeaders:=Browser),
        .SetCredentialHeaders=Sub(File,Allow) File.AllowCredentialHeaders=Allow,
        .SetMergeFinished=Sub(File,Callback) File.CanceledMergeFinished=Callback,
        .NewDownload=Function(Name,Files) New LoaderDownload(Name,Files),
        .NewSave=Function(Name,Work) New LoaderTask(Of Integer,Integer)(Name,Sub(Task) Work(Task)),
        .SaveCanceled=Function(Task) Task.IsCanceled,
        .SaveWeight=Sub(Task,Weight) Task.ProgressWeight=Weight,
        .NewCombo=Function(Name,Download,Save) New LoaderCombo(Of Integer)(Name,New LoaderBase(){Download,Save}),
        .ComboState=Function(Loader) CInt(Loader.State),
        .ComboName=Function(Loader) Loader.Name,
        .ComboError=Function(Loader) Loader.Error,
        .SetStateCallback=Sub(Loader,Callback) Loader.OnStateChanged=Sub(Changed) Callback(DirectCast(Changed,LoaderCombo(Of Integer))),
        .StartCombo=Sub(Loader) Loader.Start(),
        .AddTaskbar=Sub(Loader) LoaderTaskbarAdd(Loader),
        .NewUuid=AddressOf GetUuid
    })
End Class
