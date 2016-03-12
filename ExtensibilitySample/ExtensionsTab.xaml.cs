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

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Extension FoundItem = null;

            if (args.ChosenSuggestion != null && args.ChosenSuggestion is Extension)
            {
                FoundItem = args.ChosenSuggestion as Extension;
            }
            else if (String.IsNullOrEmpty(args.QueryText) == false)
            {
                foreach (var Item in Items)
                {
                    if (Item.AppExtension.DisplayName.Equals(args.QueryText, StringComparison.OrdinalIgnoreCase))
                    {
                        FoundItem = Item;
                        break;
                    }
                }
            }

            ShowItem(FoundItem);
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            ShowItem(args.SelectedItem as Extension);
        }

        async private void ShowItem(Extension model)
        {
            var MyDialog = new ContentDialog();

            if (model == null)
            {
                MyDialog.Title = "No item found";

            }

            MyDialog.PrimaryButtonText = "OK";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await MyDialog.ShowAsync());

        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // TODO: not being called from flyout
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                Suggestions.Clear();
                foreach (var Item in Items)
                {
                    if (Item.AppExtension.DisplayName.IndexOf(sender.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Suggestions.Add(Item);
                    }
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Extension SelectedItem = null;

            if (e.AddedItems.Count >= 1)
            {
                SelectedItem = e.AddedItems[0] as Extension;
                (sender as ListView).SelectedItem = null;
                ShowItem(SelectedItem);
            }

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
    }
}
