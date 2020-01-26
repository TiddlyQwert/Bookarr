using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Datastore.Converters
{
    public class HeterogenousListConverter<TItem, TList> : JsonConverter<TList>
    where TList : IList<TItem>, new()
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(TList).IsAssignableFrom(typeToConvert);
        }

        public override TList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ValidateToken(reader, JsonTokenType.StartArray);

            var results = new TList();

            reader.Read(); // Advance to the first object after the StartArray token. This should be either a StartObject token, or the EndArray token. Anything else is invalid.

            while (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read(); // Move to property name
                ValidateToken(reader, JsonTokenType.PropertyName);

                var typename = reader.GetString();

                reader.Read(); // Move to start of object (stored in this property)
                ValidateToken(reader, JsonTokenType.StartObject); // Start of vehicle

                var concreteItemType = Type.GetType(typename, true);
                var item = (TItem)JsonSerializer.Deserialize(ref reader, concreteItemType, options);
                results.Add(item);

                reader.Read(); // Move past end of item object
                reader.Read(); // Move past end of 'wrapper' object
            }

            ValidateToken(reader, JsonTokenType.EndArray);

            return results;
        }

        public override void Write(Utf8JsonWriter writer, TList items, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var item in items)
            {
                var itemType = item.GetType();

                writer.WriteStartObject();
                writer.WritePropertyName(itemType.ToString());
                JsonSerializer.Serialize(writer, item, itemType, options);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        // Helper function for validating where you are in the JSON
        private void ValidateToken(Utf8JsonReader reader, JsonTokenType tokenType)
        {
            if (reader.TokenType != tokenType)
            {
                throw new JsonException($"Invalid token: Was expecting a '{tokenType}' token but received a '{reader.TokenType}' token");
            }
        }
    }
}
