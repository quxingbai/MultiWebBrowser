using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// <summary>
    /// WebViews.xaml 的交互逻辑
    /// </summary>
    public partial class WebViews : UserControl
    {
        public class WebViewItem : INotifyPropertyChanged
        {
            private string _Header { get; set; }
            private string _Url { get; set; }
            public string Header { get => _Header; set { _Header = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Header")); } }
            public string Url { get => _Url; set { _Url = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Url")); } }

            public event PropertyChangedEventHandler? PropertyChanged;
        }
        public ObservableCollection<WebViewItem> Items = new ObservableCollection<WebViewItem>();
        private Task ShowMenuTask = null;
        private int ShowMenuTime = 3;
        private bool Life = true;
        public event Action<WebViews> EMenuHide;
        public event Action<WebViews> EMenuShow;
        public Window ParentWindow { get; set; }
        public WebViewControl WebViewControl { get => VIEW; }


        public double ComputedItemWidth
        {
            get { return (double)GetValue(ComputedItemWidthProperty); }
            set { SetValue(ComputedItemWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ComputedItemWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ComputedItemWidthProperty =
            DependencyProperty.Register("ComputedItemWidth", typeof(double), typeof(WebViews), new PropertyMetadata(250.0));



        public WebViews()
        {
            InitializeComponent();
            Items.CollectionChanged += Items_CollectionChanged;
            LIST.ItemsSource = Items;
            LIST.SelectionChanged += LIST_SelectionChanged;
            VIEW.NewWindowRequested += VIEW_NewWindowRequested;
            VIEW.ControlLoaded += VIEW_ControlLoaded;
            BT_Close.ToolTip = "关闭";
            BT_Max.ToolTip = "最大化/取消";
            BT_Min.ToolTip = "最小化";
            BT_WindowTop.ToolTip = "最顶层/取消";
            var ws = WebViewControlManager.QueryWebViews();
            foreach (var w in ws)
            {
                Items.Add(new WebViewItem()
                {
                    Header = w.key,
                    Url = w.Url
                });
            }
        }

        private void VIEW_ControlLoaded(WebViewControl obj)
        {
            if (Items.Count > 0 && LIST.SelectedIndex == -1)
            {
                LIST.SelectedIndex = 0;
                ChangeVIEWSource();
            }
        }

        private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            VIEW.Visibility = Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            ResetMenuItemSize();
        }

        private void VIEW_NewWindowRequested(WebViewControl arg1, CoreWebView2NewWindowRequestedEventArgs arg2)
        {
            arg1.Source = new Uri(arg2.Uri);
            arg2.Handled = true;
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            //var computedwid = ActualWidth / Items.Count-50;
            //var defaultwid = 250;
            //ComputedItemWidth = Items.Count == 0 ? defaultwid : defaultwid * Items.Count > ActualWidth ? computedwid : defaultwid;
            //base.OnRenderSizeChanged(sizeInfo);
            ResetMenuItemSize();
        }
        private void ResetMenuItemSize()
        {
            var pageWidth = ActualWidth - 300;
            var computedwid = pageWidth / Items.Count;
            var defaultwid = 200;
            ComputedItemWidth = Items.Count == 0 ? 0 : Items.Count * defaultwid > pageWidth ? computedwid : defaultwid;
        }
        private void LIST_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = LIST.SelectedItem as WebViewItem;
            if (item == null) return;
            var cs = WebViewControlManager.QueryWebCookies(VIEW.CookieManager, item.Header, item.Url);
            if (cs != null)
            {
                foreach (var c in cs)
                {
                    VIEW.CookieManager.AddOrUpdateCookie(c);
                }
                Array.Clear(cs);
            }
            ChangeVIEWSource();
        }

        public bool AddItem(string Header)
        {
            if (Header == "" || Header == null) return false;
            var data = WebViewControlManager.AddWebView(Header, "https://www.baidu.com");

            if (data != null)
            {
                var d = new WebViewItem()
                {
                    Header = data.key,
                    Url = data.Url
                };
                Items.Add(d);
                LIST.SelectedItem = d;
                return true;
            }
            return false;
        }
        public void ShowMenu()
        {
            EMenuShow?.Invoke(this);
            BD_Menu.Visibility = Visibility.Visible;
        }
        public void HideMenu()
        {
            EMenuHide?.Invoke(this);
            BD_Menu.Visibility = Visibility.Collapsed;
        }
        public void TestShowMenu()
        {
            ShowMenuTime = 3;
            if (ShowMenuTask != null) return;
            ShowMenu();
            ShowMenuTask = Task.Run(() =>
            {
                while (Life)
                {
                    Thread.Sleep(250);
                    ShowMenuTime -= 1;
                    if (ShowMenuTime == 0)
                    {
                        ShowMenuTask = null;
                        break;
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    HideMenu();
                    if (LIST.SelectedItem != null)
                    {
                        ChangeVIEWSource();
                    }
                });
            });
        }
        public void MenuNext()
        {
            ClearCookies();
            LIST.SelectedIndex = LIST.SelectedIndex == Items.Count - 1 ? Items.Count - 1 : LIST.SelectedIndex + 1;
        }
        public void MenuBack()
        {
            ClearCookies();
            LIST.SelectedIndex = LIST.SelectedIndex <= 0 ? 0 : LIST.SelectedIndex - 1;
        }
        public void MarkShow()
        {
            BD_Mark.Visibility = Visibility.Visible;
        }
        public void MarkHide()
        {
            BD_Mark.Visibility = Visibility.Collapsed;
        }
        public void OnSave()
        {
            if (LIST.SelectedItem == null) return;
            WebViewItem item = LIST.SelectedItem as WebViewItem;
            WebViewControlManager.SaveCookie(VIEW, item.Header, item.Url);
            WebViewControlManager.SaveWebView(item.Header, item.Url);
        }
        private void ClearCookies()
        {
            VIEW.CookieManager.DeleteAllCookies();
        }
        public void ChangeVIEWSource()
        {
            WebViewItem item = LIST.SelectedItem as WebViewItem;
            VIEW.Source = new Uri(item.Url);
        }
        private void LIST_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void VIEW_SourceChanging(WebViewControl arg1, WebViewControl.SourceChangeType arg2)
        {
            if (LIST.SelectedItem == null) return;
            WebViewItem item = LIST.SelectedItem as WebViewItem;
            item.Url = VIEW.Source.ToString();
        }

        private void VIEW_SourceChanged(WebViewControl arg1, WebViewControl.SourceChangeType arg2)
        {
            OnSave();
        }

        private void BD_Menu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = Parent as Window;
            if(e.LeftButton== MouseButtonState.Pressed)
            {
                if (ParentWindow != null)
                {
                    ParentWindow.DragMove();
                }
                else if (p != null)
                {
                    p.DragMove();
                }
            }
        }

        private void BT_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            WebViewItem item=((Button)sender).DataContext as WebViewItem;
            var b = WebViewControlManager.DeleteWebView(item.Header);
            if (!b)
            {
                MessageBox.Show("删除失败");
            }
            else
            {
                Items.Remove(item);
                LIST.SelectedIndex = 0;
            }

        }

        private void BT_Min_Click(object sender, RoutedEventArgs e)
        {
            if(ParentWindow!=null)
            ParentWindow.WindowState = WindowState.Minimized;
        }

        private void BT_Max_Click(object sender, RoutedEventArgs e)
        {
            if(ParentWindow!=null)
            ParentWindow.WindowState = ParentWindow.WindowState== WindowState.Maximized? WindowState.Normal: WindowState.Maximized;
        }

        private void BT_Close_Click(object sender, RoutedEventArgs e)
        {
            if(ParentWindow!=null)
            ParentWindow.Close(); ;
        }

        private void BD_Menu_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton== MouseButtonState.Pressed)
            {
                if (ParentWindow != null&&ParentWindow.WindowState== WindowState.Maximized)
                {
                    var p = Mouse.GetPosition(null);
                    this.ParentWindow.WindowState= WindowState.Normal;
                    this.ParentWindow.Left = p.X-(ParentWindow.ActualWidth/2);
                    this.ParentWindow.Top = p.Y - 10;
                    this.ParentWindow.DragMove();
                }
            }
        }
        public void ShowAddItemInfo()
        {
            BD_AddWebViewInfo.Visibility = Visibility.Visible;
            Keyboard.Focus(TEXT_AddWebView_Header);
        }
        public void HideAddItemInfo()
        {
            BD_AddWebViewInfo.Visibility = Visibility.Collapsed;
            TEXT_AddWebView_Header.Text = "";
        }
        private void BT_Add_Click(object sender, RoutedEventArgs e)
        {
            ShowAddItemInfo();
        }

        private void BT_AddWebView_Ok_Click(object sender, RoutedEventArgs e)
        {
            bool b =AddItem(TEXT_AddWebView_Header.Text);
            if (!b)
            {
                MessageBox.Show("添加失败");
            }
            HideAddItemInfo();
        }

        private void BT_AddWebView_No_Click(object sender, RoutedEventArgs e)
        {
            HideAddItemInfo();
        }

        private void TEXT_AddWebView_Header_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key== Key.Enter)
            {
                BT_AddWebView_Ok_Click(null, null);
            }else if(e.Key== Key.Escape)
            {
                HideAddItemInfo();
            }
        }

        private void BT_WindowTop_Click(object sender, RoutedEventArgs e)
        {
            if (ParentWindow != null)
            {
                ParentWindow.Topmost = !ParentWindow.Topmost;
            }
        }
    }
}
