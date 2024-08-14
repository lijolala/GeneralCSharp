using System.Collections.Generic;
using Newtonsoft.Json;

namespace Conn.RiskEventSource.Dataminr.ApiAccess.Models {
	public partial class DataminrRoot
	{
		[JsonProperty("data")]
		public DataminrData Data { get; set; }
	}

	public class DataminrData{
		[JsonProperty("alerts")]
		public IList<DataminrJson> Alerts { get; set; }
		[JsonProperty("from")]
		public string From { get; set; }
		[JsonProperty("to")]
		public string To { get; set; }
	}
	public class DataminrJson {
		[JsonProperty("alertId")]
		public string AlertID { get; set; }

		[JsonProperty("parentAlertId")]
		public string ParentAlertID { get; set; }

		[JsonProperty("displayTweet")]
		public DisplayTweet DisplayTweet { get; set; }

		[JsonProperty("eventTime")]
		public long? EventTime { get; set; }

		[JsonProperty("eventVolume")]
		public int? EventVolume { get; set; }

		[JsonProperty("eventLocation")]
		public EventLocation EventLocation { get; set; }

		[JsonProperty("categories")]
		public List<Category> Categories { get; set; }

		[JsonProperty("watchlistsMatchedByType")]
		public List<WatchlistsMatchedByType> WatchlistsMatchedByType { get; set; }

		[JsonProperty("headerColor")]
		public string HeaderColor { get; set; }

		[JsonProperty("headerLabel")]
		public string HeaderLabel { get; set; }

		[JsonProperty("alertType")]
		public AlertType AlertType { get; set; }

		[JsonProperty("publisherCategory")]
		public PublisherCategory PublisherCategory { get; set; }

		[JsonProperty("eventMapSmallURL")]
		public string EventMapSmallURL { get; set; }

		[JsonProperty("eventMapLargeURL")]
		public string EventMapLargeURL { get; set; }

		[JsonProperty("expandAlertURL")]
		public string ExpandAlertURL { get; set; }

		[JsonProperty("expandMapURL")]
		public string ExpandMapURL { get; set; }

		[JsonProperty("expandUserURL")]
		public string ExpandUserURL { get; set; }

		[JsonProperty("relatedTerms")]
		public List<RelatedTerm> RelatedTerms { get; set; }

		[JsonProperty("relatedTermsQueryURL")]
		public string RelatedTermsQueryURL { get; set; }

		[JsonProperty("userRecentImages")]
		public List<UserRecentImage> UserRecentImages { get; set; }

		[JsonProperty("userTopHashtags")]
		public List<UserTopHashtag> UserTopHashtags { get; set; }

		[JsonProperty("availableRelatedAlerts")]
		public int? AvailableRelatedAlerts { get; set; }
	}

	public class UserTopHashtag {
		[JsonProperty("text")]
		public string Text { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }
	}

	public class UserRecentImage {
		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("display_url")]
		public string DisplayUrl { get; set; }

		[JsonProperty("expanded_url")]
		public string ExpandedUrl { get; set; }

		[JsonProperty("media_url")]
		public string MediaUrl { get; set; }

