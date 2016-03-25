using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ExtensibilitySample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExtensionsTab : Page
    {

        public ObservableCollection<Extension> Items = null;
        public ObservableCollection<Extension> Suggestions { get; private set; }

        public ExtensionsTab()
        {
            this.InitializeComponent();

            this.Suggestions = new ObservableCollection<Extension>();

            Items = AppData.ExtensionManager.Extensions;
            this.DataContext = Items;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
                this.Frame.GoBack();
        }

        private async void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Extension ext = cb.DataContext as Extension;
            if (!ext.Enabled)
            {
                await ext.Enable();
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Extension ext = cb.DataContext as Extension;
            if (ext.Enabled)
            {
                ext.Disable();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // remove the package
            Button btn = sender as Button;
            Extension ext = btn.DataContext as Extension;
            AppData.ExtensionManager.RemoveExtension(ext);
        }
    }
}
