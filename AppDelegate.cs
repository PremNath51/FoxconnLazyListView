//
// AppDelegate.cs
//
// Author:
//       PremNath
//

using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UIKit;
using System.Net;
using System.Collections.ObjectModel;

namespace LazyTableImages {
	/// <summary>
	/// The UIApplicationDelegate for the application. This class is responsible for launching the
	/// User Interface of the application, as well as listening (and optionally responding) to
	/// application events from iOS.
	/// </summary>

	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate {

		static readonly Uri RssFeedUrl = new Uri ("http://phobos.apple.com/WebObjects/MZStoreServices.woa/ws/RSS/toppaidapplications/limit=75/xml");

		UINavigationController NavigationController { get; set; }

		RootViewController RootController { get; set; }

		public override UIWindow Window { get; set; }

		/// <summary>
		/// This method is invoked when the application has loaded and is ready to run. In this
		/// method you should instantiate the window, load the UI into it and then make the window
		/// visible.
		/// </summary>
		/// <remarks>
		/// You have 5 seconds to return from this method, or iOS will terminate your application.
		/// </remarks>
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Window = new UIWindow (UIScreen.MainScreen.Bounds);
			RootController = new RootViewController ("RootViewController", null);
			NavigationController = new UINavigationController (RootController);
			Window.RootViewController = NavigationController;

			// make the window visible
			Window.MakeKeyAndVisible ();

			BeginDownloading ();
			return true;
		}

		void BeginDownloading ()
		{
			// Show the user that data is about to be downloaded
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;

			// Retrieve the rss feed from the server
			var downloader = new GzipWebClient ();
			downloader.DownloadStringCompleted += DownloadCompleted;
			downloader.DownloadStringAsync (RssFeedUrl);
		}

		void DownloadCompleted (object sender, DownloadStringCompletedEventArgs e)
		{
			// The WebClient will invoke the DownloadStringCompleted event on a
			// background thread. We want to do UI updates with the result, so process
			// the result on the main thread.
			UIApplication.SharedApplication.BeginInvokeOnMainThread (() => {
				// First disable the download indicator
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;

				// Now handle the result from the WebClient
				if (e.Error != null) {
					DisplayError ("Warning", "The rss feed could not be downloaded: " + e.Error.Message);
				} else {
					try {
						RootController.Apps.Clear ();
						foreach (var v in RssParser.Parse (e.Result))
							RootController.Apps.Add (v);
					} catch {
						DisplayError ("Warning", "Malformed Xml was found in the Rss Feed.");
					}
				}
			});
		}

		void DisplayError (string title, string errorMessage, params object[] formatting)
		{
			var alert = new UIAlertView (title, string.Format (errorMessage, formatting), null, "ok", null);
			alert.Show ();
		}
	}
}
