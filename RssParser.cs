//
// RssParser.cs
//
// Author:
//       PremNath
//

using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LazyTableImages {

	public static class RssParser {

		// These are used to select the correct nodes and attributes from the Rss feed
		static readonly XName FeedElement = XName.Get ("feed", "http://www.w3.org/2005/Atom");
		static readonly XName EntryElement = XName.Get ("entry", "http://www.w3.org/2005/Atom");
		static readonly XName AppUrlElement = XName.Get ("id", "http://www.w3.org/2005/Atom");

		static readonly XName AppNameElement = XName.Get ("name", "http://itunes.apple.com/rss");
		static readonly XName ArtistElement = XName.Get ("artist", "http://itunes.apple.com/rss");
		static readonly XName ImageUrlElement = XName.Get ("image", "http://itunes.apple.com/rss");

		static readonly XName HeightAttribute = XName.Get ("height", "");

		public static List<App> Parse (string xml)
		{
			// Open the xml
			var doc = XDocument.Parse (xml);

			// We want to convert all the raw Xml nodes called 'entry' which
			// are in that namespace into instances of the 'App' class so they
			// can be displayed easily in the table.
			return doc.Element (FeedElement) // Select the 'feed' node.
				.Elements (EntryElement)     // Select all children with the name 'entry'.
				.Select (XmlElementToApp)    // Convert the 'entry' nodes to instances of the App class.
				.ToList ();                  // Return as a List<App>.
		}

		static App XmlElementToApp (XElement entry)
		{
			// The document may contain many image nodes. Select the one with
			// the largest resolution.
			var imageUrlNode = entry.Elements (ImageUrlElement)
				.Where (n => n.Attribute (HeightAttribute) != null)
				.OrderBy (node => int.Parse (node.Attribute (HeightAttribute).Value))
				.LastOrDefault ();

			// Parse the rest of the apps information from the XElement and
			// return the App instance.
			return new App {
				Name = entry.Element (AppNameElement).Value,
				Url = new Uri (entry.Element (AppUrlElement).Value),
				Artist = entry.Element (ArtistElement).Value,
				ImageUrl = new Uri (imageUrlNode.Value)
			};
		}
	}
}
