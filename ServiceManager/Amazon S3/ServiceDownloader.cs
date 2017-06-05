using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using static System.String;

namespace ServiceManager
{
    public static class ServiceDownloader
    {
        public static void ExtractZipFile(string archiveFilenameIn, string password, string outFolder)
        {
            ZipFile zf = null;
            try
            {
                using (var fs = File.OpenRead(archiveFilenameIn))
                {
                    zf = new ZipFile(fs);
                    if (!IsNullOrEmpty(password))
                    {
                        zf.Password = password;     // AES encrypted entries are handled automatically
                    }
                    foreach (var zipEntry in zf.Cast<ZipEntry>().Where(zipEntry => zipEntry.IsFile))
                    {
                        var entryFileName = zipEntry.Name;
                        // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                        // Optionally match entrynames against a selection list here to skip as desired.
                        // The unpacked length is available in the zipEntry.Size property.

                        var buffer = new byte[4096];     // 4K is optimum
                        var zipStream = zf.GetInputStream(zipEntry);

                        // Manipulate the output filename here as desired.
                        var fullZipToPath = Path.Combine(outFolder, entryFileName);
                        var directoryName = Path.GetDirectoryName(fullZipToPath);
                        if (!IsNullOrEmpty(directoryName))
                            Directory.CreateDirectory(directoryName);

                        // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                        // of the file, but does not waste memory.
                        // The "using" will close the stream even if an exception occurs.
                        using (var streamWriter = File.Create(fullZipToPath))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                    }
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }

                //Delete zip downloaded zip
                if(File.Exists(archiveFilenameIn)) File.Delete($"{archiveFilenameIn}");
            }
        }

        public static void CreateUserDataFile(byte[] userdataFile, string outfolder)
        {
            File.WriteAllBytes($"{outfolder}\\userdata.json", userdataFile);
        }
    }

}
