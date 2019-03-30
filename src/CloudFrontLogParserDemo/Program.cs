using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace CloudFrontLogParserDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            if (File.Exists(filePath))
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (GZipStream decompressionStream = new GZipStream(fs, CompressionMode.Decompress))
                {

                }
            }
        }
    }
}
