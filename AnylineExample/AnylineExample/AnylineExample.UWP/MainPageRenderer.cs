using AnylineExample.UWP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.Foundation;

[assembly: ExportRenderer(typeof(AnylineExample.MainPage), typeof(MainPageRenderer))]

namespace AnylineExample.UWP
{
    class MainPageRenderer : PageRenderer
    {

        Page _page;
        
        public MainPageRenderer() { }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Page> args)
        {
            base.OnElementChanged(args);
            
            if (args.OldElement != null || Element == null)
                return;
            
            _page = new ExamplePage();
            _page.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            _page.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            Children.Add(_page);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _page.Arrange(new Windows.Foundation.Rect(0, 0, finalSize.Width, finalSize.Height));
            _page.Width = finalSize.Width;
            _page.Height = finalSize.Height;
            return finalSize;
        }
    }
}