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
using Windows.ApplicationModel.AppService;
using Windows.UI.Xaml.Media.Imaging;

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

        private async void Grayscale_Click(object sender, RoutedEventArgs e)
        {
            using (var connection = new AppServiceConnection())
            {
                connection.AppServiceName = "com.microsoft.grayscaleservice";
                connection.PackageFamilyName = "AppExtensibility.Extension.Grayscale_byq669axdz8jy";

                AppServiceConnectionStatus status = await connection.OpenAsync();
                if (status == AppServiceConnectionStatus.Success)
                {
                    #region SendMessage
                    // send request to service
                    var request = new ValueSet();
                    request.Add("Command", "Grayscale");
                    request.Add("Pixels", ImageTools.GetBitmapBytes(AppData.currentImage));
                    request.Add("Height", AppData.currentImage.PixelHeight);
                    request.Add("Width", AppData.currentImage.PixelWidth);

                    // get response
                    AppServiceResponse response = await connection.SendMessageAsync(request);

                    if (response.Status == AppServiceResponseStatus.Success)
                    #endregion
                    #region HandleMessage
                    {
                        #region ErrorHandling
                        // convert imagestring back
                        ValueSet message = response.Message as ValueSet;
                        if (message.ContainsKey("Pixels") &&
                            message.ContainsKey("Height") &&
                            message.ContainsKey("Width"))
                        {
                            #endregion
                            byte[] pixels = message["Pixels"] as byte[];
                            int height = (int)message["Height"];
                            int width = (int)message["Width"];

                            // encode the bytes to a string, and then the image.
                            string encodedImage = await ImageTools.EncodeBytesToPNGString(pixels, (uint)width, (uint)height);
                            await AppData.currentImage.SetSourceAsync(ImageTools.DecodeStringToBitmapSource(encodedImage));
                            AppData.currentImageString = encodedImage;
                        }
                    }
                    #endregion

                }
            }
        }

        private async void Crop_Click(object sender, RoutedEventArgs e) 
        {
            #region File Setup
            // Load a File Picker that shows image file types
            FileOpenPicker open = new FileOpenPicker();
            open.FileTypeFilter.Add(".jpg");
            open.FileTypeFilter.Add(".png");
            open.FileTypeFilter.Add(".jpeg");
            open.ViewMode = PickerViewMode.Thumbnail;
            open.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            open.CommitButtonText = "Open";

            // Wait for user to select a file 
            StorageFile source = await open.PickSingleFileAsync();

            // Verify the source is not null
            if (source != null)
            {
                try
                {
                    // Create a destination file
                    StorageFile dest = await KnownFolders.PicturesLibrary.CreateFileAsync("Cropped.jpg", CreationCollisionOption.ReplaceExisting);

                    #endregion 

                    // Call CropImageAsync and receive Result
                    LaunchUriResult result = await this.CropImageAsync(source, dest, 500, 500);

                    // Load Destination Image into Image Preview
                    var stream = await dest.OpenReadAsync();
                    await AppData.currentImage.SetSourceAsync(stream);

            #region Error Handling
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
            #endregion
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
