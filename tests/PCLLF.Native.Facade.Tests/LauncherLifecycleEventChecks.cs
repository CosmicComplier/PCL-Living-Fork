using System.Collections;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;
internal static partial class LauncherLifecycleChecks
{
    private static void CustomEventAndSecret()
    {
        Check("custom:81", typeof(CustomEvent).Assembly == Native.Main);
        using (var file = File.OpenRead(Native.Main.Location))
        using (var pe = new PEReader(file))
        {
            MetadataReader metadata = pe.GetMetadataReader();
            Check("custom:78", AssemblyName.GetAssemblyName(Native.Main.Location).FullName == Native.Main.FullName &&
                metadata.GetGuid(metadata.GetModuleDefinition().Mvid) == Native.Main.ManifestModule.ModuleVersionId);
        }
        Type owner = typeof(CustomEventService), evt = typeof(CustomEvent);
        DependencyProperty[] attached = [CustomEventService.EventsProperty, CustomEventService.EventTypeProperty, CustomEventService.EventDataProperty];
        string[] names = ["Events", "EventType", "EventData"];
        for (int i = 0; i < attached.Length; i++)
        {
            Check("custom:29", attached[i].OwnerType == owner, names[i]);
            var descriptor = DependencyPropertyDescriptor.FromName(names[i], owner, typeof(Button));
            Check("custom:31", descriptor is not null && ReferenceEquals(descriptor.DependencyProperty, attached[i]), names[i]);
        }
        Check("custom:33", CustomEventService.EventTypeProperty.PropertyType == typeof(CustomEvent.EventType) && CustomEventService.EventTypeProperty.DefaultMetadata.DefaultValue.Equals(CustomEvent.EventType.None));
        Check("custom:34", CustomEvent.TypeProperty.OwnerType == evt && CustomEvent.TypeProperty.PropertyType == typeof(CustomEvent.EventType) && CustomEvent.TypeProperty.DefaultMetadata.DefaultValue.Equals(CustomEvent.EventType.None));
        Check("custom:35", CustomEvent.DataProperty.OwnerType == evt && CustomEvent.DataProperty.DefaultMetadata.DefaultValue is null && CustomEventService.EventDataProperty.DefaultMetadata.DefaultValue is null);
        foreach (string name in new[] { "Type", "Data" }) Check("custom:36", DependencyPropertyDescriptor.FromName(name, evt, evt) is not null, name);
        Button first = new(), second = new(); CustomEventCollection a = CustomEventService.GetEvents(first), b = CustomEventService.GetEvents(second);
        Check("custom:38", ReferenceEquals(a, CustomEventService.GetEvents(first)) && !ReferenceEquals(a, b));
        FieldInfo? field = typeof(CustomEventCollection).GetField("_Events", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Check("custom:40", field is not null && ReferenceEquals(field.GetValue(a), a.Events));
        CustomEvent item = new(CustomEvent.EventType.复制文本, "fixture"); a.Events.Add(item);
        Check("custom:41", a.Count() == 1 && ((IEnumerable)a).Cast<CustomEvent>().Single() == item);
        Check("custom:42", item.GetValue(CustomEvent.TypeProperty).Equals(CustomEvent.EventType.复制文本) && (string)item.GetValue(CustomEvent.DataProperty) == "fixture");
        string local = "clr-namespace:PCL;assembly=" + evt.Assembly.GetName().Name;
        var parsed = (Button)XamlReader.Parse("<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='" + local + "' local:CustomEventService.EventType='复制文本' local:CustomEventService.EventData='raw|tokens' />");
        Check("custom:45", CustomEventService.GetEventType(parsed) == CustomEvent.EventType.复制文本 && CustomEventService.GetEventData(parsed) == "raw|tokens");
        Style style = (Style)XamlReader.Parse("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='" + local + "' TargetType='Button'><Setter Property='local:CustomEventService.EventType' Value='复制文本'/><Setter Property='local:CustomEventService.EventData' Value='styled'/></Style>");
        Button styled = new() { Style = style };
        Check("custom:47", ReferenceEquals(((Setter)style.Setters[0]).Property, CustomEventService.EventTypeProperty) && CustomEventService.GetEventType(styled) == CustomEvent.EventType.复制文本 && CustomEventService.GetEventData(styled) == "styled");
        Button collection = (Button)XamlReader.Parse("<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='" + local + "'><local:CustomEventService.Events><local:CustomEventCollection><local:CustomEvent Type='复制文本' Data='first'/><local:CustomEvent Type='刷新帮助' Data='second'/></local:CustomEventCollection></local:CustomEventService.Events></Button>");
        var values = CustomEventService.GetEvents(collection).Events;
        Check("custom:49", values.Count == 2 && values[0].GetValue(CustomEvent.TypeProperty).Equals(CustomEvent.EventType.复制文本) && (string)values[1].GetValue(CustomEvent.DataProperty) == "second");
        SecretHeaders();
        Console.WriteLine("PASS native lifecycle custom events: actual Main five DP owners/defaults, private collection/enumerator identity, attached/Setter/content XAML; no event is fired/clipboard or external action invoked.");
    }

    private static void SecretHeaders()
    {
        Type type = Native.Type("PCL.ModSecret"); MethodInfo method = Native.Method(type, "SecretHeadersSign"); var parameters = method.GetParameters();
        Check("custom:53", parameters.Length == 4 && parameters[1].ParameterType.IsByRef && Equals(parameters[2].DefaultValue, false) && Equals(parameters[3].DefaultValue, true));
        object signer = Native.Field(type, "NetworkSigner").GetValue(null)!;
        var host = (LegacySecretNetworkHost)Native.Get(signer, "host")!; Func<string> previousKey = host.CurseForgeKey; int reads = 0;
        // Replace only the existing credential platform factory. The real Main
        // signer/service/URI policy are not replaced, and no CI secret is read.
        host.CurseForgeKey = () => { reads++; return "synthetic-lifecycle-request-key"; };
        try
        {
            using (HttpRequestMessage request = new(HttpMethod.Get, "https://api.curseforge.com/v1/mods"))
            {
                object?[] args = ["https://api.curseforge.com/v1/mods", request, false, false]; Native.Call(type, method.Name, null, args);
                Check("custom:56", ReferenceEquals(args[1], request) && !request.Headers.Contains("x-api-key") && request.Headers.UserAgent.ToString().StartsWith("PCL-Living-Fork/", StringComparison.Ordinal));
            }
            using (HttpRequestMessage request = new(HttpMethod.Get, "https://edge.forgecdn.net/files/fixture"))
            {
                request.Headers.Add("User-Agent", "Fixture/1"); object?[] args = ["https://api.curseforge.com/v1/mods", request, true, true]; Native.Call(type, method.Name, null, args);
                Check("custom:60", !request.Headers.Contains("x-api-key") && !request.Headers.Contains("Referer") && request.Headers.UserAgent.ToString() == "Fixture/1");
            }
            using (HttpRequestMessage request = new(HttpMethod.Get, "https://api.curseforge.com/v1/mods"))
            {
                request.Headers.Add("x-api-key", "synthetic-existing"); object?[] args = ["https://api.curseforge.com/v1/mods", request, true, true]; Native.Call(type, method.Name, null, args);
                Check("custom:64", request.Headers.GetValues("x-api-key").Single() == "synthetic-existing");
            }
            Native.Check(reads == 0, "Original no-credential/URI/existing-header paths evaluated a credential factory.");
            using (HttpRequestMessage request = new(HttpMethod.Get, "https://api.curseforge.com/v1/mods"))
            {
                Native.Call(type, method.Name, null, "https://api.curseforge.com/v1/mods", request, false, true);
                Native.Check(reads == 1 && request.Headers.GetValues("x-api-key").Single() == "synthetic-lifecycle-request-key", "Explicit controlled key factory did not reach the real signer exactly once.");
            }
            Check("custom:66", (string)Native.Call(type, "SecretCdnSign", null, "https://fixture.invalid/a b{CDN}")! == "https://fixture.invalid/a%20b");
        }
        finally { host.CurseForgeKey = previousKey; }
        Console.WriteLine("PASS native lifecycle secret signer: real Main ByRef/Optional ABI and LF/URI/browser/existing headers/CDN; synthetic factory only, no real credential value logged or read, no HTTP sent.");
    }
}
