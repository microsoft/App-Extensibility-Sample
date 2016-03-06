using System;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
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
            DataContext = AppData.currentImage;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
