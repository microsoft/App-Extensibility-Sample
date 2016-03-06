using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtension;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using System.ComponentModel;

namespace ExtensibilitySample
{
    class ExtensionManager
    {
        private ObservableCollection<Extension> _extensions;
        private string _contract;
        private CoreDispatcher _dispatcher;
        private AppExtensionCatalog _catalog;

        public ExtensionManager(string contract)
        {
            // extensions list   
            _extensions = new ObservableCollection<Extension>();

            // catalog & contract
            _contract = contract;
            _catalog = AppExtensionCatalog.Open(_contract);

            // using a method that uses the UI Dispatcher before initializing will throw an exception
            _dispatcher = null;
        }

        public ObservableCollection<Extension> Extensions
        {
            get { return _extensions; }
        }

        public string Contract
        {
            get { return _contract; }
        }

        // this sets up UI dispatcher and does initial extension scan
        public void Initialize()
        {
            // check that we haven't already been initialized
            if (_dispatcher != null)
            {
                throw new ExtensionManagerException("Extension Manager for " + this.Contract + " is already initialized.");
            }

            // thread that initializes the extension manager has the dispatcher
            _dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            // set up extension management events
            _catalog.PackageInstalled += Catalog_PackageInstalled;
            _catalog.PackageUninstalling += Catalog_PackageUninstalling;
            _catalog.PackageUpdating += Catalog_PackageUpdating;
            _catalog.PackageUpdated += Catalog_PackageUpdated;
            _catalog.PackageStatusChanged += Catalog_PackageStatusChanged;

            // Scan all extensions
            FindAllExtensions();
        }

        public async void FindAllExtensions()
        {
            // make sure we have initialized
            if (_dispatcher == null)
            {
                throw new ExtensionManagerException("Extension Manager for " + this.Contract + " is not initialized.");
            }

            // load all the extensions currently installed
            IReadOnlyList<AppExtension> extensions = await _catalog.FindAllAsync();
            foreach (AppExtension ext in extensions)
            {
                // load this extension
                await LoadExtension(ext);
            }
        }

