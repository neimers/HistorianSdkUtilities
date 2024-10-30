using C1.WPF.Grid;
using dataPARC.Store.EnterpriseCore.DataPoints;
using HistorianSdkUtilities.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace HistorianSdkUtilities.Forms
{
    /// <summary>
    /// Interaction logic for TagUpdateWindow.xaml
    /// </summary>
    public partial class TagUpdateWindow : Window
    {
        readonly TagUpdateViewModel _vm;

        public TagUpdateWindow(TagUpdateViewModel tagUpdateViewModel)
        {
            _vm = tagUpdateViewModel;

            InitializeComponent();

            this.DataContext = _vm;
        }

        /// <summary>
        /// Try to map selections on grid to our selected data points collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fgridTagValues_SelectionChanged(object sender, C1.WPF.Grid.GridCellRangeEventArgs e)
        {
            GridCellRange rng = fgridTagValues.Selection;

            if(_vm.selectedTagDataPoints == null)
            {                               
                _vm.selectedTagDataPoints = new ObservableCollection<BindableTagDataPoint>();
            }

            _vm.selectedTagDataPoints.Clear();

            if (rng != null) 
            {
                foreach(GridCellRange cell in rng.Cells)
                {
                    if (!_vm.selectedTagDataPoints.Contains((BindableTagDataPoint)fgridTagValues.Rows[cell.Row].DataItem))
                    {
                        _vm.selectedTagDataPoints.Add((BindableTagDataPoint)fgridTagValues.Rows[cell.Row].DataItem);   
                    }                    
                }
            }

            _vm.NotifySelectedTagDataPointsUpdated();
        }

        private async void btnGetValuesForTag_Click(object sender, RoutedEventArgs e)
        {
            await _vm.FetchTagDataAsync();
        }

        private async void btnDeleteSelectedTagValues_Click(object sender, RoutedEventArgs e)
        {
            await _vm.DeleteSelectedTagDataAsync();
            await _vm.FetchTagDataAsync();
        }

        private async void btnWriteDataPointToTag_Click(object sender, RoutedEventArgs e)
        {
            await _vm.WriteTagDataPointAsync();
            await _vm.FetchTagDataAsync();
        }

        private async void btnDeleteTagValuesInTimeRange_Click(object sender, RoutedEventArgs e)
        {
            await _vm.DeleteSelectedTagDataRangeAsync();
            await _vm.FetchTagDataAsync();
        }
    }
}
