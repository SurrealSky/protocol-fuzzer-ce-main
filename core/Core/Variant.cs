﻿


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using Peach.Core.IO;

namespace Peach.Core
{
	/// <summary>
	/// Variant class emulates untyped scripting languages
	/// variables were typing can change as needed.  This class
	/// solves the problem of boxing internal types.  Instead
	/// explicit casts are used to access the value as needed.
	/// 
	/// TODO: Investigate implicit casting as well.
	/// TODO: Investigate deligates for type -> byte[] conversion.
	/// </summary>
	[Serializable]
	public class Variant : IXmlSerializable
	{
		public enum VariantType
		{
			Unknown,
			Int,
			Long,
			ULong,
			String,
			ByteString,
			BitStream,
			Boolean,
			Double,
		}

		VariantType _type = VariantType.Unknown;
		bool? _valueBool;
		int? _valueInt;
		long? _valueLong;
		ulong? _valueULong;
		string _valueString;
		byte[] _valueByteArray;
		BitwiseStream _valueBitStream = null;
		double? _valueDouble;

		public Variant()
		{
		}

		public Variant(bool v)
		{
			SetValue(v);
		}

		public Variant(int v)
		{
			SetValue(v);
		}

		public Variant(long v)
		{
			SetValue(v);
		}

		public Variant(ulong v)
		{
			SetValue(v);
		}

		public Variant(double v)
		{
			SetValue(v);
		}

		public Variant(string v)
		{
			SetValue(v);
		}

		public Variant(string v, string type)
		{
			switch (type.ToLower())
			{
				case "system.int32":
					SetValue(Int32.Parse(v, CultureInfo.InvariantCulture));
					break;
				case "system.double":
					SetValue(Double.Parse(v, CultureInfo.InvariantCulture));
					break;
				case "system.string":
					SetValue(v);
					break;
				case "system.boolean":
					SetValue(bool.Parse(v));
					break;
				default:
					throw new NotImplementedException("Value Type not implemented: " + type);
			}
		}

		public Variant(byte[] v)
		{
			SetValue(new BitStream(v));
		}

		public Variant(BitwiseStream v)
		{
			SetValue(v);
		}

		public VariantType GetVariantType()
		{
			return _type;
		}

		public void SetValue(int v)
		{
			_type = VariantType.Int;
			_valueInt = v;
			_valueString = null;
			_valueBitStream = null;
			_valueByteArray = null;
		}

		public void SetValue(long v)
		{
			_type = VariantType.Long;
			_valueLong = v;
			_valueString = null;
			_valueBitStream = null;
			_valueByteArray = null;
		}

		public void SetValue(ulong v)
		{
			_type = VariantType.ULong;
			_valueULong = v;
			_valueString = null;
			_valueBitStream = null;
			_valueByteArray = null;
		}

		public void SetValue(double v)
		{
			_type = VariantType.Double;
			_valueDouble = v;
			_valueString = null;
			_valueBitStream = null;
			_valueByteArray = null;
		}
		
		public void SetValue(string v)
		{
			_type = VariantType.String;
			_valueString = v;
			_valueBitStream = null;
			_valueByteArray = null;
		}

		//public void SetValue(byte[] v)
		//{
		//    _type = VariantType.ByteString;
		//    _valueByteArray = v;
		//    _valueString = null;
		//    _valueBitStream = null;
		//}

		public void SetValue(BitwiseStream v)
		{
			_type = VariantType.BitStream;
			_valueBitStream = v;
			_valueString = null;
			_valueByteArray = null;
		}

		public void SetValue(bool v)
		{
			_type = VariantType.Boolean;
			_valueBool = v;
			_valueString = null;
			_valueByteArray = null;
		}

