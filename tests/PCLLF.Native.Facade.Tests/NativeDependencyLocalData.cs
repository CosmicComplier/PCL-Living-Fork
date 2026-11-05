// Exact TOML fixtures from LF50 Test-ModDependencies.ps1, LF LICENCE.
namespace PCLLF.Native.Facade.Tests;
internal static class NativeDependencyLocalData
{
    internal const string ForgeMulti = """
modLoader="javafml"
loaderVersion="[47,)"
license="fixture"
[[mods]]
modId="multi_one"
version="FILE_VERSION"
displayName="Multi one"
description='''
[[dependencies.multi_one]]
modId="fake_from_description"
mandatory=true
'''
[[mods]]
modId="multi_two"
version="2.0"
displayName="Name # with hash"
[[dependencies.multi_one]]
modId="multi_two"
mandatory=true
versionRange="[2.0,3.0)" # real comment
side="CLIENT"
[[dependencies.multi_one]]
modId="server_missing"
mandatory=true
side="SERVER"
[[dependencies.multi_one]]
modId="optional_missing"
mandatory=false
versionRange="[1,)"
[[dependencies.multi_two]]
modId="minecraft"
mandatory=true
versionRange="[1.20.1]"
[features.multi_two]
java_version="[17,)"
""";
    internal const string ForgeOptional = """
modLoader="javafml"
loaderVersion="[47,)"
[[mods]]
modId="optional_owner"
version="1.0"
[[dependencies.optional_owner]]
modId="optional_library"
mandatory=false
versionRange="[2.0,)"
""";
    internal const string Neo = """
modLoader="javafml"
loaderVersion="[4,)"
[[mods]]
modId="neo"
version="1.0"
[[dependencies.neo]]
modId="neoforge"
type="required"
versionRange="[21.1,22)"
[[dependencies.neo]]
modId="optional_absent"
type="optional"
[[dependencies.neo]]
modId="bad"
type="incompatible"
versionRange="[1,2)"
""";
    internal const string JarJarInner = """
modLoader="javafml"
loaderVersion="[4,)"
[[mods]]
modId="jarjar_library"
version="1.0"
""";
}
