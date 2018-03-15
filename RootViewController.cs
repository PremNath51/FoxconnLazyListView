//
// RootViewController.cs
//
// Author:
//       PremNath
//

using System;
using CoreGraphics;
using System.Linq;
using UIKit;
using Foundation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace LazyTableImages {

	public partial class RootViewController : UITableViewController {

		public ObservableCollection<App> Apps { get; private set; }

		public RootViewController (string nibName, NSBundle bundle) : base (nibName, bundle)
		{
			Apps = new ObservableCollection<App> ();
			Title = NSBundle.MainBundle.LocalizedString ("Lazy List", "Images");
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			TableView.Source = new DataSource (this);
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
		}

		public override void DidReceiveMemoryWarning ()
		{
			// Release all cached images. This will cause them to be redownloaded
			// later as they're displayed.
			foreach (var v in Apps)
				v.Image = null;
		}

		class DataSource : UITableViewSource {

			RootViewController Controller { get; set; }
			Task DownloadTask { get; set; }
			UIImage PlaceholderImage { get; set; }

			public DataSource (RootViewController controller)
			{
				Controller = controller;

				// Listen for changes to the Apps collection so the TableView can be updated
				Controller.Apps.CollectionChanged += HandleAppsCollectionChanged;
				// Initialise DownloadTask with an empty and complete task
				DownloadTask = Task.Factory.StartNew (() => { });
				// Load the Placeholder image so it's ready to be used immediately
				PlaceholderImage = UIImage.FromFile ("Images/Placeholder.png");

				// If either a download fails or the image we download is corrupt, ignore the problem.
				TaskScheduler.UnobservedTaskException += delegate(object sender, UnobservedTaskExceptionEventArgs e) {
					e.SetObserved ();
				};
			}

			void HandleAppsCollectionChanged (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
			{
				// Whenever the Items change, reload the data.
				Controller.TableView.ReloadData ();
			}

			public override nint NumberOfSections (UITableView tableView)
			{
				return 1;
			}

			public override nint RowsInSection (UITableView tableview, nint section)
			{
				return Controller.Apps.Count;
			}

			// Customize the appearance of table view cells.
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				UITableViewCell cell;
				// If the list is empty, put in a 'loading' entry
				if (Controller.Apps.Count == 0 && indexPath.Row == 0) {
					cell = tableView.DequeueReusableCell ("Placeholder");
					if (cell == null) {
						cell = new UITableViewCell (UITableViewCellStyle.Subtitle, "Placeholder");
						cell.DetailTextLabel.TextAlignment = UITextAlignment.Center;
						cell.SelectionStyle = UITableViewCellSelectionStyle.None;
						cell.DetailTextLabel.Text = "Loading";
					}
					return cell;
				}

				cell = tableView.DequeueReusableCell ("Cell");
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Subtitle, "Cell");
					cell.SelectionStyle = UITableViewCellSelectionStyle.None;
				}

				// Set the tag of each cell to the index of the App that
				// it's displaying. This allows us to directly match a cell
				// with an item when we're updating the Image
				var app = Controller.Apps [indexPath.Row];
				cell.Tag = indexPath.Row;
				cell.TextLabel.Text = app.Name;
				cell.DetailTextLabel.Text = app.Artist;

				// If the Image for this App has not been downloaded,
				// use the Placeholder image while we try to download
				// the real image from the web.
				if (app.Image == null) {
					app.Image = PlaceholderImage;
					BeginDownloadingImage (app, indexPath);
				}
				cell.ImageView.Image = app.Image;
				return cell;
			}

			void BeginDownloadingImage (App app, NSIndexPath path)
			{
				// Queue the image to be downloaded. This task will execute
				// as soon as the existing ones have finished.
				byte[] data = null;
				DownloadTask = DownloadTask.ContinueWith (prevTask => {
					try {
						UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
						using (var c = new GzipWebClient ())
							data = c.DownloadData (app.ImageUrl);
					} finally {
						UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
					}
				});

				// When the download task is finished, queue another task to update the UI.
				// Note that this task will run only if the download is successful and it
				// uses the CurrentSyncronisationContext, which on MonoTouch causes the task
				// to be run on the main UI thread. This allows us to safely access the UI.
				DownloadTask = DownloadTask.ContinueWith (t => {
					// Load the image from the byte array.
					app.Image = UIImage.LoadFromData (NSData.FromArray (data));

					// Retrieve the cell which corresponds to the current App. If the cell is null, it means the user
					// has already scrolled that app off-screen.
					var cell = Controller.TableView.VisibleCells.Where (c => c.Tag == Controller.Apps.IndexOf (app)).FirstOrDefault ();
					if (cell != null)
						cell.ImageView.Image = app.Image;
				}, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext ());
			}
		}
	}
}
