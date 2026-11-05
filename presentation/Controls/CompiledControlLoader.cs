using System;
using System.IO;
using System.Windows;
using System.Windows.Baml2006;
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Permissions;

namespace PCLLF.Presentation.Controls;

/// <summary>Loads the original compiled visual into the actual legacy derived control instance.</summary>
internal static class CompiledControlLoader {
    // Application.LoadComponent(object, Uri) rejects a component whose actual assembly
    // differs from the BAML assembly (dotnet/wpf v10.0.14 Application.cs:424).
    // Public Baml2006Reader(Stream, XamlReaderSettings), XamlObjectWriter.RootObjectInstance
    // and RootNameScope are also part of the .NET Framework 4.8 WPF/System.Xaml contract.
    // XamlObjectWriter forwards ConnectionId to the existing IComponentConnector;
    // no private WPF loader, raw XAML parsing or alternate root object is used.
    public static void Load(FrameworkElement component, Type viewType, string resourcePath) {
        Uri relative = new("/" + viewType.Assembly.GetName().Name + ";component/" + resourcePath, UriKind.Relative);
        var resource = Application.GetResourceStream(relative) ?? throw new FileNotFoundException("Compiled LF control visual was not found.", relative.OriginalString);
        Uri location = new(new Uri("pack://application:,,,/", UriKind.Absolute), relative);
        NameScope scope = new();
        NameScope.SetNameScope(component, scope);
        if (component is IUriContext uriContext) uriContext.BaseUri = location;
        using Stream stream = resource.Stream;
        using Baml2006Reader reader = new(stream, new XamlReaderSettings {
            BaseUri = location,
            LocalAssembly = viewType.Assembly,
            IgnoreUidsOnPropertyElements = true,
            ProvideLineInfo = true
        });
        using XamlObjectWriter writer = new(reader.SchemaContext, new XamlObjectWriterSettings {
            RootObjectInstance = component,
            AccessLevel = XamlAccessLevel.AssemblyAccessTo(viewType.Assembly),
            ExternalNameScope = scope,
            RegisterNamesOnExternalNamescope = true,
            SkipDuplicatePropertyCheck = true
        });
        while (reader.Read()) writer.WriteNode(reader);
        if (!ReferenceEquals(writer.Result, component)) throw new InvalidOperationException("Compiled LF control replaced its supplied root instance.");
        NameScope.SetNameScope(component, writer.RootNameScope);
    }
}
