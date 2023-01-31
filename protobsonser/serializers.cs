using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.IO;
#if BINPAK
	using BinaryPack;
#elif SALAR
	using Salar.Bois;
	using Salar.BinaryBuffers;
	#if SALARCOMP
		using Salar.Bois.LZ4;
	#endif
#else
	using MongoDB.Bson;
	using MongoDB.Bson.IO;
	using MongoDB.Bson.Serialization;
	using MongoDB.Bson.Serialization.Serializers;
#endif

namespace protobsonser
{
	delegate void Serialize<T>(T obj, Stream output) where T : new();
	delegate T Deserialize<T>(Stream input) where T : new();
	public static class BsonSupport
	{
#if SALAR
		static class Proxy<T> where T : new()
		{
#if SALARCOMP
			public static BoisLz4Serializer boiser;
#else
		static BoisSerializer boiser;
#endif

			static Proxy()
			{
#if SALARCOMP
				BoisLz4Serializer.Initialize();
				boiser = new BoisLz4Serializer();
#else
			BoisSerializer.Initialize();
			boiser = new BoisSerializer();
#endif
			}

			public static void Serialize(T obj, Stream output) =>
#if SALARCOMP
				boiser.Pickle<T>(obj,output);
#else
				boiser.Serialize(obj, output);
#endif
			public static T Deserialize(Stream input) =>
#if SALARCOMP
				boiser.Unpickle<T>(input);
#else
				boiser.Deserialize<T>(input);
#endif
		}
#endif

#if BINPAK
		public static byte[] BsonSerialize<T>(this T obj) where T : new() =>
			obj==null? Array.Empty<byte>() : BinaryConverter.Serialize(obj);

		public static T BsonDeserialize<T>(this byte[] buffer) where T : new() =>
			buffer == null || buffer.Length == 0 ? default : BinaryConverter.Deserialize<T>(buffer);

		public static ByteString BsonSerializeBS<T>(this T obj) where T : new()
		{
			if (obj == null)
				return ByteString.Empty;
			var arr = BinaryConverter.Serialize(obj);
			return ByteString.CopyFrom(arr);
		}

		public static BytesValue BsonSerializeBV<T>(this T obj) where T : new()
		{
			if (obj == null)
				return new BytesValue { Value = ByteString.Empty };
			var arr = BinaryConverter.Serialize(obj);
			return new BytesValue { Value = ByteString.CopyFrom(arr) };
		}

		public static T BsonDeserializeBS<T>(this ByteString buffer) where T : new()
			=> buffer.Length == 0 ? default : BinaryConverter.Deserialize<T>(buffer.ToByteArray());

		public static T BsonDeserializeBV<T>(this BytesValue buffer) where T : new()
			=> buffer.Value.Length == 0 ? default : BinaryConverter.Deserialize<T>(buffer.Value.ToByteArray());

#elif SALAR
				public static byte[] BsonSerialize<T>(this T obj) where T : new()
		{
			using (var strm = new MemoryStream())
			{
				Proxy<T>.Serialize(obj, strm);
				return strm.GetBuffer();
			}
		}

		public static T BsonDeserialize<T>(this byte[] buffer) where T : new()
		{
			using (var strm = new MemoryStream(buffer))
			{
				return Proxy<T>.Deserialize(strm);
			}
		}

		public static ByteString BsonSerializeBS<T>(this T obj) where T : new()
		{
			if (obj == null)
				return ByteString.Empty;

			using (var strm = new MemoryStream())
			{
				Proxy<T>.Serialize(obj, strm);
				return ByteString.CopyFrom(strm.GetBuffer());
			}
		}

		public static BytesValue BsonSerializeBV<T>(this T obj) where T : new()
		{
			if (obj == null)
				return new BytesValue { Value = ByteString.Empty };
			using (var strm = new MemoryStream())
			{
				Proxy<T>.Serialize(obj, strm);
				return new BytesValue { Value = ByteString.CopyFrom(strm.GetBuffer()) };
			}
		}

		public static T BsonDeserializeBS<T>(this ByteString buffer) where T : new() =>
			(buffer.Length == 0)? default : BsonDeserialize<T>(buffer.ToByteArray());

		public static T BsonDeserializeBV<T>(this BytesValue buffer) where T : new() =>
			(buffer.Value.Length == 0)? default : BsonDeserialize<T>(buffer.Value.ToByteArray());

#else
		public static byte[] BsonSerialize<T>(this T obj) where T:class
		{
			if (obj == null)
				return new byte[0];
			var writer = new BsonBinaryWriter(new MemoryStream());
			BsonSerializer.Serialize<T>(writer, obj);
			return ((MemoryStream)writer.BaseStream).ToArray();
		}

		public static T BsonDeserialize<T>(this byte[] buffer) where T:class
			=> buffer==null || buffer.Length==0? default : BsonSerializer.Deserialize<T>(buffer);

		public static ByteString BsonSerializeBS<T>(this T obj) where T:class
		{
			if (obj == null)
				return ByteString.Empty;
			var writer = new BsonBinaryWriter(new MemoryStream());
			BsonSerializer.Serialize<T>(writer, obj);
			return ByteString.CopyFrom(((MemoryStream)writer.BaseStream).ToArray());
		}

		public static BytesValue BsonSerializeBV<T>(this T obj) where T : class
		{
			if (obj == null)
				return new BytesValue { Value = ByteString.Empty };
			var writer = new BsonBinaryWriter(new MemoryStream());
			BsonSerializer.Serialize<T>(writer, obj);
			return new BytesValue { Value = ByteString.CopyFrom(((MemoryStream)writer.BaseStream).ToArray()) };
		}

		public static T BsonDeserializeBS<T>(this ByteString buffer) where T : class
			=> buffer.Length==0 ? default : BsonSerializer.Deserialize<T>(buffer.ToByteArray());

		public static T BsonDeserializeBV<T>(this BytesValue buffer) where T : class
			=> buffer.Value.Length==0? default : BsonSerializer.Deserialize<T>(buffer.Value.ToByteArray());
#endif
	}
}
