using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;


namespace landrope.modb
{
	public class Auth
	{
		protected readonly HttpClient http;

		public string key { get; set; }
		public bool? invalid { get; set; }

		public Auth() { }
		public Auth(HttpClient http)
		{
			this.http = http;
		}

		protected T AsyncWaiter<T>(Task<T> task)
		{
			task.Wait();
			if (!task.IsCompleted && task.Exception != null)
				throw task.Exception;
			return task.Result;
		}

		protected void AsyncWaiter(Task task)
		{
			task.Wait();
			if (!task.IsCompleted && task.Exception != null)
				throw task.Exception;
		}
	
		async protected Task<string> GetError(HttpResponseMessage result)
		{
			var msg = await result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(msg))
				msg = result.StatusCode.ToString("g");
			return msg;
		}
	}

	public class NamedAuth : Auth
	{
		public string identifier { get; set; }

		public NamedAuth() { }
		public NamedAuth(HttpClient http)
			: base(http)
		{
		}
	}

	public class User : NamedAuth
	{
		public string FullName { get; set; }

		public User() : base(){ }
		public User(HttpClient http)
			: base(http)
		{
		}

		private UserInRole_u[] _roles = null;
		public UserInRole_u[] roles
		{
			get
			{
				if (_roles == null & http!=null)
					GetRoles();
				return _roles ?? new UserInRole_u[0];
			}
		}

		private void GetRoles()
		{
			AsyncWaiter(GetRolesAsync());
		}

		async private Task GetRolesAsync()
		{
			var payload = new StringContent(key, Encoding.UTF8, "application/json");
			var result = await http.PostAsync("auth/user/role-list", payload);
			var json = await result.Content.ReadAsStringAsync();
			if (result.StatusCode != System.Net.HttpStatusCode.OK)
			{
				if (string.IsNullOrEmpty(json))
					json = result.StatusCode.ToString("g");
				throw new Exception(json);
			}
			_roles = JsonConvert.DeserializeObject<UserInRole_u[]>(json);
		}

		//public List<UserInRole>
		public (bool OK, string err) ResetPassword()
		{
			return AsyncWaiter<(bool, string)>(ResetPasswordAsync());
		}

		async public Task<(bool OK, string err)> ResetPasswordAsync()
		{
			try
			{
				var payload = new StringContent(key, Encoding.UTF8, "application/json");
				var result = await http.PostAsync("auth/user/password/reset", payload);
				if (result.StatusCode != System.Net.HttpStatusCode.OK)
					return (false, await GetError(result));
				return (true, null);
			}
			catch (Exception ex)
			{
				return (false, ex.Message);
			}
		}

		public string AddRole(Role role)
		{
			return AsyncWaiter<string>(AddRoleAsync(role));
		}

		async private Task<string> AddRoleAsync(Role role)
		{
			var keys = new Couple { key1 = key, key2 = role.key };
			var content = new StringContent(JsonConvert.SerializeObject(keys), Encoding.UTF8, "application/json");
			var result = await http.PostAsync("auth/user/add-role", content);
			if (result.StatusCode != System.Net.HttpStatusCode.OK)
				return await GetError(result);
			await GetRolesAsync();
			return null;
		}
	}



	public class Role : Auth
	{
		public string description { get; set; }

		public Role() : base() { }
		public Role(HttpClient http)
			: base(http)
		{
		}

		private UserInRole_r[] _users = null;
		public UserInRole_r[] users
		{
			get
			{
				if (_users == null & http != null)
					GetUsers();
				return _users ?? new UserInRole_r[0];
			}
		}

		private RoleAction_r[] _actions = null;
		public RoleAction_r[] actions
		{
			get
			{
				if (_actions == null & http != null)
					GetActions();
				return _actions ?? new RoleAction_r[0];
			}
		}

		public void GetUsers()
		{
			AsyncWaiter(GetUsersAsync());
		}

		async private Task GetUsersAsync()
		{
			var payload = new StringContent(key, Encoding.UTF8, "application/json");
			var result = await http.PostAsync("auth/role/user-list", payload);
			var json = await result.Content.ReadAsStringAsync();
			if (result.StatusCode != System.Net.HttpStatusCode.OK)
			{
				if (string.IsNullOrEmpty(json))
					json = result.StatusCode.ToString("g");
				throw new Exception(json);
			}
			_users = JsonConvert.DeserializeObject<UserInRole_r[]>(json);
		}

		public void GetActions()
		{
			AsyncWaiter(GetActionsAsync());
		}

		async private Task GetActionsAsync()
		{
			var payload = new StringContent(key, Encoding.UTF8, "application/json");
			var result = await http.PostAsync("auth/role/action-list", payload);
			var json = await result.Content.ReadAsStringAsync();
			if (result.StatusCode != System.Net.HttpStatusCode.OK)
			{
				if (string.IsNullOrEmpty(json))
					json = result.StatusCode.ToString("g");
				throw new Exception(json);
			}
			_actions = JsonConvert.DeserializeObject<RoleAction_r[]>(json);
		}
	}


	public class Action : NamedAuth
	{
		public string description { get; set; }

		public Action() : base() { }
		public Action(HttpClient http)
			: base(http)
		{
		}

		private RoleAction_a[] _roles = null;

		public RoleAction_a[] roles
		{
			get
			{
				if (_roles == null & http != null)
					GetRoles();
				return _roles ?? new RoleAction_a[0];
			}
		}
		public void GetRoles()
		{
			var task = GetRolesAsync();
			if (!task.IsCompleted && task.Exception != null)
				throw task.Exception;
		}

		async private Task GetRolesAsync()
		{
			var payload = new StringContent(key, Encoding.UTF8, "application/json");
			var result = await http.PostAsync("auth/action/role-list", payload);
			var json = await result.Content.ReadAsStringAsync();
			if (result.StatusCode != System.Net.HttpStatusCode.OK)
			{
				if (string.IsNullOrEmpty(json))
					json = result.StatusCode.ToString("g");
				throw new Exception(json);
			}
			_roles = JsonConvert.DeserializeObject<RoleAction_a[]>(json);
		}

		public string AddRole(Role role)
		{
			return AsyncWaiter<string>(AddRoleAsync(role));
		}

		async private Task<string> AddRoleAsync(Role role)
		{
			var keys = new Couple { key1 = key, key2 = role.key };
			var content = new StringContent(JsonConvert.SerializeObject(keys), Encoding.UTF8, "application/json");
			var result = await http.PostAsync("auth/user/add-role", content);
			if (result.StatusCode != System.Net.HttpStatusCode.OK)
				return await GetError(result);
			await GetRolesAsync();
			return null;
		}
	}


	#region supporting class
	public class UserInRole_u
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
		public Role role { get; set; }
	}

	public class UserInRole_r
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
		public User user { get; set; }
	}

	public class RoleAction_a : UserInRole_u
	{
	}

	public class RoleAction_r
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
		public Action action { get; set; }
	}

	public class Couple
	{
		public string key1 { get; set; }
		public string key2 { get; set; }
	}
	#endregion

}