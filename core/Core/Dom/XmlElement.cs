﻿


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Text;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using NLog;
using System.Security;
using System.IO;
using System.Reflection;

namespace Peach.Core.Dom
{
	[DataElement("XmlElement")]
	[PitParsable("XmlElement")]
	[Parameter("name", typeof(string), "Name of element", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("elementName", typeof(string), "Name of XML element")]
	[Parameter("ns", typeof(string), "XML Namespace", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class XmlElement : DataElementContainer
	{
		protected static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		string _elementName = null;
		string _ns = null;

		public string version { get; set; }
		public string encoding { get; set; }
		public string standalone { get; set; }

		public XmlElement()
		{
		}

		public XmlElement(string name)
			: base(name)
		{
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "XmlElement")
				return null;

			var xmlElement = DataElement.Generate<XmlElement>(node, parent);

			xmlElement.elementName = node.getAttrString("elementName");

			if (node.hasAttr("ns"))
				xmlElement.ns = node.getAttrString("ns");

			context.handleCommonDataElementAttributes(node, xmlElement);
			context.handleCommonDataElementChildren(node, xmlElement);
			context.handleDataElementContainer(node, xmlElement);

			return xmlElement;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("XmlElement");

			pit.WriteAttributeString("elementName", elementName);
			if(!string.IsNullOrEmpty(ns))
				pit.WriteAttributeString("ns", ns);

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			foreach (var child in this)
				child.WritePit(pit);

			pit.WriteEndElement();
		}


		/// <summary>
		/// XML Element tag name
		/// </summary>
		public virtual string elementName
		{
			get { return _elementName; }
			set
			{
				_elementName = value;
				// DefaultValue isn't used internally, but this makes the Validator show helpful text
				_defaultValue = new Variant("<{0}> Element".Fmt(value));
				Invalidate();
			}
		}

		/// <summary>
		/// XML Namespace for element
		/// </summary>
		public virtual string ns
		{
			get { return _ns; }
			set
			{
				_ns = value;
				Invalidate();
			}
		}

		protected static string ElemToStr(DataElement elem)
		{
			var iv = elem.InternalValue;
			if (iv.GetVariantType() != Variant.VariantType.BitStream)
				return (string)iv;

			var bs = elem.Value;
			var ret = new BitReader(bs).ReadString(Encoding.ISOLatin1);
			bs.Seek(0, System.IO.SeekOrigin.Begin);
			return ret;
		}

		protected static string ContToStr(DataElementContainer cont)
		{
			var sb = new StringBuilder();
			foreach (var item in cont)
				sb.Append(ElemToStr(item));
			return sb.ToString();
		}

		static DataElement ResolveChoices(DataElement elem)
		{
			while (true)
			{
				var asChoice = elem as Choice;
				if (asChoice == null)
					return elem;

				if (asChoice.SelectedElement == null)
					asChoice.SelectDefault();

				elem = asChoice.SelectedElement;
			}
		}

		protected void GenXmlNode(XmlDocument doc, XmlNode parent)
		{
			var node = doc.CreateElement(elementName, ns);
			parent.AppendChild(node);

			foreach (var childElem in this)
			{
				var child = ResolveChoices(childElem);

				var asAttr = child as XmlAttribute;
				if (asAttr != null && asAttr.attributeName != null)
				{
					var asStr = ContToStr(asAttr);

					// If the attribute is xmlns and the value is empty
					// we can't add it to the document w/o getting an
					// ArgumentException when generating the final xml
					if (asAttr.attributeName.Split(new[] { ':' })[0] == "xmlns" && string.IsNullOrEmpty(asStr))
						continue;

					var attr = doc.CreateAttribute(asAttr.attributeName, asAttr.ns);
					attr.Value = asStr;
					node.Attributes.Append(attr);
					continue;
				}

				var asElem = child as XmlElement;
				if (asElem != null && !asElem.mutationFlags.HasFlag(MutateOverride.TypeTransform))
				{
					asElem.GenXmlNode(doc, node);
					continue;
				}

				var asCData = child as XmlCharacterData;
				if (asCData != null && !asCData.mutationFlags.HasFlag(MutateOverride.TypeTransform))
				{
					asCData.GenXmlNode(doc, node);
					continue;
				}

				var text = doc.CreateTextNode(ElemToStr(child));
				node.AppendChild(text);
			}
		}

		public override Variant DefaultValue
		{
			get { return GenerateDefaultValue(); } 
			set { throw new NotSupportedException(); }
		}

		protected override Variant GenerateDefaultValue()
		{
			var enc = "utf-8";
			var doc = new XmlDocument();

			if (!string.IsNullOrEmpty(version) || !string.IsNullOrEmpty(encoding) || !string.IsNullOrEmpty(standalone))
			{
				var decl = doc.CreateXmlDeclaration(version, encoding, standalone);
				doc.AppendChild(decl);
				enc = encoding ?? enc;
			}

			GenXmlNode(doc, doc);

			var bs = new BitStream();
			var writer = new PeachXmlWriter(bs, enc);

			doc.WriteTo(writer);
			writer.Flush();
			bs.Seek(0, SeekOrigin.Begin);

			var reader = new BitReader(bs);
			return new Variant(reader.ReadString());
		}

		protected override Variant GenerateInternalValue()
		{
			if (mutationFlags.HasFlag(MutateOverride.TypeTransform))
				return MutatedValue;

			var enc = "utf-8";
			var doc = new XmlDocument();

			if (!string.IsNullOrEmpty(version) || !string.IsNullOrEmpty(encoding) || !string.IsNullOrEmpty(standalone))
			{
				var decl = doc.CreateXmlDeclaration(version, encoding, standalone);
				doc.AppendChild(decl);
				enc = encoding ?? enc;
			}

			GenXmlNode(doc, doc);

			var bs = new BitStream();
			var writer = new PeachXmlWriter(bs, enc);

			doc.WriteTo(writer);
			writer.Flush();
			bs.Seek(0, SeekOrigin.Begin);

			return new Variant(bs);
		}

		protected override BitwiseStream InternalValueToBitStream()
		{
			return (BitwiseStream)InternalValue;
		}

		class PeachXmlWriter : XmlTextWriter
		{
#if MONO
			private static readonly bool MonoRawMethod = false;

			static PeachXmlWriter()
			{
				var version = Platform.MonoRuntimeVersion;

				if (version.Major > 4 || (version.Major == 4 && version.Minor >= 2))
					MonoRawMethod = true;
			}
#endif

			public PeachXmlWriter(BitStream stream, string encoding)
				: base(stream, Encoding.GetEncoding(encoding).RawEncoding)
			{
			}

			public override void WriteString(string text)
			{
#if MONO
				if(MonoRawMethod)
				{
					var encoded = SecurityElement.Escape(text);
					char[] raw = encoded.ToCharArray();

					WriteRaw(raw, 0, raw.Length);
				}
				else
				{
					base.WriteString(text);
				}
#else
				var encoded = SecurityElement.Escape(text);
				char[] raw = encoded.ToCharArray();

				WriteRaw(raw, 0, raw.Length);
#endif
			}
		}
	}
}

// end
