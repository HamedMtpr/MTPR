using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mtpr.Core.Repositories;

public class CriteriaFilterConverter : JsonConverter<FilterCriteriaDto>
{
    public override FilterCriteriaDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        FilterCriteriaDto filter = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return filter;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case nameof(FilterCriteria.PropertyName):
                    filter.PropertyName = reader.GetString();
                    break;

                case nameof(FilterCriteria.FilterOperation):
                    filter.FilterOperation = reader.GetString();
                    break;

                case nameof(FilterCriteria.Value):
                    filter.Value = readValue(ref reader);
                    break;

                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Invalid Filter Criteria Json");
    }

    public override void Write(Utf8JsonWriter writer, FilterCriteriaDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(FilterCriteria.PropertyName), value.PropertyName);
        writer.WriteString(nameof(FilterCriteria.FilterOperation), value.FilterOperation);
        writer.WritePropertyName(nameof(FilterCriteria.Value));
        JsonSerializer.Serialize(writer, value.Value, typeof(object), options);
        writer.WriteEndObject();
    }

    private static object? readValue(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Number:
                return reader.GetDouble();

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.Null:
                return null;

            case JsonTokenType.StartArray:
                var items = new List<object?>();

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    items.Add(readValue(ref reader));

                return items;

            default:
                throw new JsonException($"Unsupported filter value token '{reader.TokenType}'.");
        }
    }
}