		[JsonProperty("media_url_https")]
		public string MediaUrlHttps { get; set; }
	}

	public class RelatedTerm {
		[JsonProperty("text")]
		public string Text { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }
	}

	public class PublisherCategory {
		[JsonProperty("id")]
		public string ID { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("color")]
		public string Color { get; set; }

		[JsonProperty("shortName")]
		public string ShortName { get; set; }
	}

	public class AlertType {
		[JsonProperty("id")]
		public string ID { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("color")]
		public string Color { get; set; }
	}

	public class WatchlistsMatchedByType {
		[JsonProperty("id")]
		public string ID { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("userProperties")]
		public UserProperties UserProperties { get; set; }
	}

	public class UserProperties {
	}

	public class Category {
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("topicType")]
		public string TopicType { get; set; }

		[JsonProperty("id")]
		public string ID { get; set; }

		[JsonProperty("idStr")]
		public string IDStr { get; set; }

		[JsonProperty("requested")]
		public string Requested { get; set; }

		[JsonProperty("path")]
		public string Path { get; set; }
	}

	public class EventLocation {
		[JsonProperty("coordinates")]
		public List<double?> Coordinates { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("places")]
		public List<string> Places { get; set; }

		[JsonProperty("radius")]
		public double? Radius { get; set; }
	}

	public class DisplayTweet {
		[JsonProperty("created_at")]
		public string CreatedAt { get; set; }

		[JsonProperty("source")]
		public string Source { get; set; }

		[JsonProperty("text")]
		public string Text { get; set; }

		[JsonProperty("id")]
		public long? ID { get; set; }

		[JsonProperty("id_str")]
		public string IDStr { get; set; }

		[JsonProperty("user")]
		public User User { get; set; }

		[JsonProperty("userV2")]
		public UserV2 UserV2 { get; set; }

		[JsonProperty("timestamp")]
		public long? Timestamp { get; set; }

		[JsonProperty("lang")]
		public string Lang { get; set; }

		[JsonProperty("langs")]
		public List<Lang> Langs { get; set; }

		[JsonProperty("translatedText")]
		public string TranslatedText { get; set; }

		[JsonProperty("translatedLang")]
		public string TranslatedLang { get; set; }

		[JsonProperty("entities")]
		public Entities Entities { get; set; }

		[JsonProperty("isDeleted")]
		public bool? IsDeleted { get; set; }
	}

	public class Entities {
		[JsonProperty("urls")]
		public List<object> Urls { get; set; }

		[JsonProperty("media")]
		public List<Medium> Media { get; set; }
	}

	public class Medium {
		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("display_url")]
		public string DisplayUrl { get; set; }

		[JsonProperty("expanded_url")]
		public string ExpandedUrl { get; set; }

		[JsonProperty("media_url")]
		public string MediaUrl { get; set; }

		[JsonProperty("media_url_https")]
		public string MediaUrlHttps { get; set; }
	}

	public class Lang {
		[JsonProperty("lang")]
		public string Language { get; set; }

		[JsonProperty("indices")]
		public List<int?> Indices { get; set; }
	}

	public class UserV2 {
		[JsonProperty("id")]
		public string ID { get; set; }

		[JsonProperty("display_name")]
		public string DisplayName { get; set; }

		[JsonProperty("entity_name")]
		public string EntityName { get; set; }

		[JsonProperty("image")]
		public string Image { get; set; }

		[JsonProperty("thumbnail")]
		public string Thumbnail { get; set; }

		[JsonProperty("channels")]
		public List<object> Channels { get; set; }
	}

	public class User {
		[JsonProperty("created_at")]
		public string CreatedAt { get; set; }

		[JsonProperty("id")]
		public long? ID { get; set; }

		[JsonProperty("id_str")]
		public string IDStr { get; set; }

		[JsonProperty("screen_name")]
		public string ScreenName { get; set; }

		[JsonProperty("statuses_count")]
		public string StatusesCount { get; set; }

		[JsonProperty("followers_count")]
		public int? FollowersCount { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("profile_image_url")]
		public string ProfileImageUrl { get; set; }

		[JsonProperty("flags")]
		public List<object> Flags { get; set; }

		[JsonProperty("hashtags")]
		public List<string> Hashtags { get; set; }

		[JsonProperty("recentMedia")]
		public List<RecentMedia> RecentMedia { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("verified")]
		public bool? Verified { get; set; }

		[JsonProperty("location")]
		public string Location { get; set; }

		[JsonProperty("restricted")]
		public bool? Restricted { get; set; }

		[JsonProperty("isUnavailable")]
		public bool? IsUnavailable { get; set; }
	}

	public class RecentMedia {
		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("display_url")]
		public string DisplayUrl { get; set; }

		[JsonProperty("expanded_url")]
		public string ExpandedUrl { get; set; }

		[JsonProperty("media_url")]
		public string MediaUrl { get; set; }

		[JsonProperty("media_url_https")]
		public string MediaUrlHttps { get; set; }
	}

	// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
	public class Properties	{
		[JsonProperty("watchlistColor")]
		public string WatchlistColor { get; set; }
	}

	public class DataminrList{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("properties")]
		public Properties Properties { get; set; }
	}

	public class Watchlists{
		[JsonProperty("Custom")]
		public IList<DataminrList> Custom { get; set; }
		[JsonProperty("Topic")]
		public IList<DataminrList> Topic { get; set; }
		[JsonProperty("Company")]
		public IList<DataminrList> Company { get; set; }
	}

	public class WatchListRoot {
		[JsonProperty("watchlists")]
		public Watchlists Watchlists { get; set; }
	}
}