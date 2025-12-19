using System.Text.Json;
using System.Text.Json.Serialization;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Custom_items
{
    public class FloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        
            => reader.GetSingle();

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            var rounded = decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);

            writer.WriteNumberValue(rounded);
        }
    }
}
