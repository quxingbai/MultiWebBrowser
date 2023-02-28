using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiWebBrowser.Customs
{
    public class WebViewControl : HeaderedContentControl
    {


        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(WebViewControl), new PropertyMetadata(new Uri("https://www.baidu.com/")));



        public enum SourceChangeType
        {
            SourceSet,
            BackSource,
            FrontSource
        }
        public WebView2 WebView { get => GetTemplateChild("WebView") as WebView2; }
        private Button BackButton { get => GetTemplateChild("BT_Back") as Button; }
        private Button FrontButton { get => GetTemplateChild("BT_Front") as Button; }
        private Button HeaderButton { get => GetTemplateChild("BT_Header") as Button; }
        
        private TextBox TextUrl { get => GetTemplateChild("TEXT_Url") as TextBox; }
        private Stack<Uri> UriBack = new Stack<Uri>();
        private Stack<Uri> UriFront = new Stack<Uri>();
        private Uri OldSource { get; set; } = null;
        private bool IsNoPropSourceSet = false;
        public CoreWebView2CookieManager CookieManager { get => WebView.CoreWebView2.CookieManager; }

        public event Action<WebViewControl, CoreWebView2NewWindowRequestedEventArgs> NewWindowRequested;
        public event Action<WebViewControl> ControlLoaded;
        public event Action<WebViewControl, SourceChangeType> SourceChanged;
        public event Action<WebViewControl, SourceChangeType> SourceChanging;

        static WebViewControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WebViewControl), new FrameworkPropertyMetadata(typeof(WebViewControl)));
        }

        public WebViewControl()
        {
            //Loaded += WebViewControl_Loaded;
        }
        public async override void OnApplyTemplate()
        {
            ToolTipService.SetInitialShowDelay(this, 2);
            ToolTipService.SetBetweenShowDelay(this, 2);
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: @"./WebView2Data");
            await WebView.EnsureCoreWebView2Async(env);
            WebView.CoreWebView2.NewWindowRequested += (ss, ee) => NewWindowRequested?.Invoke(this, ee);
            WebView.NavigationCompleted += WebView_NavigationCompleted;
            WebView.NavigationStarting += WebView_NavigationStarting; ;
            WebView.SourceChanged += WebView_SourceChanged;
            HeaderButton.Click += (ss, ee) => WebView.Reload();
            BackButton.Click += (ss, ee) => TryBackUrl();
            FrontButton.Click += (ss, ee) => TryFrontUrl();
            TextUrl.KeyDown += (ss, ee) =>
            {
                if (ee.Key == Key.Enter)
                {
                    try
                    {
                        var u = new Uri(TextUrl.Text);
                        Source = u;
                    }
                    catch
                    {
                        try
                        {
                            var u1 = new Uri("https://www." + TextUrl.Text);
                            Source = u1;
                        }
                        catch
                        {
                            TextUrl.Text = Source.ToString();
                        }
                    }
                    WebView.Focus();
                }
                else if (ee.Key == Key.Escape)
                {
                    TextUrl.Text = Source.ToString();
                    WebView.Focus();
                }
            };
            OnPropertyChanged(new DependencyPropertyChangedEventArgs(SourceProperty, null, Source));
            BackButton.ToolTip = "返回";
            FrontButton.ToolTip = "前进";
            HeaderButton.ToolTip = "刷新";
            ControlLoaded?.Invoke(this);
            base.OnApplyTemplate();
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            this.Header = "加载中...";
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            this.Header = WebView.CoreWebView2.DocumentTitle;
        }

        private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (WebView.Source != Source)
            {
                SetSourceNoOnProp(WebView.Source);
                SourceChanging?.Invoke(this, SourceChangeType.SourceSet);
                SourceChanged?.Invoke(this, SourceChangeType.SourceSet);
            }
        }

        public void UpdateCookie(string key, string value)
        {
            WebView.CoreWebView2.CookieManager.AddOrUpdateCookie(WebView.CoreWebView2.CookieManager.CreateCookie(key, value, Source.Host, "/"));
        }

        public bool TryBackUrl()
        {
            try
            {
                if (UriBack.Count == 0) return false;
                OnWebBrowserUrlChanged(UriBack.Pop(), SourceChangeType.BackSource);
                return true;
            }
            catch
            {
                UriFront.TryPop(out Uri u);
                return false;
            }
        }
        public bool TryFrontUrl()
        {
            if (UriFront.Count == 0) return false;
            OnWebBrowserUrlChanged(UriFront.Pop(), SourceChangeType.FrontSource);
            return true;
        }
        private void OnWebBrowserUrlChanged(Uri uri, SourceChangeType type = SourceChangeType.SourceSet)
        {
            if (!IsLoaded)
            {
                return;
            }
            switch (type)
            {
                case SourceChangeType.SourceSet:
                    if (OldSource != null)
                        UriBack.Push(OldSource);
                    break;
                case SourceChangeType.BackSource:
                    UriFront.Push(Source);
                    break;
                case SourceChangeType.FrontSource:
                    if (OldSource != null)
                        UriBack.Push(OldSource);
                    break;
                default:
                    break;
            }
            if (WebView.Source == uri)
            {
                try
                {
                    WebView.Reload();

                }
                catch
                {

                }
            }
            else
            {
                SourceChanging?.Invoke(this, type);
                WebView.Source = uri;
                SourceChanged?.Invoke(this, type);
                OldSource = uri;
            }
        }
        private void SetSourceNoOnProp(Uri s)
        {
            IsNoPropSourceSet = true;
            Source = s;
            IsNoPropSourceSet = false;
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == SourceProperty)
            {
                if (!IsNoPropSourceSet)
                    OnWebBrowserUrlChanged(Source);
            }
            base.OnPropertyChanged(e);
        }
    }
}
