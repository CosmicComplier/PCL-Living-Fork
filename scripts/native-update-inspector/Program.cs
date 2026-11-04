using System.Text.Json;
using PCL;
try {
 if(args.Length!=1) throw new ArgumentException("Provide the actual native payload path.");
 var identity=ModernBundleIdentity.Read(args[0]);
 if(!ModernBundleIdentity.IsLivingExecutable(args[0],"PCL Living Fork")) throw new InvalidDataException("Not a supported genuine LF native payload: "+JsonSerializer.Serialize(identity));
 Console.WriteLine(JsonSerializer.Serialize(identity));
 return 0;
} catch(Exception error) {Console.Error.WriteLine(error.GetType().Name+": "+error.Message);return 1;}
