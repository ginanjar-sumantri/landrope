//using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace landrope.mod2
{
	//public interface IConvertible<T> where T:class
	//{
	//	void PreForward();
	//	void PostForward(T toObj);

	//	void PreBackward(T obj);

	//	void PostBackward(T obj);
	//}

	public static class Converter
	{
		public static TOut Forward<TIn,TOut>(TIn obj) where TIn: class where TOut :class
		{
			if (obj == null)
				return null;
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
			var newobj = Newtonsoft.Json.JsonConvert.DeserializeObject<TOut>(json);
			return newobj;
		}

		public static TIn Reverse<TIn, TOut>(TOut obj) where TIn : class where TOut : class
		{
			if (obj == null)
				return null;
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
			var newobj = Newtonsoft.Json.JsonConvert.DeserializeObject<TIn>(json);
			return newobj;
		}
	}

}
