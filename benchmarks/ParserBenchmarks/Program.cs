using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CloudFrontLogParserDemo;
using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ParserBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<CloudFrontParserBenchmarks>();
        }
    }

    [MemoryDiagnoser]
    public class CloudFrontParserBenchmarks
    {
        [Benchmark(Baseline = true)]
        public async Task Original()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(CloudFrontRecord)).Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            for (int i = 0; i < 5; i++)
            {
                var results = await OldCloudFrontParser.ParseAsync(filePath);
            }
        }

        [Benchmark]
        public async Task Optimised()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(CloudFrontRecord)).Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            var newData = ArrayPool<CloudFrontRecord>.Shared.Rent(100); // make the record a struct?

            for (int i = 0; i < 5; i++)
            {
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
}
