﻿using System;
using Peach.Core.Analyzers;
using System.Xml;
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Indicate a class implements methods required
	/// to support PIT Parsing.
	/// </summary>
	/// <remarks>
	/// Any type that is marked with this attribute must implement
	/// the following methods:
	/// 
	/// public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
	/// 
	/// If unable to parse the current XML, just return null.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class PitParsableAttribute : Attribute
	{
		/// <summary>
		/// XML element name that corresponds to this type.
		/// </summary>
		public string xmlElementName { get; set; }

		/// <summary>
		/// Is this a top level XML element.
		/// </summary>
		public bool topLevel { get; set; }

		/// <summary>
		/// Indicate a class implements methods required
		/// to support PIT Parsing.
		/// </summary>
		/// <param name="xmlElementName">XML element name that corresponds to this type.</param>
		public PitParsableAttribute(string xmlElementName)
		{
			this.xmlElementName = xmlElementName;
			this.topLevel = false;
		}
	}

	// Top level PitParser has Dom parent and returns a DataModel
	public delegate DataModel PitParserTopLevelDelegate(PitParser context, XmlNode node, Dom.Dom parent);

	// Child level PitParser has DataElementContainer parent and returns a DataElement
	public delegate DataElement PitParserDelegate(PitParser context, XmlNode node, DataElementContainer parent);
}
