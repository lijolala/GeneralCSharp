﻿// <auto-generated />
//
// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using CodeBeautify;
//
//    var welcome2 = Welcome2.FromJson(jsonString);

namespace CodeBeautify
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class Welcome2
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("alerts")]
        public Alert[] Alerts { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }

    public partial class Alert
    {
        [JsonProperty("alertId")]
        public string AlertId { get; set; }

        [JsonProperty("watchlistsMatchedByType")]
        public WatchlistsMatchedByType[] WatchlistsMatchedByType { get; set; }

        [JsonProperty("availableRelatedAlerts")]
        public long AvailableRelatedAlerts { get; set; }

        [JsonProperty("eventTime")]
        public long EventTime { get; set; }

        [JsonProperty("eventVolume")]
        public long EventVolume { get; set; }

        [JsonProperty("eventLocation")]
        public EventLocation EventLocation { get; set; }

        [JsonProperty("source")]
        public Source Source { get; set; }

        [JsonProperty("post")]
        public Post Post { get; set; }

        [JsonProperty("caption")]
        public string Caption { get; set; }

        [JsonProperty("subCaption")]
        public SubCaption SubCaption { get; set; }

        [JsonProperty("companies")]
        public object[] Companies { get; set; }

        [JsonProperty("categories")]
        public Category[] Categories { get; set; }

        [JsonProperty("sectors")]
        public object[] Sectors { get; set; }

        [JsonProperty("headerColor")]
        public string HeaderColor { get; set; }

        [JsonProperty("headerLabel")]
        public string HeaderLabel { get; set; }

        [JsonProperty("alertType")]
        public AlertType AlertType { get; set; }

        [JsonProperty("publisherCategory")]
        public AlertType PublisherCategory { get; set; }

        [JsonProperty("eventMapSmallURL")]
        public Uri EventMapSmallUrl { get; set; }

        [JsonProperty("eventMapLargeURL")]
        public Uri EventMapLargeUrl { get; set; }

        [JsonProperty("expandAlertURL")]
        public Uri ExpandAlertUrl { get; set; }

        [JsonProperty("expandMapURL")]
        public string ExpandMapUrl { get; set; }

        [JsonProperty("relatedTerms")]
        public RelatedTerm[] RelatedTerms { get; set; }

        [JsonProperty("relatedTermsQueryURL")]
        public Uri RelatedTermsQueryUrl { get; set; }

        [JsonProperty("userRecentImages")]
        public object[] UserRecentImages { get; set; }

        [JsonProperty("userTopHashtags")]
        public object[] UserTopHashtags { get; set; }
    }

    public partial class AlertType
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("shortName", NullValueHandling = NullValueHandling.Ignore)]
        public string ShortName { get; set; }
    }

    public partial class Category
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("topicType")]
        public string TopicType { get; set; }

        [JsonProperty("id")]
       // [JsonConverter(typeof(PurpleParseStringConverter))]
        public long Id { get; set; }

        [JsonProperty("idStr")]
       // [JsonConverter(typeof(PurpleParseStringConverter))]
        public long IdStr { get; set; }

        [JsonProperty("requested", NullValueHandling = NullValueHandling.Ignore)]
      //  [JsonConverter(typeof(FluffyParseStringConverter))]
        public bool? Requested { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("retired")]
        public bool Retired { get; set; }
    }

    public partial class EventLocation
    {
        [JsonProperty("coordinates")]
        public double[] Coordinates { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("places")]
        public string[] Places { get; set; }

        [JsonProperty("probability")]
        public double Probability { get; set; }

        [JsonProperty("radius")]
        public double Radius { get; set; }
    }

    public partial class Post
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("languages")]
        public object[] Languages { get; set; }

        [JsonProperty("media")]
        public object[] Media { get; set; }

        [JsonProperty("link")]
        public Uri Link { get; set; }
    }

    public partial class RelatedTerm
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("link")]
        public Uri Link { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("channels")]
        public string[] Channels { get; set; }
    }

    public partial class SubCaption
    {
        [JsonProperty("bullets")]
        public Bullets Bullets { get; set; }
    }

    public partial class Bullets
    {
        [JsonProperty("media")]
        public string Media { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }

    public partial class WatchlistsMatchedByType
    {
        [JsonProperty("id")]
       // [JsonConverter(typeof(PurpleParseStringConverter))]
        public long Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("externalTopicIds")]
       // [JsonConverter(typeof(DecodeArrayConverter))]
        public long[] ExternalTopicIds { get; set; }

        [JsonProperty("userProperties")]
        public UserProperties UserProperties { get; set; }
    }

    public partial class UserProperties
    {
        [JsonProperty("omnilist")]
       // [JsonConverter(typeof(FluffyParseStringConverter))]
        public bool Omnilist { get; set; }
    }

    public partial class Welcome2
    {
        public static Welcome2 FromJson(string json) => JsonConvert.DeserializeObject<Welcome2>(json, CodeBeautify.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Welcome2 self) => JsonConvert.SerializeObject(self, CodeBeautify.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    //internal class PurpleParseStringConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

    //    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    //    {
    //        if (reader.TokenType == JsonToken.Null) return null;
    //        var value = serializer.Deserialize<string>(reader);
    //        long l;
    //        if (Int64.TryParse(value, out l))
    //        {
    //            return l;
    //        }
    //        throw new Exception("Cannot unmarshal type long");
    //    }

    //    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    //    {
    //        if (untypedValue == null)
    //        {
    //            serializer.Serialize(writer, null);
    //            return;
    //        }
    //        var value = (long)untypedValue;
    //        serializer.Serialize(writer, value.ToString());
    //        return;
    //    }

    //    public static readonly PurpleParseStringConverter Singleton = new PurpleParseStringConverter();
    //}

    //internal class FluffyParseStringConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type t) => t == typeof(bool) || t == typeof(bool?);

    //    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    //    {
    //        if (reader.TokenType == JsonToken.Null) return null;
    //        var value = serializer.Deserialize<string>(reader);
    //        bool b;
    //        if (Boolean.TryParse(value, out b))
    //        {
    //            return b;
    //        }
    //        throw new Exception("Cannot unmarshal type bool");
    //    }

    //    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    //    {
    //        if (untypedValue == null)
    //        {
    //            serializer.Serialize(writer, null);
    //            return;
    //        }
    //        var value = (bool)untypedValue;
    //        var boolString = value ? "true" : "false";
    //        serializer.Serialize(writer, boolString);
    //        return;
    //    }

    //    public static readonly FluffyParseStringConverter Singleton = new FluffyParseStringConverter();
    //}

    //internal class DecodeArrayConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type t) => t == typeof(long[]);

    //    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    //    {
    //        reader.Read();
    //        var value = new List<long>();
    //        while (reader.TokenType != JsonToken.EndArray)
    //        {
    //            var converter = PurpleParseStringConverter.Singleton;
    //            var arrayItem = (long)converter.ReadJson(reader, typeof(long), null, serializer);
    //            value.Add(arrayItem);
    //            reader.Read();
    //        }
    //        return value.ToArray();
    //    }

    //    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    //    {
    //        var value = (long[])untypedValue;
    //        writer.WriteStartArray();
    //        foreach (var arrayItem in value)
    //        {
    //            var converter = PurpleParseStringConverter.Singleton;
    //            converter.WriteJson(writer, arrayItem, serializer);
    //        }
    //        writer.WriteEndArray();
    //        return;
    //    }

    //    public static readonly DecodeArrayConverter Singleton = new DecodeArrayConverter();
    //}
}
