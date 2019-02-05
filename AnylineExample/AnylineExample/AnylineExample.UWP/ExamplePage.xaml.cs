using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace AnylineExample.UWP
{
    public sealed partial class ExamplePage : Page
    {
        public ExamplePage()
        {
            this.InitializeComponent();
            
            BtnScan.Click += BtnScan_Click;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var item in ResultListView.Items)
                (item as FrameworkElement).Width = Window.Current.Bounds.Width;

            return base.ArrangeOverride(finalSize);
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            JsonObject inputJson = null;
            JsonObject result = null;

            // send in the corresponding JSON config
            if ((bool)RBMeterScan.IsChecked)
                inputJson = await GetJsonObjectFromConfigFileName("Config/meterConfig.json");
            else if ((bool)RBSerialNumberScan.IsChecked)
                inputJson = await GetJsonObjectFromConfigFileName("Config/serialNumberConfig.json");
            else if ((bool)RBBarcode.IsChecked)
                inputJson = await GetJsonObjectFromConfigFileName("Config/barcodeConfig.json");
            else if ((bool)RBPhoto.IsChecked)
                inputJson = await GetJsonObjectFromConfigFileName("Config/photoConfig.json");
            else if ((bool)RBPhotoFar.IsChecked)
                inputJson = await GetJsonObjectFromConfigFileName("Config/photoConfigFar.json");
            else if ((bool)RBPhoto1024.IsChecked)
                inputJson = await GetJsonObjectFromConfigFileName("Config/photoConfig1024.json");
            else
                throw new InvalidOperationException("Usecase not supported.");
            
            result = await LaunchAppForResults(inputJson);
            

            // retrieve the result as JSON in string format & fill ListView with result
            if(result != null)
            {
                var item = new ListViewItem
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var grid = new Grid
                {
                    Margin = new Thickness(5)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());

                if (result.ContainsKey("result"))
                {
                    var resultTextBlock = new TextBlock
                    {
                        Text = result.GetNamedString("result"),
                        Margin = new Thickness(10)
                    };
                    grid.Children.Add(resultTextBlock);
                    Grid.SetColumn(resultTextBlock, 0);
                }
                if (result.ContainsKey("imagePathToken"))
                {

                    BitmapImage bitmapImage = new BitmapImage();

                    // sharing file across 2 apps
                    var tokenFile = await SharedStorageAccessManager.RedeemTokenForFileAsync(result["imagePathToken"].GetString());
                    using (var stream = (await tokenFile.OpenStreamForReadAsync()).AsRandomAccessStream())
                    {
                        bitmapImage.SetSource(stream);
                    }
                    var resultImage = new Image
                    {
                        Source = bitmapImage,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        MaxHeight = 150
                    };
                    grid.Children.Add(resultImage);
                    Grid.SetColumn(resultImage, 1);
                }

                var infoBlock = new TextBlock
                {
                    Margin = new Thickness(10)
                };
                infoBlock.Text = "";
                if (result.ContainsKey("scanMode"))
                    infoBlock.Text = result.GetNamedString("scanMode");
                if (result.ContainsKey("barcodeFormat"))
                    infoBlock.Text = result.GetNamedString("barcodeFormat");

                grid.Children.Add(infoBlock);
                Grid.SetColumn(infoBlock, 2);
                
                var removeButton = new Button
                {
                    Content = "X",
                    Margin = new Thickness(15),
                    VerticalAlignment = VerticalAlignment.Top
                };
                removeButton.Click += (s, args) => {
                    ResultListView.Items.Remove(item);
                };
                grid.Children.Add(removeButton);
                Grid.SetColumn(removeButton, 3);

                item.Content = grid;

                ResultListView.Items.Add(item);
            }
        }

        private async Task<JsonObject> LaunchAppForResults(JsonObject json)
        {
            var appUri = new Uri("app2scanner:"); // The protocol handled by the launched app
            var options = new LauncherOptions
            {
                TargetApplicationPackageFamilyName = "anyline.uwp.testapp_h89hmanpz8x02",
                DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseMore
            };

            var inputData = new ValueSet
            {
                ["jsonConfig"] = json.ToString()
            };

            string scanResult = null;
            LaunchUriResult result = await Windows.System.Launcher.LaunchUriForResultsAsync(appUri, options, inputData);

            if (result.Status == LaunchUriStatus.Success &&
                result.Result != null)
            {
                ValueSet res = result.Result;
                if (result.Result.ContainsKey("result"))
                {
                    scanResult = res["result"] as string;
                }
            }
            if (scanResult != null) { 
                var scanResultString = scanResult.ToString();
                Debug.WriteLine($"Result received: {scanResultString}");
                return JsonObject.Parse(scanResult);
            }

            return null;
        }

        private async Task<JsonObject> GetJsonObjectFromConfigFileName(string filePath)
        {
            try
            {
                Uri dataUri = new Uri("ms-appx:///" + filePath);
                StorageFile fileToRequest = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
                string content = await FileIO.ReadTextAsync(fileToRequest);

                JsonObject jsonObject = JsonObject.Parse(content);
                return jsonObject;
            }
            catch (Exception e)
            {
                throw  new UnauthorizedAccessException($"Unable to load '{filePath}'. Reason: {e.Message}");
            }
        }
    }
}
