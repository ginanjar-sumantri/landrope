using maps.mod;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Json;

namespace maploader
{
	public class Loader
	{
		public static (DesaFeature[] desas, LandFeature[] lands) GetFeatures(string token, params string[] keys)
		{
			var pkeys = string.Join(",", keys);

			var client = new HttpClient();
			client.BaseAddress = new Uri("http://10.10.1.80:7879");
			
			var result = client.PostAsync($"api/reporting/map2?token={token}&key={pkeys}",new ByteArrayContent(new byte[] { 0 }))
				.GetAwaiter().GetResult();
			if (result.StatusCode != System.Net.HttpStatusCode.OK)
				return (new DesaFeature[0], new LandFeature[0]);

			var txt = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			var data = JsonConvert.DeserializeAnonymousType(txt, new { d = "", l = "" });
			return (FeatureBase.Deserialize64<DesaFeature>(data.d), FeatureBase.Deserialize64<LandFeature>(data.l));
		}
	}
}