		/// <summary>
		/// Access variant as an int value.
		/// </summary>
		/// <param name="v">Variant to cast</param>
		/// <returns>int representation of value</returns>
		public static explicit operator int(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");
			unchecked
			{
				switch (v._type)
				{
					case VariantType.Int:
						return (int)v._valueInt;
					case VariantType.Long:
						if (v._valueLong > int.MaxValue || v._valueLong < int.MinValue)
							throw new ApplicationException("Converting this long to an int would cause loss of data [" + v._valueLong + "]");

						return (int)v._valueLong;
					case VariantType.ULong:
						if (v._valueULong > int.MaxValue)
							throw new ApplicationException("Converting this ulong to an int would cause loss of data [" + v._valueULong + "]");

						return (int)v._valueULong;
					case VariantType.Double:
						if (v._valueDouble > int.MaxValue)
							throw new ApplicationException("Converting this double to an int would cause loss of data [" + v._valueULong + "]");

						return (int)v._valueDouble;
					case VariantType.String:
						if (v._valueString == string.Empty)
							return 0;

						return Convert.ToInt32(v._valueString, CultureInfo.InvariantCulture);
					case VariantType.ByteString:
						throw new NotSupportedException("Unable to convert byte[] to int type.");
					case VariantType.BitStream:
						throw new NotSupportedException("Unable to convert BitStream to int type.");
					default:
						throw new NotSupportedException("Unable to convert to unknown type.");
				}
			}
		}

		/// <summary>
		/// Access variant as an int value.
		/// </summary>
		/// <param name="v">Variant to cast</param>
		/// <returns>int representation of value</returns>
		public static explicit operator uint(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");
			unchecked
			{
				switch (v._type)
				{
					case VariantType.Int:
						if (v._valueLong < 0)
							throw new ApplicationException("Converting this long to an int would cause loss of data");

						return (uint)v._valueInt;
					case VariantType.Long:
						if (v._valueLong > uint.MaxValue || v._valueLong < uint.MinValue)
							throw new ApplicationException("Converting this long to an int would cause loss of data");

						return (uint)v._valueLong;
					case VariantType.ULong:
						if (v._valueULong > uint.MaxValue)
							throw new ApplicationException("Converting this ulong to an int would cause loss of data");

						return (uint)v._valueULong;
					case VariantType.Double:
						if (v._valueDouble > uint.MaxValue)
							throw new ApplicationException("Converting this double to an int would cause loss of data");

						return (uint)v._valueDouble;
					case VariantType.String:
						if (v._valueString == string.Empty)
							return 0;

						return Convert.ToUInt32(v._valueString, CultureInfo.InvariantCulture);
					case VariantType.ByteString:
						throw new NotSupportedException("Unable to convert byte[] to int type.");
					case VariantType.BitStream:
						throw new NotSupportedException("Unable to convert BitStream to int type.");
					default:
						throw new NotSupportedException("Unable to convert to unknown type.");
				}
			}
		}

		public static explicit operator long(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");

			unchecked
			{
				switch (v._type)
				{
					case VariantType.Int:
						unchecked
						{
							return (long)v._valueInt;
						}
					case VariantType.Long:
						unchecked
						{
							return (long)v._valueLong;
						}
					case VariantType.ULong:
						if (v._valueULong > long.MaxValue)
							throw new ApplicationException("Converting this ulong to a long would cause loss of data");

						unchecked
						{
							return (long)v._valueULong;
						}
					case VariantType.Double:
						if (v._valueDouble > long.MaxValue)
							throw new ApplicationException("Converting this double to a long would cause loss of data");

						unchecked
						{
							return (long)v._valueDouble;
						}
					case VariantType.String:
						if (v._valueString == string.Empty)
							return 0;

						return Convert.ToInt64(v._valueString, CultureInfo.InvariantCulture);
					case VariantType.ByteString:
						throw new NotSupportedException("Unable to convert byte[] to int type.");
					case VariantType.BitStream:
						throw new NotSupportedException("Unable to convert BitStream to int type.");
					default:
						throw new NotSupportedException("Unable to convert to unknown type.");
				}
			}
		}

