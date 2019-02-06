using Anyline.SDK.Models;
using Anyline.SDK.Modules.Energy;
using Anyline.SDK.Plugins;
using Anyline.SDK.Plugins.Barcode;
using Anyline.SDK.Plugins.Meter;
using Anyline.SDK.Util;
using Anyline.SDK.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Scanner
{
    public sealed partial class MainPage : Page, IScanResultListener<MeterScanResult>, IScanResultListener<BarcodeScanResult>, IPhotoCaptureListener
    {

        private Windows.System.ProtocolForResultsOperation _operation = null;

        // for anyline.uwp.testapp
        public readonly string LICENSE_KEY = "eyAiYW5kcm9pZElkZW50aWZpZXIiOiBbICJhbnlsaW5lLnV3cC50ZXN0YXBwIiBdLCAiZGVidWdSZXBvcnRpbmciOiAib24iLCAiaW9zSWRlbnRpZmllciI6IFsgImFueWxpbmUudXdwLnRlc3RhcHAiIF0sICJsaWNlbnNlS2V5VmVyc2lvbiI6IDIsICJtYWpvclZlcnNpb24iOiAiNCIsICJtYXhEYXlzTm90UmVwb3J0ZWQiOiAwLCAicGluZ1JlcG9ydGluZyI6IHRydWUsICJwbGF0Zm9ybSI6IFsgImlPUyIsICJBbmRyb2lkIiwgIldpbmRvd3MiIF0sICJzY29wZSI6IFsgIkFMTCIgXSwgInNob3dQb3BVcEFmdGVyRXhwaXJ5IjogZmFsc2UsICJzaG93V2F0ZXJtYXJrIjogdHJ1ZSwgInRvbGVyYW5jZURheXMiOiA5MCwgInZhbGlkIjogIjIwMTktMDQtMTAiLCAid2luZG93c0lkZW50aWZpZXIiOiBbICJhbnlsaW5lLnV3cC50ZXN0YXBwIiBdIH0KUStwSG9jRXlDYnQyKzlTQzdxSUFIc1Z2b2tIdVFBRTdpdEFDQ3FycjZJd3V6RDFKL05aWGpvZ1VKZmdZVmE3cwpvWlVKbGplZ3psQUJ4Vk5oNmplaXlQL0k1M3NUTFNleUd0cEdGNkV0SjB3TytLTW91ajVrNVpOeVhNUzJoWGlrCmZuV1VhblkrdTZOTS9OclBEZDhPbm9RT2JvREdUK1hubkZLSkhxQUZtc0pTVlpqcUJaUkpUQ2l1SUUyTjFZSXYKS3dLSS9NQklyV2hNcm1xSnczZVJ4dURRS3hxWFVmSktWZUR5VTBIbndsT25yN2hXMWczT05sbytkcGlMM1JYMQpQYkU3b1ZKcXpkLzl0S0syYzlKdUU2L2hxSGd3cjBvdDYwcUp2TUNpWlR2eis5eFF6T2lLMWVJMWM3bXBPOXpsClhlcjIxcGg1SkdRL0FRMW16ZXpkNWc9PQo=";


        //private MeterScanViewPlugin _meterScanViewPlugin;
        private IScanViewPlugin _scanViewPlugin;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            try
            {
                var protocolForResultsArgs = args.Parameter as ProtocolForResultsActivatedEventArgs;
                _operation = protocolForResultsArgs.ProtocolForResultsOperation;

                if (protocolForResultsArgs.Data.ContainsKey("jsonConfig"))
                {
                    string dataFromCaller = protocolForResultsArgs.Data["jsonConfig"] as string;

                    JsonObject config = JsonObject.Parse(dataFromCaller);
                    
                    // init the Anyline scan view and store the scanViewPlugin
                    AnylineScanView.Init(config, LICENSE_KEY);
                    _scanViewPlugin = AnylineScanView.ScanViewPlugin;

                    // set this only if it is necessary
                    ResourceManager.MemoryCollectionRate = MemoryCollectionRate.Always;

                    // add the result and photoCapture listeners if meter scanning
                    if (_scanViewPlugin is MeterScanViewPlugin)
                    {
                        (_scanViewPlugin as MeterScanViewPlugin).AddScanResultListener(this);
                        (_scanViewPlugin as MeterScanViewPlugin).PhotoCaptureListener = this;
                        (_scanViewPlugin as MeterScanViewPlugin).PhotoCaptureTarget = PhotoCaptureTarget.File;
                    }

                    // add this as result listener if barcode scanning
                    if (_scanViewPlugin is BarcodeScanViewPlugin)
                        (_scanViewPlugin as BarcodeScanViewPlugin).AddScanResultListener(this);

                    // register to the cameraOpened event
                    AnylineScanView.CameraView.CameraOpened += CameraView_CameraOpened;

                    // finally, open the camera
                    AnylineScanView.CameraView.OpenCameraInBackground();
                }

            }
            catch (Exception e)
            {
                var exJson = new JsonObject
                {
                    { "exception", JsonValue.CreateStringValue(e.Message) }
                };
                await SendResultAsync(exJson);
            }
        }

        private void CameraView_CameraOpened(object sender, Size e)
        {
            if (_scanViewPlugin != null && !_scanViewPlugin.IsScanning())
            {
                _scanViewPlugin.StartScanning();
            }
        }

        private async Task SendResultAsync(JsonObject result)
        {
            ValueSet val = new ValueSet();
            
            try
            {
                if (result.ContainsKey("imagePath"))
                {
                    var imagePath = result["imagePath"].GetString();
                    var imageFile = await StorageFile.GetFileFromPathAsync(imagePath);
                    result.Add("imagePathToken", JsonValue.CreateStringValue(SharedStorageAccessManager.AddFile(imageFile)));
                }

                if (result.ContainsKey("fullImagePath"))
                {
                    var imagePath = result["fullImagePath"].GetString();
                    var imageFile = await StorageFile.GetFileFromPathAsync(imagePath);
                    result.Add("fullImagePathToken", JsonValue.CreateStringValue(SharedStorageAccessManager.AddFile(imageFile)));
                }
                val["result"] = result.ToString();
            }
            catch (Exception e)
            {
                val["exception"] = e.Message;
            }
            finally
            {
                _operation.ReportCompleted(val);
            }
        }
        

        public async void OnResult(MeterScanResult result)
        {
            // the AnylineImages are saved whithin the ToJson() method
            var jsonResult = result.ToJson();
            await SendResultAsync(jsonResult);
        }

        public async void OnResult(BarcodeScanResult result)
        {
            // the AnylineImages are saved whithin the ToJson() method
            var jsonResult = result.ToJson();
            await SendResultAsync(jsonResult);
        }

        public void OnPhotoCaptured(AnylineImage anylineImage)
        {
            throw new NotImplementedException();
        }

        public async void OnPhotoToFile(StorageFile file)
        {
            JsonObject result = new JsonObject();
            result.Add("imagePathToken", JsonValue.CreateStringValue(SharedStorageAccessManager.AddFile(file)));

            await SendResultAsync(result);
            
        }
    }
}
