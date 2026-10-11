using System.IO;
using System.IO.Compression;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static string ReadAuthoredMeshJson(){
   string plain=Root+"/Art/Authored/ApartmentFurniture.meshdata.json";
   if(!File.Exists(plain+".gz"))return File.ReadAllText(plain);
   using(var source=File.OpenRead(plain+".gz"))
   using(var gzip=new GZipStream(source,CompressionMode.Decompress))
   using(var reader=new StreamReader(gzip))return reader.ReadToEnd();
  }
 }
}
