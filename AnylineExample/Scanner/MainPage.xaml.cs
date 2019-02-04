using Anyline.SDK.Plugins;
using Anyline.SDK.Plugins.Meter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Scanner
{
    public sealed partial class MainPage : Page, IScanResultListener<MeterScanResult>
    {

        private Windows.System.ProtocolForResultsOperation _operation = null;

        // for anyline.uwp.testapp
        public readonly string LICENSE_KEY = "eyAiYW5kcm9pZElkZW50aWZpZXIiOiBbICJhbnlsaW5lLnV3cC50ZXN0YXBwIiBdLCAiZGVidWdSZXBvcnRpbmciOiAib24iLCAiaW9zSWRlbnRpZmllciI6IFsgImFueWxpbmUudXdwLnRlc3RhcHAiIF0sICJsaWNlbnNlS2V5VmVyc2lvbiI6IDIsICJtYWpvclZlcnNpb24iOiAiNCIsICJtYXhEYXlzTm90UmVwb3J0ZWQiOiAwLCAicGluZ1JlcG9ydGluZyI6IHRydWUsICJwbGF0Zm9ybSI6IFsgImlPUyIsICJBbmRyb2lkIiwgIldpbmRvd3MiIF0sICJzY29wZSI6IFsgIkFMTCIgXSwgInNob3dQb3BVcEFmdGVyRXhwaXJ5IjogZmFsc2UsICJzaG93V2F0ZXJtYXJrIjogdHJ1ZSwgInRvbGVyYW5jZURheXMiOiA5MCwgInZhbGlkIjogIjIwMTktMDQtMTAiLCAid2luZG93c0lkZW50aWZpZXIiOiBbICJhbnlsaW5lLnV3cC50ZXN0YXBwIiBdIH0KUStwSG9jRXlDYnQyKzlTQzdxSUFIc1Z2b2tIdVFBRTdpdEFDQ3FycjZJd3V6RDFKL05aWGpvZ1VKZmdZVmE3cwpvWlVKbGplZ3psQUJ4Vk5oNmplaXlQL0k1M3NUTFNleUd0cEdGNkV0SjB3TytLTW91ajVrNVpOeVhNUzJoWGlrCmZuV1VhblkrdTZOTS9OclBEZDhPbm9RT2JvREdUK1hubkZLSkhxQUZtc0pTVlpqcUJaUkpUQ2l1SUUyTjFZSXYKS3dLSS9NQklyV2hNcm1xSnczZVJ4dURRS3hxWFVmSktWZUR5VTBIbndsT25yN2hXMWczT05sbytkcGlMM1JYMQpQYkU3b1ZKcXpkLzl0S0syYzlKdUU2L2hxSGd3cjBvdDYwcUp2TUNpWlR2eis5eFF6T2lLMWVJMWM3bXBPOXpsClhlcjIxcGg1SkdRL0FRMW16ZXpkNWc9PQo=";


        private MeterScanViewPlugin _meterScanViewPlugin;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            try
            {
                var protocolForResultsArgs = args.Parameter as ProtocolForResultsActivatedEventArgs;
                _operation = protocolForResultsArgs.ProtocolForResultsOperation;

                if (protocolForResultsArgs.Data.ContainsKey("jsonConfig"))
                {
                    string dataFromCaller = protocolForResultsArgs.Data["jsonConfig"] as string;

                    JsonObject config = JsonObject.Parse(dataFromCaller);

                    AnylineScanView.Init(config, LICENSE_KEY);
                    _meterScanViewPlugin = AnylineScanView.ScanViewPlugin as MeterScanViewPlugin;

                    _meterScanViewPlugin.AddScanResultListener(this);

                    AnylineScanView.CameraView.CameraOpened += CameraView_CameraOpened;

                    AnylineScanView.CameraView.OpenCameraInBackground();
                }

            }
            catch (Exception e)
            {
                var exJson = new JsonObject
                {
                    { "exception", JsonValue.CreateStringValue(e.Message) }
                };
                SendResult(exJson.ToString());
            }
        }

        private void CameraView_CameraOpened(object sender, Size e)
        {
            if (_meterScanViewPlugin != null && !_meterScanViewPlugin.IsScanning())
            {
                _meterScanViewPlugin.StartScanning();
            }
        }

        private void SendResult(string result)
        {
            ValueSet val = new ValueSet
            {
                ["result"] = result
            };
            _operation.ReportCompleted(val);
        }

        public void OnResult(MeterScanResult result)
        {
            var jsonResult = result.ToJson().ToString();
            SendResult(jsonResult);
        }
    }
}
