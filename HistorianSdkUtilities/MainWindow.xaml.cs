using C1.WPF.Grid;
using dataPARC.Authorization.CertificateValidation;
using dataPARC.Store.EnterpriseCore.Entities;
using dataPARC.Store.EnterpriseCore.TagDataPoints;
using dataPARC.Store.SDK;
using dataPARC.TimeSeries.Core.DataPoints;
using HistorianSdkUtilities.Model;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HistorianSdkUtilities
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainViewModel();
            
            this.DataContext = _vm;
        }

        /// <summary>
        /// Set the selected item on the view model, assuming that the selection mode is "row". Feel like there should be a better way to do this...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlexGrid_SelectionChanged(object sender, C1.WPF.Grid.GridCellRangeEventArgs e)
        {
            try
            {
                if (e.CellRange != null && e.CellRange.Row >= 0)
                {
                    _vm.SelectedTagConfig = (TagConfig)fgridTags.Rows[e.CellRange.Row].DataItem;
                }
                else
                {
                    _vm.SelectedTagConfig = null;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }            
        }

        private async void btnFindTags_Click(object sender, RoutedEventArgs e)
        {
            await _vm.FetchTagsAsync();
        }

        private void btnLoadTagWindow_Click(object sender, RoutedEventArgs e)
        {
            _vm.LaunchTagWindow();
        }

        private async void btnConnectAndLoadInterfaces_Click(object sender, RoutedEventArgs e)
        {
            await _vm.TestConnectionToHistorianAndGetInterfacesAsync();
        }

        private void btnImportBackfillFileWindow_Click(object sender, RoutedEventArgs e)
        {
            _vm.LaunchInterfaceFileBackfillWindow();
        }
    }
}