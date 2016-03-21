using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ExtensibilitySample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditorTab : Page
    {
        public EditorTab()
        {
            
            //this.commands.Width = this.ActualWidth;
            this.InitializeComponent();
            //DataContext = AppData.currentImage;
            DataContext = AppData.ExtensionManager.Extensions;

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine(this.ActualWidth);
            base.OnNavigatedTo(e);
        }


        // run an extension button
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            // test the extension
            Button btn = sender as Button;
            Extension ext = btn.DataContext as Extension;

            if (AppData.currentImageString != null)
            {
                ext.InvokeLoad(ImageTools.AddDataURIHeader(AppData.currentImageString));
            }
        }

        // open image button
        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            // open file
            FileOpenPicker open = new FileOpenPicker();
            open.FileTypeFilter.Add(".jpg");
            open.FileTypeFilter.Add(".png");
            open.FileTypeFilter.Add(".jpeg");
            open.ViewMode = PickerViewMode.Thumbnail;
            open.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            open.CommitButtonText = "Open";

            // Open a stream for the selected file 
            StorageFile file = await open.PickSingleFileAsync();

            // load the file as the image
            if (file != null)
            {
                try
                {
                    string imgstr = await ImageTools.FileToString(file);
                    if (imgstr != null)
                    {
                        await AppData.currentImage.SetSourceAsync(ImageTools.DecodeStringToBitmapSource(imgstr));
                        AppData.currentImageString = imgstr;
                    }
                }
                catch
                {
                    MessageDialog md = new MessageDialog("Error loading image file.");
                    await md.ShowAsync();
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Crop_Click(object sender, RoutedEventArgs e)
        {
            // open file
            FileOpenPicker open = new FileOpenPicker();
            open.FileTypeFilter.Add(".jpg");
            open.FileTypeFilter.Add(".png");
            open.FileTypeFilter.Add(".jpeg");
            open.ViewMode = PickerViewMode.Thumbnail;
            open.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            open.CommitButtonText = "Open";

            // Open a stream for the selected file 
            StorageFile source = await open.PickSingleFileAsync();

            // load the file as the image
            if (source != null)
            {
                try
                {
                    StorageFile dest = await KnownFolders.PicturesLibrary.CreateFileAsync("Cropped.jpg", CreationCollisionOption.ReplaceExisting);

                    // Receive launch result from CropImage
                    LaunchUriResult result = await this.CropImageAsync(source, dest, 500, 500);

                    // Verify result and load picture into the source
                    if (result.Status == LaunchUriStatus.Success && result.Result != null)
                    {
                        string imgstr = await ImageTools.FileToString(dest);
                        if (imgstr != null)
                        {
                            await AppData.currentImage.SetSourceAsync(ImageTools.DecodeStringToBitmapSource(imgstr));
                            AppData.currentImageString = imgstr;
                        }
                    }
                    
                }
                catch
                {
                    MessageDialog md = new MessageDialog("Error loading image file.");
                    await md.ShowAsync();
                }
            }
        }

        private async Task<LaunchUriResult> CropImageAsync(IStorageFile input, IStorageFile destination, int width, int height)
        {
            // Get access tokens to pass input and output files between apps
            var inputToken = SharedStorageAccessManager.AddFile(input);
            var destinationToken = SharedStorageAccessManager.AddFile(destination);

            // Specify an app to launch by using LaunchUriForResultsAsync
            var options = new LauncherOptions();
            options.TargetApplicationPackageFamilyName = "Microsoft.Windows.Photos_8wekyb3d8bbwe";

            // Specify protocol launch options
            var parameters = new ValueSet();
            parameters.Add("InputToken", inputToken);
            parameters.Add("DestinationToken", destinationToken);
            parameters.Add("CropWidthPixels", 500);
            parameters.Add("CropHeightPixels", 500);
            parameters.Add("EllipticalCrop", false);

            // Perform LaunchUriForResultsAsync
            return await Launcher.LaunchUriForResultsAsync(new Uri("microsoft.windows.photos.crop:"), options, parameters);
        }

    }
}
