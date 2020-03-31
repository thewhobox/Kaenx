using Kaenx.MVVM;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Store : Page
    {
        public ObservableCollection<StoreItem> Products { get; set; } = new ObservableCollection<StoreItem>();

        private StoreItem _storedItem;
        private StoreContext c = StoreContext.GetDefault();

        public Store()
        {
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            this.InitializeComponent();
            this.DataContext = this;
            LoadStoreProducts();
        }

        private async void LoadStoreProducts()
        {
            string[] productKinds = { "Durable" };
            List<String> filterList = new List<string>(productKinds);
            StoreProductQueryResult queryResult = await c.GetAssociatedStoreProductsAsync(filterList);

            foreach (KeyValuePair<string, StoreProduct> item in queryResult.Products)
            {
                StoreProduct product = item.Value;

                //StorePurchaseResult result = await c.RequestPurchaseAsync(product.StoreId);
                Products.Add(new StoreItem(product));
            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string && e.Parameter.ToString() == "main")
            {
                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                currentView.BackRequested += CurrentView_BackRequested;
                ApplicationView.GetForCurrentView().Title = "Store";
            }
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled) return;

            if (SmokeGrid.Visibility == Visibility.Visible)
                DetailClose(null, null);
            else
                App.Navigate(typeof(MainPage));

            e.Handled = true;
        }

        private void DetailOpen(object sender, ItemClickEventArgs e)
        {
            ConnectedAnimation animation = null;

            // Get the collection item corresponding to the clicked item.
            if (ProductsList.ContainerFromItem(e.ClickedItem) is GridViewItem container)
            {
                // Stash the clicked item for use later. We'll need it when we connect back from the detailpage.
                _storedItem = (StoreItem)container.Content;

                // Prepare the connected animation.
                // Notice that the stored item is passed in, as well as the name of the connected element. 
                // The animation will actually start on the Detailed info page.
                animation = ProductsList.PrepareConnectedAnimation("forwardAnimation", _storedItem, "ConnectedElement");
                destinationElement.DataContext = _storedItem;
            }


            SmokeGrid.Visibility = Visibility.Visible;
            animation.TryStart(destinationElement);
        }

        private void Animation_Completed(ConnectedAnimation sender, object args)
        {
            SmokeGrid.Visibility = Visibility.Collapsed;
        }

        private async void DetailClose(object sender, RoutedEventArgs e)
        {
            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backwardsAnimation", destinationElement);

            // Collapse the smoke when the animation completes.
            animation.Completed += Animation_Completed;

            // If the connected item appears outside the viewport, scroll it into view.
            ProductsList.ScrollIntoView(_storedItem, ScrollIntoViewAlignment.Default);
            ProductsList.UpdateLayout();

            // Use the Direct configuration to go back (if the API is available). 
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
            }

            // Play the second connected animation. 
            await ProductsList.TryStartConnectedAnimationAsync(animation, _storedItem, "ConnectedElement");
        }

        private async void BuyAddOn(object sender, RoutedEventArgs e)
        {
            StorePurchaseResult result = await c.RequestPurchaseAsync(_storedItem.Id);
        }
    }
}
