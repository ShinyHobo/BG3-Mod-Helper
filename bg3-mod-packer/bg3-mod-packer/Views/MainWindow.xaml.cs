﻿namespace bg3_mod_packer.Views
{
    using bg3_mod_packer.Services;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainWindow();
        }

        private void DivineSelect_Click(object sender, RoutedEventArgs e)
        {
            FileLocationDialog(divineLocation, "divineExe");
            ((ViewModels.MainWindow)DataContext).DivineLocation = divineLocation.Text;
        }

        private void Bg3exeSelect_Click(object sender, RoutedEventArgs e)
        {
            FileLocationDialog(bg3exeLocation, "bg3Exe");
            ((ViewModels.MainWindow)DataContext).Bg3ExeLocation = bg3exeLocation.Text;
        }

        private void ConsoleScroller_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (ConsoleScroller.VerticalOffset == ConsoleScroller.ScrollableHeight && e.ExtentHeightChange != 0)
            {   
                ConsoleScroller.ScrollToEnd();
            }
        }

        // TODO move to viewmodel
        private void FileLocationDialog(System.Windows.Controls.TextBox location, string property)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    location.Text = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    location.Text = null;
                    break;
            }
            Properties.Settings.Default[property] = location.Text;
            Properties.Settings.Default.Save();
        }

        private void Unpack_Click(object sender, RoutedEventArgs e)
        {
            unpack.Visibility = Visibility.Hidden;
            unpack_Cancel.Visibility = Visibility.Visible;
            ((ViewModels.MainWindow)DataContext).UnpackingProcess = new PakUnpackHelper();
            ((ViewModels.MainWindow)DataContext).UnpackingProcess.UnpackAllPakFiles().ContinueWith(delegate {
                Application.Current.Dispatcher.Invoke(() => {
                    if(!((ViewModels.MainWindow)DataContext).UnpackingProcess.Cancelled)
                        ((ViewModels.MainWindow)DataContext).ConsoleOutput += "Unpacking complete!\n";
                    unpack.Visibility = Visibility.Visible;
                    unpack_Cancel.Visibility = Visibility.Hidden;
                });
            });
        }

        private void Unpack_Cancel_Click(object sender, RoutedEventArgs e)
        {
            ((ViewModels.MainWindow)DataContext).UnpackingProcess.CancelUpacking();
            unpack.Visibility = Visibility.Visible;
            unpack_Cancel.Visibility = Visibility.Hidden;
        }

        #region UUID Generation
        private void GuidGenerate_Click(object sender, RoutedEventArgs e)
        {
            guidText.Content = System.Guid.NewGuid();
        }

        private void GuidText_Click(object sender, RoutedEventArgs e)
        {
            if(guidText.Content != null)
            {
                Clipboard.SetText(guidText.Content.ToString());
                ((ViewModels.MainWindow)DataContext).ConsoleOutput += $"v4 UUID [{guidText.Content}] copied to clipboard!\n";
            }
        }

        private void HandleGenerate_Click(object sender, RoutedEventArgs e)
        {
            var guid = System.Guid.NewGuid().ToString();
            var handle = $"h{guid}".Replace('-', 'g');
            handleText.Content = handle;
        }

        private void HandleText_Click(object sender, RoutedEventArgs e)
        {
            if(handleText.Content != null)
            {
                Clipboard.SetText(handleText.Content.ToString());
                ((ViewModels.MainWindow)DataContext).ConsoleOutput += $"TranslatedString handle [{handleText.Content}] copied to clipboard!\n";
            }
        }
        #endregion

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new IndexingWindow();
            searchWindow.Show();
        }
    }
}
