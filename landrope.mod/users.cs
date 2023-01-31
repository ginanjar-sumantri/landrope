using core.helpers;
using Google.Api;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using mongospace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace landrope.mod
{
	[BsonKnownTypes(typeof(User),typeof(Role))]
	[Entity("Auth", "auths")]
	public class Auth
	{
		[BsonRequired]
		public BsonObjectId _id { get; set; }
		[BsonRequired]
		public string key { get; set; }
		[BsonRequired]
		public string identifier { get; set; }
	}

	[Entity("User", "auths")]
	public class User : Auth
	{
		[BsonIgnore]
		[JsonIgnore]
		public string username {
			get => identifier;
			set { identifier = value; }
		}
		[BsonRequired]
		public string name { get; set; }
		[BsonRequired]
		[JsonIgnore]
		public byte[] password { get; set; }
		[BsonRequired]
		[JsonIgnore]
		public string[] rolekeys { get; set; } = new string[0];
		public List<Role> GetRoles(LandropeContext context)
		{
			var keys = string.Join(",", rolekeys.Select(k => "'" + k + "'"));
			return context.GetCollections(new Role(), "auths", $"{{_t:'Role', key:{{$in:[{keys}]}}").ToList();
		}

		public bool TestPassword(string pwd) =>decode(password)?.Replace(SubKey, "") == pwd;

		public bool SetPassword(string oldpwd, string newpwd)
		{
			if (!TestPassword(oldpwd))
				return false;
			password = encode(constructPwd(newpwd));
			token = null;
			tokenend = null;
			return true;
		}

		Random rnd = new Random(DateTime.Now.GetHashCode());
		string constructPwd(string pwd) => (pwd??"123456").Insert(rnd.Next(0, pwd.Length), SubKey);

		string SubKey => $"$>>>{key}>>>^";

		static (char f, char r)[] fwd = {('A','M'),('B','a'),('C','f'),('D','N'),('E','5'),('F','R'),('G','A'),('H','v'),
																('I','2'),('J','B'),('K','C'),('L','g'),('M','D'),('N','e'),('O','k'),('P','E'),
																('Q','b'),('R','F'),('S','d'),('T','G'),('U','6'),('V','='),('W','H'),('X','S'),
																('Y','I'),('Z','J'),('a','K'),('b','L'),('c','O'),('d','t'),('e','P'),('f','T'),
																('g','U'),('h','p'),('i','V'),('j','W'),('k','X'),('l','Y'),('m','Z'),('n','c'),
																('o','h'),('p','i'),('q','j'),('r','l'),('s','m'),('t','n'),('u','o'),('v','q'),
																('w','r'),('x','s'),('y','u'),('z','Q'),('0','w'),('1','x'),('2','y'),('3','z'),
																('4','0'),('5','1'),('6','3'),('7','4'),('8','7'),('9','+'),('+','8'),('=','9')};
		byte[] encode(string str)
		{
			if (str == null)
				return null;
			if (str == "")
				return new byte[0];
			var raw1 = ASCIIEncoding.ASCII.GetBytes(str);
			var str1 = Convert.ToBase64String(raw1);
			var chrs1 = forward(str1);
			var raw2 = ASCIIEncoding.ASCII.GetBytes(chrs1);
			var str2 = Convert.ToBase64String(raw2);
			var chrs2 = forward(str2);
			return ASCIIEncoding.ASCII.GetBytes(chrs2);
		}

		string decode(byte[] byt)
		{
			if (byt == null)
				return null;
			if (byt.Length == 0)
				return "";
			var chrs2 = ASCIIEncoding.ASCII.GetChars(byt);
			var str2 = reverse(chrs2);
			var raw2 = Convert.FromBase64CharArray(str2,0,str2.Length);
			var chrs1 = ASCIIEncoding.ASCII.GetChars(raw2);
			var str1 = reverse(chrs1);
			var raw1 = Convert.FromBase64CharArray(str1, 0, str1.Length);
			return ASCIIEncoding.ASCII.GetString(raw1);
		}

		char[] forward(string st) => st.Select(c => fwd.First(cc => cc.f == c).r).ToArray();
		char[] reverse(string st) => st.Select(c => fwd.First(cc => cc.r == c).f).ToArray();
		char[] reverse(char[] st) => st.Select(c => fwd.First(cc => cc.r == c).f).ToArray();

		public bool ResetPassword()
		{
			password = encode(constructPwd("123456"));
			token = null;
			tokenend = null;
			return true;
		}

		[BsonRequired]
		[JsonIgnore]
		public string token { get; set; } = null;
		[BsonRequired]
		[JsonIgnore]
		public DateTime? tokenend { get; set; } = null;

		public void SetToken(string newtoken, TimeSpan ts)
		{
			this.token = newtoken;
			tokenend = DateTime.Now + ts;
		}

		public bool ExtendToken(TimeSpan ts)
		{
			if (token == null || tokenend == null || tokenend < DateTime.Now)
				return false;

			tokenend = DateTime.Now + ts;
			return true;
		}

		public static string CreateToken() => MongoEntity.MakeKey + "99" + MongoEntity.MakeKey;
	}

	[Entity("Role", "auths")]
	public class Role : Auth
	{
		public string description { get; set; }
	}

}