		public static explicit operator ulong(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");

			unchecked
			{
				switch (v._type)
				{
					case VariantType.Int:
						return (ulong)v._valueInt;
					case VariantType.Long:
						if ((ulong)v._valueLong > ulong.MaxValue || v._valueLong < 0)
							throw new ApplicationException("Converting this long to a ulong would cause loss of data");

						return (ulong)v._valueLong;
					case VariantType.ULong:
						return (ulong)v._valueULong;
					case VariantType.Double:
						return (ulong)v._valueDouble;
					case VariantType.String:
						if (v._valueString == string.Empty)
							return 0;

						return Convert.ToUInt64(v._valueString, CultureInfo.InvariantCulture);
					case VariantType.ByteString:
						throw new NotSupportedException("Unable to convert byte[] to int type.");
					case VariantType.BitStream:
						throw new NotSupportedException("Unable to convert BitStream to int type.");
					default:
						throw new NotSupportedException("Unable to convert to unknown type.");
				}
			}
		}

		/// <summary>
		/// Access variant as an double value.
		/// </summary>
		/// <param name="v">Variant to cast</param>
		/// <returns>int representation of value</returns>
		public static explicit operator double(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");
			unchecked
			{
				switch (v._type)
				{
					case VariantType.Int:
						return (double)v._valueInt;
					case VariantType.Long:
						if (v._valueLong > double.MaxValue || v._valueLong < double.MinValue)
							throw new ApplicationException("Converting this long to an double would cause loss of data [" + v._valueLong + "]");

						return (double)v._valueLong;
					case VariantType.ULong:
						if (v._valueULong > double.MaxValue)
							throw new ApplicationException("Converting this ulong to an double would cause loss of data [" + v._valueULong + "]");

						return (double)v._valueULong;
					case VariantType.Double:
						return (double)v._valueDouble;
					case VariantType.String:
						if (v._valueString == string.Empty)
							return 0.0;

						return Convert.ToDouble(v._valueString, CultureInfo.InvariantCulture);
					case VariantType.ByteString:
						throw new NotSupportedException("Unable to convert byte[] to int type.");
					case VariantType.BitStream:
						throw new NotSupportedException("Unable to convert BitStream to int type.");
					default:
						throw new NotSupportedException("Unable to convert to unknown type.");
				}
			}
		}

		/// <summary>
		/// Access variant as string value.
		/// </summary>
		/// <param name="v">Variant to cast</param>
		/// <returns>string representation of value</returns>
		public static explicit operator string(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");

			switch (v._type)
			{
				case VariantType.Int:
					return Convert.ToString(v._valueInt, CultureInfo.InvariantCulture);
				case VariantType.Long:
					return Convert.ToString(v._valueLong, CultureInfo.InvariantCulture);
				case VariantType.ULong:
					return Convert.ToString(v._valueULong, CultureInfo.InvariantCulture);
				case VariantType.Double:
					return Convert.ToString(v._valueDouble, CultureInfo.InvariantCulture);
				case VariantType.String:
					return v._valueString;
				case VariantType.Boolean:
					return Convert.ToString(v._valueBool, CultureInfo.InvariantCulture);
				case VariantType.ByteString:
					throw new NotSupportedException("Unable to convert byte[] to string type.");
				case VariantType.BitStream:
					throw new NotSupportedException("Unable to convert BitStream to string type.");
				default:
					throw new NotSupportedException("Unable to convert to unknown type.");
			}
		}

		/// <summary>
		/// Access variant as byte[] value.  This type is currently limited
		/// as neather int or string's are properly cast to byte[] since 
		/// additional information is needed.
		/// 
		/// TODO: Investigate using deligates to handle conversion.
		/// </summary>
		/// <param name="v">Variant to cast</param>
		/// <returns>byte[] representation of value</returns>
		public static explicit operator byte[](Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");

			switch (v._type)
			{
				case VariantType.Int:
					throw new NotSupportedException("Unable to convert int to byte[] type.");
				case VariantType.Long:
					throw new NotSupportedException("Unable to convert long to byte[] type.");
				case VariantType.ULong:
					throw new NotSupportedException("Unable to convert ulong to byte[] type.");
				case VariantType.Double:
					throw new NotSupportedException("Unable to convert double to byte[] type.");
				case VariantType.String:
					throw new NotSupportedException("Unable to convert string to byte[] type.");
				case VariantType.ByteString:
					return v._valueByteArray;
				case VariantType.BitStream:
					throw new NotSupportedException("Unable to convert BitStream to byte[] type.");
				default:
					throw new NotSupportedException("Unable to convert to unknown type.");
			}
		}

