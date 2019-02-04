using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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

        protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            JsonObject result = null;

            if ((bool)RBMeterScan.IsChecked)
            {
                var json = await GetJsonObjectFromConfigFileName("Config/meterConfig.json");
                result = await LaunchAppForResults(json);
            }
            else if ((bool)RBSerialNumberScan.IsChecked)
            {
                var json = await GetJsonObjectFromConfigFileName("Config/serialNumberConfig.json");
                result = await LaunchAppForResults(json);
            }
            else if ((bool)RBPhotoHighRes.IsChecked)
            {
                var json = await GetJsonObjectFromConfigFileName("Config/photoHighResConfig.json");
                result = await LaunchAppForResults(json);
            }
            else if ((bool)RBPhotoLowRes.IsChecked)
            {
                var json = await GetJsonObjectFromConfigFileName("Config/photoLowResConfig.json");
                result = await LaunchAppForResults(json);
            }

            throw new InvalidOperationException("Usecase not supported.");
        }

        private async Task<JsonObject> LaunchAppForResults(JsonObject json)
        {
            var appUri = new Uri("app2scanner:"); // The protocol handled by the launched app
            var options = new LauncherOptions { TargetApplicationPackageFamilyName = "anyline.uwp.testapp_h89hmanpz8x02" };

            var inputData = new ValueSet
            {
                ["jsonConfig"] = json.ToString()
            };

            string scanResult = "";
            LaunchUriResult result = await Windows.System.Launcher.LaunchUriForResultsAsync(appUri, options, inputData);

            if (result.Status == LaunchUriStatus.Success &&
                result.Result != null &&
                result.Result.ContainsKey("result"))
            {
                ValueSet res = result.Result;
                scanResult = res["result"] as string;
            }
            var scanResultString = scanResult != null ? scanResult.ToString() : "- no result -";
            Debug.WriteLine($"Result received: {scanResultString}");


            return JsonObject.Parse(scanResult);
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
