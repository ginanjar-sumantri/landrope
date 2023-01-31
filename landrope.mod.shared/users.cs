using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace landrope.mod.shared
{
	public class Auth
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
	}

	public class NamedAuth : Auth
	{
		public string identifier { get; set; }
	}

	public class User : NamedAuth
	{
		public string FullName { get; set; }
	}



	public class Role : Auth
	{
		public string description { get; set; }
	}


	public class Action : NamedAuth
	{
		public string description { get; set; }
	}

	#region supporting class
	public class UserInRole_u
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
		public Role role { get; set; }
		public string description => role?.description;
	}

	public class UserInRole_r
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
		public User user { get; set; }
		public string username => user?.identifier;
		public string FullName => user?.FullName;
	}

	public class RoleAction_a : UserInRole_u
	{
	}

	public class RoleAction_r
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
		public Action action { get; set; }
		public string identifier => action?.identifier;
		public string description => action?.description;
	}

	public class Couple
	{
		public string key1 { get; set; }
		public string key2 { get; set; }
	}
	#endregion
}