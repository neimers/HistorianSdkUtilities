using C1.WPF.Grid;
using C1.WPF.Menu;
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

        private void btnSetWriteDataPointTimeToNow_Click(object sender, RoutedEventArgs e)
        {
            _vm.DataWriteTimestamp = DateTime.Now;
        }    

        private void fgridTagValues_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                
                GridHitTestInfo hitTest = fgridTagValues.HitTest(e);

                int row = hitTest.Row;
                int col = hitTest.Column;

                if(hitTest.CellType == GridCellType.Cell)
                {
                    _vm.selectedTagDataPoints ??= [];

                    _vm.selectedTagDataPoints.Clear();

                    _vm.selectedTagDataPoints.Add((BindableTagDataPoint)fgridTagValues.Rows[row].DataItem);

                    _vm.NotifySelectedTagDataPointsUpdated();

                    fgridTagValues.Select(row, 0);

                    var contextMenu = new C1ContextMenu();
                    C1MenuItem c1MenuItem = new C1MenuItem();

                    c1MenuItem.Header = "Set Data Point as Write Target";

                    c1MenuItem.Click += (s, e) =>
                    {
                        if (_vm.SelectedTagDataPoints != null && _vm.SelectedTagDataPoints.Count > 0)
                        {
                            _vm.DataWriteTimestamp = _vm.SelectedTagDataPoints[0].TimestampLocal;
                            _vm.DataWriteValue = _vm.SelectedTagDataPoints[0]?.DataPoint?.Value;
                            _vm.DataWriteQuality = _vm.SelectedTagDataPoints[0]?.DataPoint?.Quality;
                        }
                        else
                        {
                            MessageBox.Show("nothing selected");
                        }
                    };

                    contextMenu.Items.Add(c1MenuItem);

                    contextMenu.Show(e.OriginalSource as FrameworkElement);
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }            
        }
    }
}
