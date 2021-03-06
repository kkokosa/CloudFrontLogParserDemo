﻿using Pipelines.Sockets.Unofficial;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace CloudFrontLogParserDemo
{
    public static class NewCloudFrontParser
    {
        public static async Task ParseAsync(string filePath, CloudFrontRecord[] items)
        {
            if (File.Exists(filePath))
            {
                using (var fileStream = File.OpenRead(filePath))
                using (var decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    var pipeReader = StreamConnection.GetReader(decompressionStream);

                    var position = 0;

                    while (true)
                    {
                        var result = await pipeReader.ReadAsync();

                        var buffer = result.Buffer;

                        ParseLines(items, ref buffer, ref position);

                        // Tell the PipeReader how much of the buffer we have consumed
                        pipeReader.AdvanceTo(buffer.Start, buffer.End);

                        // Stop reading if there's no more data coming
                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }

                    // Mark the PipeReader as complete
                    pipeReader.Complete();
                }
            }
        }

        private static void ParseLines(CloudFrontRecord[] itemsArray, ref ReadOnlySequence<byte> buffer, ref int position)
        {
            const byte newLine = (byte)'\n';

            var reader = new SequenceReader<byte>(buffer);

            while (!reader.End)
            {
                var span = reader.UnreadSpan;
                var index = span.IndexOf(newLine);
                int length;

                if (index != -1)
                {
                    length = index;

                    var parsedLine = LineParser.ParseLine(span.Slice(0, index));

                    if (parsedLine != null) itemsArray[position++] = parsedLine;
                }
                else
                {
                    // We didn't find the new line in the current segment, see if it's 
                    // another segment
                    var current = reader.Position;
                    var linePos = buffer.Slice(current).PositionOf(newLine);

                    if (linePos == null)
                    {
                        // Nope
                        break;
                    }

                    // We found one, so get the line and parse it
                    var line = buffer.Slice(current, linePos.Value);

                    var parsedLine = ParseLine(line);

                    if (parsedLine != null) itemsArray[position++] = parsedLine;

                    length = (int)line.Length;
                }

                // Advance past the line + the \n
                reader.Advance(length + 1);
            }

            // Update the buffer
            buffer = buffer.Slice(reader.Position);
        }

        private static CloudFrontRecord ParseLine(in ReadOnlySequence<byte> line)
        {
            // Lines are always small so we incur a small copy if we happen to cross a buffer boundary
            if (line.IsSingleSegment)
            {
                return LineParser.ParseLine(line.First.Span);
            }
            else if (line.Length < 256)
            {
                // Small lines we copy to the stack
                Span<byte> stackLine = stackalloc byte[(int)line.Length];
                line.CopyTo(stackLine);
                return LineParser.ParseLine(stackLine);
            }
            else
            {
                // Should be extremely rare
                var length = (int)line.Length;
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                line.CopyTo(buffer);
                var emailBeaconCloudWatchLogRecord = LineParser.ParseLine(buffer.AsSpan(0, length));
                ArrayPool<byte>.Shared.Return(buffer);
                return emailBeaconCloudWatchLogRecord;
            }
        }

        private static class LineParser
        {
            private const byte Tab = (byte)'\t', Hash = (byte)'#';

            public static CloudFrontRecord ParseLine(ReadOnlySpan<byte> line)
            {
                if (line[0] == Hash) return null;

                var tabCount = 1;

                var record = new CloudFrontRecord();

                while (tabCount < 13)
                {
                    var tabAt = line.IndexOf(Tab);

                    if (tabCount == 1)
                    {
                        {
                            var value = Encoding.UTF8.GetString(line.Slice(0, tabAt));
                            record.Date = value;
                        }
                    }
                    else if (tabCount == 2)
                    {
                        {
                            var value = Encoding.UTF8.GetString(line.Slice(0, tabAt));
                            record.Time = value;
                        }
                    }
                    else if (tabCount == 11)
                    {
                        {
                            var value = Encoding.UTF8.GetString(line.Slice(0, tabAt));
                            record.UserAgent = value;
                        }
                    }

                    line = line.Slice(tabAt + 1);

                    tabCount++;
                }

                return record;
            }
        }

    }
}
