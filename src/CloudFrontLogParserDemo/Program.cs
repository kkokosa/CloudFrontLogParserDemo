using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Buffers;

namespace CloudFrontLogParserDemo
{
    class Program
    {
        static async Task Main()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            // OLD

            var originalData = await OldCloudFrontParser.ParseAsync(filePath);

            // NEW

            var newData = ArrayPool<CloudFrontRecord>.Shared.Rent(100); // make the record a struct?

            try
            {
                await NewCloudFrontParser.ParseAsync(filePath, newData);
            }
            finally
            {
                ArrayPool<CloudFrontRecord>.Shared.Return(newData, clearArray: true);
            }  
        }
    }
}
