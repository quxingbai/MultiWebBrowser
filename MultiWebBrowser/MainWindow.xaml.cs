using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MultiWebBrowser.Customs;
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

namespace MultiWebBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            VIEWS.EMenuShow += VIEWS_EMenuShow;
            VIEWS.EMenuHide += VIEWS_EMenuHide;
            VIEWS.MarkHide();
            VIEWS.ParentWindow = this;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show("未处理异常\n" + e.Exception.Message);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (e.Key == Key.Up)
                {
                    VIEWS.MenuBack();
                    //VIEWS.TestShowMenu();
                    //VIEWS.ChangeVIEWSource();
                }
                else if (e.Key == Key.Down)
                {
                    VIEWS.MenuNext();
                    //VIEWS.ChangeVIEWSource();
                    //VIEWS.TestShowMenu();
                }
            }
            base.OnPreviewKeyDown(e);
        }
        private void VIEWS_EMenuHide(WebViews obj)
        {
            FocusManager.SetFocusedElement(this, this);
        }

        private void VIEWS_EMenuShow(WebViews obj)
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            VIEWS.OnSave();
            base.OnClosed(e);
        }
    }
}
