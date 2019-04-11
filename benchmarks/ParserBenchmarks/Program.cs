using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CloudFrontLogParserDemo;
using SpanDemo;
using System;
using System.Buffers;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ParserBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SpanBenchmarks>();
        }
    }

    [MemoryDiagnoser]
    public class SpanBenchmarks
    {
        private EventContext _context;
        private S3ObjectKeyGenerator _originalGenerator;

        [GlobalSetup]
        public void Setup()
        {
            _context = new EventContext
            {
                MessageId = "ebc6d78b-8b29-4fc3-a412-bc43be6a9d21",
                EventDateUtc = new DateTime(2019,04,01,10,00,00,DateTimeKind.Utc),
                EventName = "MyEvent",
                Product = "MyProduct",
                SiteKey = "SiteKey"
            };

            _originalGenerator = new S3ObjectKeyGenerator();
        }

        [Benchmark(Baseline = true)]
        public async Task Original() => _ = _originalGenerator.GenerateSafeObjectKey(_context);
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
