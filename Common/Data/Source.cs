using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Common.Data
{
    public class Source
    {
        public int Id { get; set; }
        public float A { get; set; }
        public float B { get; set; }

        public static Source GetFakeData()
        {
            return new Faker<Source>()
                .RuleFor(trade => trade.A, f => f.Random.Float())
                .RuleFor(trade => trade.B, f => f.Random.Float())
                .Generate();
        }

        public static Source FromBytes(byte[] sourceAsBytes)
        {
            var source = Encoding.UTF8.GetString(sourceAsBytes) ?? string.Empty;
            return JsonSerializer.Deserialize<Source>(source) ??
                throw NewDeserializationException(
                    from: $"{nameof(sourceAsBytes)} {sourceAsBytes.GetType().Name}",
                    to: $"{typeof(Source).Name}");
        }

        private static SerializationException NewDeserializationException(string from, string to) =>
            new SerializationException($"Deserialization from '{from}' to '{to}' failed.");
    }
}
