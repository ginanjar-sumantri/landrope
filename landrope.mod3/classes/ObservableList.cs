using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using MongoDB.Driver.Core;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace landrope.mod3
{
	//public class ObsListSerializer : IBsonSerializer
	//{
	//	public Type ValueType => typeof(object);

	//	public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	//	{
	//		context.DynamicArraySerializer = 
	//		var arr = BsonSerializer.Deserialize<object[]>(context.Reader);
	//	}

	//	public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
	//	{
	//		var arr = ((ObservableList<T>)value).ToArray().Clone() as T[];
	//		BsonSerializer.Serialize(context.Writer, typeof(T[]), arr);
	//	}
	//}

	//[BsonSerializer(typeof(ObsListSerializer))]
	//public interface IObservableList
	//{
	//	object FromArray(Array arr);
	//	Array ToArray(object obj);
	//}

	//public class ObservableList<T> : List<T>, IObservableList, INotifyCollectionChanged
	//{
	//	public event NotifyCollectionChangedEventHandler CollectionChanged;

	//	public ObservableList()
	//		:base()
	//	{

	//	}

	//	public ObservableList(IEnumerable<T> other)
	//		:base(other)
	//	{
	//	}

	//	new public void Add(T item)
	//	{
	//		base.Add(item);
	//		if (CollectionChanged.GetInvocationList().Any())
	//			CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(
	//				NotifyCollectionChangedAction.Add, new List<T> { item }, new List<T>())
	//				);
	//	}

	//	new public void Remove(T item)
	//	{
	//		base.Remove(item);
	//		if (CollectionChanged.GetInvocationList().Any())
	//			CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(
	//				NotifyCollectionChangedAction.Remove, new List<T>(), new List<T> { item})
	//				);
	//	}

	//	public object FromArray(Array arr)
	//	{
	//		var obj = new ObservableList<T>(arr.Cast<T>());
	//		return obj;
	//	}

	//	public Array ToArray()
	//	{
	//		throw new NotImplementedException();
	//	}
	//}
}