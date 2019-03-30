using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser;
using TinyCsvParser.Mapping;

namespace CloudFrontLogParserDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            if (File.Exists(filePath))
            {
                using (var fileStream = File.OpenRead(filePath))
                using (var decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var decompressedStream = new MemoryStream())
                {
                    await decompressionStream.CopyToAsync(decompressedStream);

                    var data = Encoding.UTF8.GetString(decompressedStream.ToArray());

                    if (!string.IsNullOrEmpty(data))
                    {
                        var parser = new CloudWatchLogParser();
                        var parsedData = parser.Parse(data);
                    }                    
                }
            }
        }

        public class CloudWatchLogParser
        {
            private readonly CsvParser<CsvEmailBeaconLogRecord> _csvParser;

            public CloudWatchLogParser()
            {
                var csvParserOptions = new CsvParserOptions(true, "#", new TinyCsvParser.Tokenizer.RFC4180.RFC4180Tokenizer(new TinyCsvParser.Tokenizer.RFC4180.Options('"', '\\', '\t')));
                var csvMapper = new EmailBeaconLogRecordMapping();

                _csvParser = new CsvParser<CsvEmailBeaconLogRecord>(csvParserOptions, csvMapper);
            }

            public IEnumerable<CsvEmailBeaconLogRecord> Parse(string contents)
            {
                try
                {
                    return _csvParser
                        .ReadFromString(new CsvReaderOptions(new[] { "\n" }), contents)
                        .Where(x => x.IsValid)
                        .Select(x => x.Result);
                }
                catch (Exception)
                {
                    return Enumerable.Empty<CsvEmailBeaconLogRecord>();
                }
            }            

            public class CsvEmailBeaconLogRecord
            {
                public string Date { get; set; }
                public string Time { get; set; }
                public string UserAgent { get; set; }
            }

            private class EmailBeaconLogRecordMapping : CsvMapping<CsvEmailBeaconLogRecord>
            {
                public EmailBeaconLogRecordMapping() : base()
                {
                    base.MapProperty(0, x => x.Date);
                    base.MapProperty(1, x => x.Time);
                    base.MapProperty(10, x => x.UserAgent);
                }
            }
        }
    }
}
