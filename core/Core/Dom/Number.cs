﻿


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using System.Globalization;

namespace Peach.Core.Dom
{
	/// <summary>
	/// A numerical data element.
	/// </summary>
	[DataElement("Number", DataElementTypes.NonDataElements)]
	[PitParsable("Number")]
	[DataElementChildSupported("Placement")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("size", typeof(uint), "Size in bits")]
	[Parameter("signed", typeof(bool), "Is number signed", "false")]
	[Parameter("endian", typeof(EndianType), "Byte order of number", "little")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(ValueType), "Format of value attribute", "string")]
	[Parameter("token", typeof(bool), "Is element a token", "false")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Number : DataElement
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		protected ulong _max = (ulong)sbyte.MaxValue;
		protected long _min = sbyte.MinValue;
		protected bool _signed = false;
		protected bool _isLittleEndian = true;
		protected Endian _endian = Endian.Little;

		public Number()
			: base()
		{
			lengthType = LengthType.Bits;
			length = 8;
			DefaultValue = new Variant(0);
		}

		public Number(string name)
			: base(name)
		{
			lengthType = LengthType.Bits;
			length = 8;
			DefaultValue = new Variant(0);
		}

		/// <summary>
		/// In the case of Double the values set
		/// by Number will cause an exception. This
		/// contructor is added as a work arround.
		/// </summary>
		/// <param name="nullCtor"></param>
		protected Number(bool nullCtor)
		{
			
		}

		/// <summary>
		/// In the case of Double the values set
		/// by Number will cause an exception. This
		/// contructor is added as a work arround.
		/// </summary>
		/// <param name="nullCtor"></param>
		/// <param name="name"></param>
		public Number(bool nullCtor, string name)
			: base(name)
		{
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Number")
				return null;

			var num = DataElement.Generate<Number>(node, parent);

			if (node.hasAttr("signed"))
				num.Signed = node.getAttrBool("signed");
			else
				num.Signed = context.getDefaultAttr(typeof(Number), "signed", num.Signed);

			if (node.hasAttr("size"))
			{
				int size = node.getAttrInt("size");

				if (size < 1 || size > 64)
					throw new PeachException(string.Format("Error, unsupported size '{0}' for {1}.", size, num.debugName));

				num.lengthType = LengthType.Bits;
				num.length = size;
			}

			string strEndian = null;
			if (node.hasAttr("endian"))
				strEndian = node.getAttrString("endian");
			if (strEndian == null)
				strEndian = context.getDefaultAttr(typeof(Number), "endian", null);

			if (strEndian != null)
			{
				switch (strEndian.ToLower())
				{
					case "little":
						num.LittleEndian = true;
						break;
					case "big":
						num.LittleEndian = false;
						break;
					case "network":
						num.LittleEndian = false;
						break;
					default:
						throw new PeachException(
							string.Format("Error, unsupported value '{0}' for 'endian' attribute on {1}.", strEndian, num.debugName));
				}
			}

			context.handleCommonDataElementAttributes(node, num);
			context.handleCommonDataElementChildren(node, num);
			context.handleCommonDataElementValue(node, num);

			return num;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Number");

			pit.WriteAttributeString("size", lengthAsBits.ToString(CultureInfo.InvariantCulture));
			pit.WriteAttributeString("signed", Signed.ToString().ToLower());

			if (!LittleEndian)
				pit.WriteAttributeString("endian", "big");

			WritePitCommonAttributes(pit);
			WritePitCommonValue(pit);
			WritePitCommonChildren(pit);

			pit.WriteEndElement();
		}


		public override long length
		{
			get
			{
				switch (_lengthType)
				{
					case LengthType.Bytes:
						return _length;
					case LengthType.Bits:
						return _length;
					case LengthType.Chars:
						throw new NotSupportedException("Length type of Chars not supported by Number.");
					default:
						throw new NotSupportedException("Error calculating length.");
				}
			}
			set
			{
				if (value <= 0 || value > 64)
					throw new ArgumentOutOfRangeException("value", value, "Value must be greater than 0 and less than 65.");

				base.length = value;

				if (_signed)
				{
					_max = (ulong)((ulong)1 << ((int)lengthAsBits - 1)) - 1;
					_min = 0 - (long)((ulong)1 << ((int)lengthAsBits - 1));
				}
				else
				{
					_max = (ulong)((ulong)1 << ((int)lengthAsBits - 1));
					_max += (_max - 1);
					_min = 0;
				}

				Invalidate();
			}
		}

		public override bool hasLength
		{
			get
			{
				return true;
			}
		}

		public override Variant DefaultValue
		{
			get
			{
				return base.DefaultValue;
			}
			set
			{
				base.DefaultValue = Sanitize(value);
			}
		}

		#region Sanitize

		private dynamic SanitizeString(string str)
		{
			string conv = str;
			NumberStyles style = NumberStyles.AllowLeadingSign;

			if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				conv = str.Substring(2);
				style = NumberStyles.AllowHexSpecifier;
			}

			if (Signed)
			{
				long value;
				if (long.TryParse(conv, style, CultureInfo.InvariantCulture, out value))
					return value;
			}
			else
			{
				ulong value;
				if (ulong.TryParse(conv, style, CultureInfo.InvariantCulture, out value))
					return value;
			}

			throw new PeachException(string.Format("Error, {0} value '{1}' could not be converted to a {2}-bit {3} number.", debugName, str, lengthAsBits, Signed ? "signed" : "unsigned"));
		}

		private dynamic SanitizeStream(BitwiseStream bs)
		{
			var pos = bs.PositionBits;

			try
			{
				if (bs.LengthBits < lengthAsBits || (bs.LengthBits + 7) / 8 != (lengthAsBits + 7) / 8)
					throw new PeachException(string.Format("Error, {0} value has an incorrect length for a {1}-bit {2} number, expected {3} bytes.", debugName, lengthAsBits, Signed ? "signed" : "unsigned", (lengthAsBits + 7) / 8));

				ulong extra;
				bs.ReadBits(out extra, (int)(bs.LengthBits - lengthAsBits));

				if (extra != 0)
					throw new PeachException(string.Format("Error, {0} value has an invalid bytes for a {1}-bit {2} number.", debugName, lengthAsBits, Signed ? "signed" : "unsigned"));

				return FromBitstream(bs);
			}
			finally
			{
				bs.PositionBits = pos;
			}
		}

		private dynamic FromBitstream(BitwiseStream bs)
		{
			ulong bits;
			int len = bs.ReadBits(out bits, (int)lengthAsBits);
			System.Diagnostics.Debug.Assert(len == lengthAsBits);

			if (Signed)
				return _endian.GetInt64(bits, (int)lengthAsBits);
			else
				return _endian.GetUInt64(bits, (int)lengthAsBits);
		}

		protected virtual Variant Sanitize(Variant variant)
		{
			dynamic value = GetNumber(variant);

			if (value < 0 && (long)value < MinValue)
				throw new PeachException(string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));
			if (value > 0 && (ulong)value > MaxValue)
				throw new PeachException(string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));

			if (Signed)
				return new Variant((long)value);
			else
				return new Variant((ulong)value);
		}

		private dynamic DoubleToInteger(double value)
		{
			if (Math.Floor(value) != value)
				throw new PeachException(string.Format("Error, {0} value '{1}' can not be converted to a {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));

			if (value < 0)
			{
				try
				{
					return Convert.ToInt64(value);
				}
				catch (OverflowException)
				{
					throw new PeachException(string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));
				}
			}
			else
			{
				try
				{
					return Convert.ToUInt64(value);
				}
				catch (OverflowException)
				{
					throw new PeachException(string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));
				}
			}
		}

		private dynamic GetNumber(Variant variant)
		{
			dynamic value = 0;

			switch (variant.GetVariantType())
			{
				case Variant.VariantType.String:
					value = SanitizeString((string)variant);
					break;
				case Variant.VariantType.ByteString:
					value = SanitizeStream(new BitStream((byte[])variant));
					break;
				case Variant.VariantType.BitStream:
					value = SanitizeStream((BitwiseStream)variant);
					break;
				case Variant.VariantType.Int:
				case Variant.VariantType.Long:
					value = (long)variant;
					break;
				case Variant.VariantType.ULong:
					value = (ulong)variant;
					break;
				case Variant.VariantType.Double:
					value = DoubleToInteger((double)variant);
					break;
				default:
					throw new ArgumentException("Variant type '" + variant.GetVariantType().ToString() + "' is unsupported.", "variant");
			}

			return value;
		}

		#endregion

		public bool Signed
		{
			get { return _signed; }
			set
			{
				_signed = value;
				length = length;

				Invalidate();
			}
		}

		public bool LittleEndian
		{
			get { return _isLittleEndian; }
			set
			{
				if (_isLittleEndian != value)
				{
					_isLittleEndian = value;
					_endian = value ? Endian.Little : Endian.Big;
					Invalidate();
				}
			}
		}

		public virtual ulong MaxValue
		{
			get { return _max; }
		}

		public virtual long MinValue
		{
			get { return _min; }
		}

		protected override BitwiseStream InternalValueToBitStream()
		{
			dynamic value = GetNumber(InternalValue);

			if (value > 0 && (ulong)value > MaxValue)
			{
				var msg = string.Format("{0} value '{1}' is greater than the maximum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned");

				if (isContolIteration)
					throw new SoftException(new OverflowException(msg));

				// If number is target of a relation, mutation could cause the value
				// to be larger than the valid max.  Throwing a soft exception will cause
				// the iteration to get skipped, this can happen a large percentage of the time
				// so until the mutators properly scope their max size based on numerical
				// limits of relationships, just ignore the overflow.
				// See peach-pro issue #411
				Logger.Trace(msg);

				value = MaxValue;
			}

			if (value < 0 && (long)value < MinValue)
			{
				var msg = string.Format("{0} value '{1}' is less than the minimum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned");

				if (isContolIteration)
					throw new SoftException(new OverflowException(msg));

				Logger.Trace(msg);

				value = MinValue;
			}

			ulong bits = _endian.GetBits(value, (int)lengthAsBits);

			var bs = new BitStream();
			bs.WriteBits(bits, (int)lengthAsBits);
			bs.Seek(0, System.IO.SeekOrigin.Begin);
			return bs;
		}

		bool isContolIteration
		{
			get
			{
				var dm = root as DataModel;
				return dm == null || dm.actionData == null || dm.actionData.action.parent.parent.parent.context.controlIteration;
			}
		}
	}
}

// end
