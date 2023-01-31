using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using auth.mod;
using geo.shared;
using GeomHelper;
using landrope.mod;
using landrope.mod.shared;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Encryption;
using mongospace;
using Newtonsoft.Json;

namespace landrope.mod2
{
	public class Map
	{
		public string ID { get; set; }
		[BsonIgnore]
		public virtual Shapes areas //{ get; set; }
		{
			get => landbase.decode(careas);
			set
			{
				careas = landbase.encode(value);
			}
		}

		[JsonIgnore]
		public byte[] careas { get; set; } = new byte[0];


		public geoPoint Center { get; set; }

		public double Area { get; set; } = 0;
		public double TheArea => Area;

		public virtual gmapObject ToGmap(string persilkey)
		{
			if (Center == null || double.IsNaN(Center.Latitude) || double.IsNaN(Center.Longitude))
			{
				var gps = areas.SelectMany(a => a.coordinates).ToList();
				if (!gps.Any())
					return new geoFeature();
				var lon = gps.Average(p => p.Longitude);
				var lat = gps.Average(p => p.Latitude);
				Center = new geoPoint(lat, lon);
			}
			var feat = areas.ToGmap() as geoFeature;
			feat.properties.Add("key", persilkey);
			feat.properties.Add("-idx-lat", Center.Latitude);
			feat.properties.Add("-idx-lon", Center.Longitude);

			return feat;
		}
		public static string finestring(string st) => String.IsNullOrEmpty(st) ? "-" : st;

	}

	public class MapMeta
	{
		public int filesize { get; set; }
		public DateTime created { get; set; }
		public DateTime updated { get; set; }
		public string updater { get; set; }
	}

	public class MapEntry : ValidateEntry<Map>
	{
		public DateTime? uploaded { get; set; }
		public string keyUploader { get; set; }
		public MapMeta metadata { get; set; }

		public Map map { get; set; }

		public override Map MakeItemCopy()
		{
			return base.MakeItemCopy();
		}
	}

	[Entity("persilMap", "persilMaps")]
	public class persilMap : MongoEntity
	{
		[BsonIgnore]
		public string persilkey
		{
			get => key;
			set { key = persilkey; }
		}
		public Map current { get; set; }
		public MapEntry[] entries { get; set; } = new MapEntry[0];

		public void AddEntry(MapEntry entry)
		{
			var lst = entries.ToList();
			lst.Add(entry);
			entries = lst.ToArray();
		}

		public void Validate(user user, bool approved, string rejectnote)
		{
			foreach (var ent in entries)
			{
				if (ent.reviewed == null)
				{
					ent.reviewed = DateTime.Now;
					ent.keyReviewer = user.key;
					ent.approved = approved;
					ent.rejectNote = rejectnote;
				}
				if (approved)
				{
					var json = JsonConvert.SerializeObject(entries.Last().map);
					current = JsonConvert.DeserializeObject<Map>(json);
				}
			}
		}
	}
}
