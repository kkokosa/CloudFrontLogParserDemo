using System;
using System.Text.RegularExpressions;

namespace SpanDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public class S3ObjectKeyGenerator
    {
        // TODO - Performance - This can be benchamrked and likely improved with Span<T>

        private const string ValidKeyPartPattern = "^[a-zA-Z0-9_]+$";
        private const string UnknownPart = "unknown";
        private const string InvalidPart = "invalid";
        private const char JoinChar = '/';

        public string GenerateSafeObjectKey(EventContext eventContext)
        {
            var parts = new string[8];

            parts[0] = GetPart(eventContext.Product);
            parts[1] = GetPart(eventContext.SiteKey);
            parts[2] = GetPart(eventContext.EventName);

            if (eventContext.EventDateUtc == default)
            {
                parts[3] = UnknownPart;
                parts[4] = UnknownPart;
                parts[5] = UnknownPart;
                parts[6] = UnknownPart;
            }
            else
            {
                // pad with a zero if less than 10 which ensures a consistent length of two chars.
                var month = eventContext.EventDateUtc.Month < 10 ? "0" + eventContext.EventDateUtc.Month.ToString() : eventContext.EventDateUtc.Month.ToString();
                var day = eventContext.EventDateUtc.Day < 10 ? "0" + eventContext.EventDateUtc.Day.ToString() : eventContext.EventDateUtc.Day.ToString();
                var hour = eventContext.EventDateUtc.Hour < 10 ? "0" + eventContext.EventDateUtc.Hour.ToString() : eventContext.EventDateUtc.Hour.ToString();

                parts[3] = eventContext.EventDateUtc.Year.ToString();
                parts[4] = month;
                parts[5] = day;
                parts[6] = hour;
            }

            parts[7] = eventContext.MessageId + ".json";

            var key = string.Join(JoinChar, parts); // TODO - Performance
            return key.ToLower(); // TODO - Performance
        }

        private string GetPart(string input)
        {
            var part = string.IsNullOrEmpty(input) ? UnknownPart : RemoveSpaces(input);
            return IsPartValid(part) ? part : InvalidPart;
        }

        private string RemoveSpaces(string part)
        {
            if (part.IndexOf(' ') == -1)
                return part;

            return part.Replace(' ', '_'); // TODO - Performance - Span<T>
        }

        private bool IsPartValid(string input) => Regex.IsMatch(input, ValidKeyPartPattern);
    }

    public class S3ObjectKeyGeneratorNew
    {
        private const string ValidKeyPartPattern = "^[a-zA-Z0-9_]+$";
        //private const string UnknownPart = "unknown";
        private const string InvalidPart = "invalid";
        private const char JoinChar = '/';

        private static char[] UnknownPart = new char[] { 'u', 'n', 'k', 'n', 'o', 'w', 'n' };

        public string GenerateSafeObjectKey(EventContext eventContext)
        {
            var parts = new string[8];

            parts[0] = GetPart(eventContext.Product);
            parts[1] = GetPart(eventContext.SiteKey);
            parts[2] = GetPart(eventContext.EventName);

            if (eventContext.EventDateUtc == default)
            {
                parts[3] = UnknownPart;
                parts[4] = UnknownPart;
                parts[5] = UnknownPart;
                parts[6] = UnknownPart;
            }
            else
            {
                // pad with a zero if less than 10 which ensures a consistent length of two chars.
                var month = eventContext.EventDateUtc.Month < 10 ? "0" + eventContext.EventDateUtc.Month.ToString() : eventContext.EventDateUtc.Month.ToString();
                var day = eventContext.EventDateUtc.Day < 10 ? "0" + eventContext.EventDateUtc.Day.ToString() : eventContext.EventDateUtc.Day.ToString();
                var hour = eventContext.EventDateUtc.Hour < 10 ? "0" + eventContext.EventDateUtc.Hour.ToString() : eventContext.EventDateUtc.Hour.ToString();

                parts[3] = eventContext.EventDateUtc.Year.ToString();
                parts[4] = month;
                parts[5] = day;
                parts[6] = hour;
            }

            parts[7] = eventContext.MessageId + ".json";

            var key = string.Join(JoinChar, parts); // TODO - Performance
            return key.ToLower(); // TODO - Performance
        }

        private ReadOnlySpan<char> GetPart(ReadOnlySpan<char> input)
        {
            // todo - do valid check first against array of allowed chars
            // quicker in cases where it's invalid

            if (input.Length == 0 || MemoryExtensions.IsWhiteSpace(input))
                return UnknownPart;
            
            var part = RemoveSpaces(input);

            return part;

            //return IsPartValid(part) ? part : InvalidPart;
        }

        private ReadOnlySpan<char> RemoveSpaces(ReadOnlySpan<char> part)
        {
            if (part.IndexOf(' ') < 0)
                return part;

            if (part.Length < 128)
            { 
                Span<char> newPart = stackalloc char[part.Length];

                return newPart;
            }


            var indexOfSpace = 0;

            while(indexOfSpace >= 0)
            {
                indexOfSpace = part.IndexOf(' ');

                if
            }

            if (part.IndexOf(' ') == -1)
                return part;



            return part.Replace(' ', '_'); // TODO - Performance - Span<T>
        }

        //private bool IsPartValid(string input) => Regex.IsMatch(input, ValidKeyPartPattern);
    }

    public class EventContext
    {
        public DateTime EventDateUtc { get; set; }
        public string Product { get; set; }
        public string SiteKey { get; set; }
        public string EventName { get; set; }
        public string MessageId { get; set; }
    }
}