        private async void Catalog_PackageInstalled(AppExtensionCatalog sender, AppExtensionPackageInstalledEventArgs args)
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                foreach (AppExtension ext in args.Extensions)
                {
                    await LoadExtension(ext);
                }
            });
        }

        // package has been updated, so reload the extensions
        private async void Catalog_PackageUpdated(AppExtensionCatalog sender, AppExtensionPackageUpdatedEventArgs args)
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                foreach (AppExtension ext in args.Extensions)
                {
                    await LoadExtension(ext);
                }
            });
        }

        // package is updating, so just unload the extensions
        private async void Catalog_PackageUpdating(AppExtensionCatalog sender, AppExtensionPackageUpdatingEventArgs args)
        {
            await UnloadExtensions(args.Package);
        }

        // package is removed, so unload all the extensions in the package and remove it
        private async void Catalog_PackageUninstalling(AppExtensionCatalog sender, AppExtensionPackageUninstallingEventArgs args)
        {
            await RemoveExtensions(args.Package);
        }

        // package status has changed, could be invalid, licensing issue, app was on USB and removed, etc
        private async void Catalog_PackageStatusChanged(AppExtensionCatalog sender, AppExtensionPackageStatusChangedEventArgs args)
        {
            // get package status
            if (!(args.Package.Status.VerifyIsOK()))
            {
                // if it's offline unload only
                if (args.Package.Status.PackageOffline)
                {
                    await UnloadExtensions(args.Package);
                }

                // package is being serviced or deployed
                else if (args.Package.Status.Servicing || args.Package.Status.DeploymentInProgress)
                {
                    // ignore these package status events
                }

                // package is tampered or invalid or some other issue, remove the extensions
                else
                {
                    await RemoveExtensions(args.Package);
                }

            }
            // if package is now OK, attempt to load the extensions
            else
            {
                // try to load any extensions associated with this package
                await LoadExtensions(args.Package);
            }
        }

        // loads an extension
        public async Task LoadExtension(AppExtension ext)
        {
            // get unique identifier for this extension
            string identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;

            // load the extension if the package is OK
            if (!(ext.Package.Status.VerifyIsOK()
                    // This is where we'd normally do signature verfication, but for demo purposes it isn't important
                    // Below is an example of how you'd ensure that you only load store-signed extensions :)
                    //&& extension.Package.SignatureKind == PackageSignatureKind.Store
                    ))
            {
                // if this package doesn't meet our requirements
                // ignore it and return
                return;
            }

            // if its already existing then this is an update
            Extension existingExt = _extensions.Where(e => e.UniqueId == identifier).FirstOrDefault();

            // new extension
            if (existingExt == null)
            {
                // get extension properties
                ValueSet properties = await ext.GetExtensionPropertiesAsync() as ValueSet;

                // get logo 
                var filestream = await (ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1))).OpenReadAsync();
                BitmapImage logo = new BitmapImage();
                logo.SetSource(filestream);

                // create new extension
                Extension nExt = new Extension(ext, properties, logo);

                // Add it to extension list
                _extensions.Add(nExt);

                // load it
                await nExt.Load();
            }
            // update
            else
            {
                // unload the extension
                existingExt.Unload();

                // update the extension
                await existingExt.Update(ext);
            }
        }

        // loads all extensions associated with a package - used for when package status comes back
        public async Task LoadExtensions(Package package)
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                _extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(async e => { await e.Load(); });
            });
        }

        // unloads all extensions associated with a package - used for updating and when package status goes away
        public async Task UnloadExtensions(Package package)
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                _extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(e => { e.Unload(); });
            });
        }

        // removes all extensions associated with a package - used when removing a package or it becomes invalid
        public async Task RemoveExtensions(Package package)
        {
            await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                _extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(e => { e.Unload(); _extensions.Remove(e); });
            });
        }


        public async void RemoveExtension(Extension ext)
        {
            await _catalog.RequestRemovePackageAsync(ext.AppExtension.Package.Id.FullName);
        }

        // For exceptions using the Extension Manager
        public class ExtensionManagerException : Exception
        {
            public ExtensionManagerException() { }

            public ExtensionManagerException(string message) : base(message) { }

            public ExtensionManagerException(string message, Exception inner) : base(message, inner) { }
        }
    }

    public class Extension : INotifyPropertyChanged
    {
        private AppExtension _extension;
        private ValueSet _properties;
        private bool _enabled;
        private bool _loaded;
        private bool _offline;
        private string _uniqueId;
        private WebView _extwebview;
        private BitmapImage _logo;
        private Visibility _visibility;

        private readonly object _sync = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        public Extension(AppExtension ext, ValueSet properties, BitmapImage logo)
        {
            _extension = ext;
            _properties = properties;
            _enabled = false;
            _loaded = false;
            _offline = false;
            _logo = logo;
            _visibility = Visibility.Collapsed;
            _extwebview = new WebView();    // this is the core functionality that will be executed

            //AUMID + Extension ID is the unique identifier for an extension
            _uniqueId = ext.AppInfo.AppUserModelId + "!" + ext.Id;

            // webview event when the extension notifies us that something happened
            _extwebview.ScriptNotify += ExtensionCallback;
        }

        public BitmapImage Logo
        {
            get { return _logo; }
        }

        public string UniqueId
        {
            get { return _uniqueId; }
        }

        public bool Enabled
        {
            get { return _enabled; }
        }

        public WebView ExtensionWebView
        {
            get { return _extwebview; }
        }

        public bool Offline
        {
            get { return _offline; }
        }

        public AppExtension AppExtension
        {
            get { return _extension; }
        }

        public Visibility Visible
        {
            get { return _visibility; }
        }

        // these are the calls to specific functions inside the app

        // this calls the 'extensionLoad' function in the script file, if it exists.
        public async void InvokeLoad(string str)
        {
            // dont' try to invoke anything if it isn't loaded.
            if (this._loaded)
            {
                // the script function may not exist
                try
                {
                    await _extwebview.InvokeScriptAsync("extensionLoad", new string[] { str });
                }
                catch (Exception e)
                {
                    //MessageDialog md = new MessageDialog("Invoking extension load failed!");
                    //await md.ShowAsync();
                }
            }
        }

        // calls the 'extensionUpdate' function
        // this is called whenever the data in the host app is updated so the extension
        // can keep track of the changes
        public async void InvokeUpdate(string str)
        {
            if (this._loaded)
            {
                // the script function may not exist
                try
                {
                    await _extwebview.InvokeScriptAsync("extensionUpdate", new string[] { str });
                }
                catch (Exception e)
                {
                    //MessageDialog md = new MessageDialog("Invoking extension update failed!");
                    //await md.ShowAsync();
                }
            }
        }

        // called when the javascript in the extension signals a notify
        // we use this to receive image data from the callback
        // a fancier example would have multiple arguments indicating the type of data being returned
        private async void ExtensionCallback(object sender, NotifyEventArgs e)
        {
            if (this._loaded)
            {
                try
                {
                    string encodedImage = ImageTools.StripDataURIHeader(e.Value);
                    await AppData.currentImage.SetSourceAsync(ImageTools.DecodeStringToBitmapSource(encodedImage));
                    AppData.currentImageString = encodedImage;
                }
                catch (Exception ex)
                {
                    //MessageDialog error = new MessageDialog("Error converting image from Extension.");
                    //await error.ShowAsync();
                    return;
                }
            }
        }


        public async Task Update(AppExtension ext)
        {
            // ensure this is the same uid
            string identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;
            if (identifier != this.UniqueId)
            {
                return;
            }

            // get extension properties
            ValueSet properties = await ext.GetExtensionPropertiesAsync() as ValueSet;

            // get logo 
            var filestream = await (ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1))).OpenReadAsync();
            BitmapImage logo = new BitmapImage();
            logo.SetSource(filestream);

            // update the extension
            this._extension = ext;
            this._properties = properties;
            this._logo = logo;

            // load it
            await Load();
        }

        // this controls loading of the extension
        public async Task Load()
        {
            // if it's not enabled or already loaded, don't load it
            if (!_enabled || _loaded)
            {
                return;
            }

            // make sure package is OK to load
            if (!_extension.Package.Status.VerifyIsOK())
            {
                return;
            }

            // Extension is not loaded and enabled - load it
            StorageFolder folder = await _extension.GetPublicFolderAsync();
            if (folder != null)
            {
                // load the webview html
                // this is the html that does not have UI that runs
                string fileName = @"extension.html";
                StorageFile extensionfile = null;
                try
                {
                    extensionfile = await folder.GetFileAsync(fileName);
                }
                catch (Exception e)
                {
                    // something bad happened
                    return;
                }
                if (extensionfile != null)
                {
                    // read entire file as string
                    string extwebview = await FileIO.ReadTextAsync(extensionfile);

                    // load webview and navigate to it
                    // this will run the app
                    _extwebview.NavigateToString(extwebview);

                    // all went well, set state
                    _loaded = true;
                    _visibility = Visibility.Visible;
                    RaisePropertyChanged("Visible");
                    _offline = false;
                }
            }
        }

        // This enables the extension
        public async Task Enable()
        {
            // indicate desired state is enabled
            _enabled = true;

            // load the extension
            await Load();
        }

        // this is different from Disable, example: during updates where we only want to unload -> reload
        // Enable is user intention. Load respects enable, but unload doesn't care
        public void Unload()
        {
            // unload it
            lock (_sync)
            {
                if (_loaded)
                {

                    // unload the webview
                    _extwebview.NavigateToString("");

                    // see if package is offline
                    if (!_extension.Package.Status.VerifyIsOK() && !_extension.Package.Status.PackageOffline)
                    {
                        _offline = true;
                    }

                    _loaded = false;
                    _visibility = Visibility.Collapsed;
                    RaisePropertyChanged("Visible");
                }
            }
        }

        // user-facing action to disable the extension
        public void Disable()
        {
            // only disable if it is enabled
            if (_enabled)
            {
                // set desired state to disabled
                _enabled = false;
                // unload the app
                Unload();
            }
        }

        // typical property changed handler
        private void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