		public static explicit operator BitwiseStream(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");

			switch (v._type)
			{
				case VariantType.Int:
					throw new NotSupportedException("Unable to convert int to BitStream type.");
				case VariantType.Long:
					throw new NotSupportedException("Unable to convert long to BitStream type.");
				case VariantType.ULong:
					throw new NotSupportedException("Unable to convert ulong to BitStream type.");
				case VariantType.Double:
					throw new NotSupportedException("Unable to convert double to BitStream type.");
				case VariantType.String:
					throw new NotSupportedException("Unable to convert string to BitStream type.");
				case VariantType.ByteString:
					throw new NotSupportedException("Unable to convert byte[] to BitStream type.");
				case VariantType.BitStream:
					return v._valueBitStream;
				default:
					throw new NotSupportedException("Unable to convert to unknown type.");
			}
		}

		public static explicit operator bool(Variant v)
		{
			if (v == null)
				throw new ApplicationException("Parameter v is null");

			switch (v._type)
			{
				case VariantType.Boolean:
					return v._valueBool.Value;
				case VariantType.Int:
					throw new NotSupportedException("Unable to convert int to bool type.");
				case VariantType.Long:
					throw new NotSupportedException("Unable to convert long to bool type.");
				case VariantType.ULong:
					throw new NotSupportedException("Unable to convert ulong to bool type.");
				case VariantType.Double:
					throw new NotSupportedException("Unable to convert double to bool type.");
				case VariantType.String:
					throw new NotSupportedException("Unable to convert string to bool type.");
				case VariantType.ByteString:
					throw new NotSupportedException("Unable to convert byte[] to bool type.");
				case VariantType.BitStream:
					throw new NotSupportedException("Unable to convert BitStream to bool type.");
				default:
					throw new NotSupportedException("Unable to convert unknown to bool type.");
			}
		}

		public static bool operator ==(Variant a, Variant b)
		{
			if (((object)a == null) && ((object)b == null))
				return true;

			if (((object)a == null) || ((object)b == null))
				return false;

			if (a.GetVariantType() == VariantType.BitStream && b.GetVariantType() == VariantType.BitStream)
			{
				BitStream aa = (BitStream)a;
				BitStream bb = (BitStream)b;

				if (aa.Length != bb.Length)
					return false;

				aa.Seek(0, System.IO.SeekOrigin.Begin);
				bb.Seek(0, System.IO.SeekOrigin.Begin);

				while (true)
				{
					int lhs = aa.ReadByte();
					int rhs = bb.ReadByte();

					if (lhs != rhs)
						return false;

					if (lhs == -1)
						break;
				}

				return true;
			}

			if (a.GetVariantType() == VariantType.BitStream || b.GetVariantType() == VariantType.BitStream)
				throw new NotSupportedException("Unable to compare BitStream to Non-BitStream.");

			string stra = (string)a;
			string strb = (string)b;

			if (stra.Equals(strb))
				return true;
			else
				return false;
		}

		public static bool operator !=(Variant a, Variant b)
		{
			return !(a == b);
		}

		private static string BitsToString(BitwiseStream bs)
		{
			if (bs.LengthBits == 0)
				return "";

			byte[] buf = new byte[32];
			long pos = bs.PositionBits;
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			int len = bs.Read(buf, 0, buf.Length);

			StringBuilder ret = new StringBuilder();
			if (len > 0)
				ret.AppendFormat("{0:x2}", buf[0]);

			int end = Math.Min(len, buf.Length);
			for (int i = 1; i < end; ++i)
				ret.AppendFormat(" {0:x2}", buf[i]);

			long lengthBits = bs.LengthBits;

			if ((len * 8) < lengthBits)
			{
				if (len < buf.Length)
				{
					ulong tmp;
					int bits = bs.ReadBits(out tmp, 64);
					System.Diagnostics.Debug.Assert(bits < 8);

					tmp <<= (8 - bits);

					if (len != 0)
						ret.Append(" ");

					ret.AppendFormat("{0:x2}", tmp);
					ret.AppendFormat(" (Len: {0} bits)", lengthBits);
				}
				else if ((lengthBits % 8) == 0)
				{
					ret.AppendFormat(".. (Len: {0} bytes)", lengthBits / 8);
				}
				else
				{
					ret.AppendFormat(".. (Len: {0} bits)", lengthBits);
				}
			}

			bs.SeekBits(pos, System.IO.SeekOrigin.Begin);

			return ret.ToString();
		}

