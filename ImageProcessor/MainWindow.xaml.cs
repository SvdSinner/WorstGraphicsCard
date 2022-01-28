using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace ImageProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public int ZoomPercentage
        {
            get { return (int)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register("MyProperty", typeof(int), typeof(MainWindow), new PropertyMetadata(100));

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            //HACK:Windows only.  Won't work on Linux
            var ofd = new OpenFileDialog() { Title = "Source Image", ValidateNames = true, FileName = txtSource.Text };
            if (ofd.ShowDialog() == true)
                txtSource.Text = ofd.FileName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //HACK:Windows only.  Won't work on Linux
            var sfd = new SaveFileDialog() { Title = "Output file", ValidateNames = true, FileName = txtDestination.Text, Filter = "*.c|Arduino Code" };
            if (sfd.ShowDialog() == true)
                txtDestination.Text = sfd.FileName; 
         }

        private void txtSource_TextChanged(object sender, RoutedEventArgs e)
        {
            if ( null == imgViewer) return;
            var fi = new FileInfo(txtSource.Text);
            if (fi.Exists)
            {
                var imageUri = new Uri(fi.FullName, UriKind.Relative);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = imageUri;
                bitmap.EndInit();
                var image = new WriteableBitmap( bitmap);
                imgViewer.Source = bitmap;
                Debug.WriteLine($"Height: {imgViewer.ActualHeight}, Width: {imgViewer.ActualWidth}");
                rbOriginal.IsChecked = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtSource_TextChanged(sender, e);
            Debug.Assert(Crapifier.TestRead(), "Test read passed.");
        }

        private async void rbProcessed_Checked(object sender, RoutedEventArgs e)
        {
            imgProcessed.Source = Crapifier.GetProcessedImage(imgViewer);  
        }
    }
}
