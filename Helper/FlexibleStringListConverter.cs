using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SankalpAPI.Helper
{
    /// <summary>
    /// Custom System.Text.Json converter that accepts a string ("1,2,3,4,5"),
    /// an array ([1, 2, 3] or ["1", "2"]), or a number (1), and normalizes it to a comma-separated string.
    /// </summary>
    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long longVal))
                {
                    return longVal.ToString();
                }
                return reader.GetDouble().ToString();
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var items = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var str = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(str)) items.Add(str.Trim());
                    }
                    else if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetInt64(out long num)) items.Add(num.ToString());
                    }
                }
                return string.Join(",", items);
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    /// <summary>
    /// Custom Newtonsoft.Json converter that normalizes arrays, strings, or numbers to a comma-separated string.
    /// </summary>
    public class FlexibleNewtonsoftStringConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == Newtonsoft.Json.JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == Newtonsoft.Json.JsonToken.StartArray)
            {
                var items = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == Newtonsoft.Json.JsonToken.EndArray)
                    {
                        break;
                    }

                    var val = reader.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) items.Add(val.Trim());
                }
                return string.Join(",", items);
            }

            return reader.Value?.ToString();
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }

    /// <summary>
    /// Custom System.Text.Json converter that flexibly handles:
    /// - Array of numbers: [1, 2, 3]
    /// - Array of strings: ["1", "2", "3"]
    /// - Comma-separated string: "1,2,3,4,5"
    /// - Single number or string: 1 or "1"
    /// into a List&lt;string&gt;.
    /// </summary>
    public class FlexibleStringListConverter : JsonConverter<List<string>>
    {
        public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var list = new List<string>();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var val = reader.GetString();
                        AddSplitValues(list, val);
                    }
                    else if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetInt64(out long longVal))
                        {
                            list.Add(longVal.ToString());
                        }
                        else if (reader.TryGetDouble(out double doubleVal))
                        {
                            list.Add(doubleVal.ToString());
                        }
                    }
                }
                return list;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var val = reader.GetString();
                AddSplitValues(list, val);
                return list;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long longVal))
                {
                    list.Add(longVal.ToString());
                }
                else if (reader.TryGetDouble(out double doubleVal))
                {
                    list.Add(doubleVal.ToString());
                }
                return list;
            }

            return list;
        }

        private static void AddSplitValues(List<string> list, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    list.Add(part);
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }

    /// <summary>
    /// Custom Newtonsoft.Json converter providing the same flexible list parsing.
    /// </summary>
    public class FlexibleNewtonsoftStringListConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(List<string>);

        public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == Newtonsoft.Json.JsonToken.Null)
            {
                return null;
            }

            var list = new List<string>();

            if (reader.TokenType == Newtonsoft.Json.JsonToken.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == Newtonsoft.Json.JsonToken.EndArray)
                    {
                        break;
                    }

                    var val = reader.Value?.ToString();
                    AddSplitValues(list, val);
                }
                return list;
            }

            if (reader.Value != null)
            {
                AddSplitValues(list, reader.Value.ToString());
            }

            return list;
        }

        private static void AddSplitValues(List<string> list, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    list.Add(part);
                }
            }
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// Custom System.Text.Json converter that flexibly handles:
    /// - Single number: 23 -> [23]
    /// - Single numeric string: "23" -> [23]
    /// - Array of numbers: [23, 24] -> [23, 24]
    /// - Array of strings: ["23", "24"] -> [23, 24]
    /// - Comma-separated string: "23,24" -> [23, 24]
    /// into a List&lt;long&gt;.
    /// </summary>
    public class FlexibleLongListConverter : JsonConverter<List<long>>
    {
        public override List<long>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new List<long>();
            }

            var list = new List<long>();

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long num))
                {
                    list.Add(num);
                }
                return list;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var val = reader.GetString();
                AddSplitLongs(list, val);
                return list;
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetInt64(out long num))
                        {
                            list.Add(num);
                        }
                    }
                    else if (reader.TokenType == JsonTokenType.String)
                    {
                        var str = reader.GetString();
                        AddSplitLongs(list, str);
                    }
                }
                return list;
            }

            return list;
        }

        private static void AddSplitLongs(List<long> list, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (long.TryParse(part, out long parsed) && !list.Contains(parsed))
                {
                    list.Add(parsed);
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, List<long> value, JsonSerializerOptions options)
        {
            if (value == null || value.Count == 0)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }

            if (value.Count == 1)
            {
                writer.WriteNumberValue(value[0]);
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in value)
                {
                    writer.WriteNumberValue(item);
                }
                writer.WriteEndArray();
            }
        }
    }

    /// <summary>
    /// Custom Newtonsoft.Json converter that flexibly handles single number, string, or array into List&lt;long&gt;.
    /// </summary>
    public class FlexibleNewtonsoftLongListConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(List<long>);

        public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == Newtonsoft.Json.JsonToken.Null)
            {
                return new List<long>();
            }

            var list = new List<long>();

            if (reader.TokenType == Newtonsoft.Json.JsonToken.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == Newtonsoft.Json.JsonToken.EndArray)
                    {
                        break;
                    }

                    if (reader.Value != null && long.TryParse(reader.Value.ToString(), out long num))
                    {
                        if (!list.Contains(num)) list.Add(num);
                    }
                }
                return list;
            }

            if (reader.TokenType == Newtonsoft.Json.JsonToken.Integer)
            {
                if (reader.Value != null && long.TryParse(reader.Value.ToString(), out long num))
                {
                    list.Add(num);
                }
                return list;
            }

            if (reader.Value != null)
            {
                var parts = reader.Value.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts != null)
                {
                    foreach (var part in parts)
                    {
                        if (long.TryParse(part, out long parsed) && !list.Contains(parsed))
                        {
                            list.Add(parsed);
                        }
                    }
                }
            }

            return list;
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is List<long> list)
            {
                if (list.Count == 1)
                {
                    writer.WriteValue(list[0]);
                }
                else
                {
                    serializer.Serialize(writer, value);
                }
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}