		private static string BytesToString(byte[] buf)
		{
			if (buf.Length == 0)
				return "";

			StringBuilder ret = new StringBuilder();
			ret.AppendFormat("{0:x2}", buf[0]);

			int end = Math.Min(32, buf.Length);
			for (int i = 1; i < end; ++i)
				ret.AppendFormat(" {0:x2}", buf[i]);

			if (end != buf.Length)
				ret.AppendFormat(".. (Len: {0} bytes)", buf.Length);

			return ret.ToString();
		}

		public override bool Equals(object obj)
		{
			// This is a reference type so perform reference Equals
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			// This is a reference type so perform reference GetHashCode
			return base.GetHashCode();
		}

		public override string ToString()
		{
			switch (_type)
			{
				case VariantType.Int:
					return this._valueInt.Value.ToString(CultureInfo.InvariantCulture);
				case VariantType.Long:
					return this._valueLong.Value.ToString(CultureInfo.InvariantCulture);
				case VariantType.ULong:
					return this._valueULong.Value.ToString(CultureInfo.InvariantCulture);
				case VariantType.Double:
					return this._valueDouble.Value.ToString(CultureInfo.InvariantCulture);
				case VariantType.String:
					if (this._valueString.Length <= 80)
						return this._valueString.ToString();
					return _valueString.Substring(0, 64) + ".. (Len: " + _valueString.Length + " chars)";
				case VariantType.ByteString:
					return BytesToString(_valueByteArray);
				case VariantType.BitStream:
					return BitsToString(_valueBitStream);
				default:
					return base.ToString();
			}
		}

		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(System.Xml.XmlReader reader)
		{
			XmlSerializer serializer;

			if (!reader.Read())
				return;

			reader.ReadStartElement("type");
			_type = (VariantType) reader.ReadContentAsInt();
			reader.ReadEndElement();

			reader.ReadStartElement("value");
			
			switch (_type)
			{
				case VariantType.Int:
					_valueInt = reader.ReadContentAsInt();
					break;
				case VariantType.Long:
					_valueLong = reader.ReadContentAsLong();
					break;
				case VariantType.ULong:
					_valueULong = (ulong) reader.ReadContentAsLong();
					break;
				case VariantType.Double:
					_valueDouble = (double) reader.ReadContentAsDouble();
					break;
				case VariantType.String:
					_valueString = reader.ReadContentAsString();
					break;
				case VariantType.ByteString:
					serializer = new XmlSerializer(typeof(byte[]));
					_valueByteArray = (byte[])serializer.Deserialize(reader);
					break;
				case VariantType.BitStream:
					serializer = new XmlSerializer(typeof(BitwiseStream));
					_valueBitStream = (BitwiseStream) serializer.Deserialize(reader);
					break;
			}

			reader.ReadEndElement();
		}

		public void WriteXml(System.Xml.XmlWriter writer)
		{
			XmlSerializer serializer;

			writer.WriteStartElement("type");
			writer.WriteValue((int)_type);
			writer.WriteEndElement();

			writer.WriteStartElement("value");

			switch (_type)
			{
				case VariantType.Int:
					writer.WriteValue(_valueInt);
					break;
				case VariantType.Long:
					writer.WriteValue(_valueLong);
					break;
				case VariantType.ULong:
					writer.WriteValue(_valueULong);
					break;
				case VariantType.Double:
					writer.WriteValue(_valueDouble);
					break;
				case VariantType.String:
					writer.WriteValue(_valueString);
					break;
				case VariantType.ByteString:
					serializer = new XmlSerializer(typeof(byte[]));
					serializer.Serialize(writer, _valueByteArray);
					break;
				case VariantType.BitStream:
					serializer = new XmlSerializer(typeof(BitwiseStream));
					serializer.Serialize(writer, _valueBitStream);
					break;
			}

			writer.WriteEndElement();
		}
	}
}
