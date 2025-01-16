using HistorianSdkUtilities.Model;
using Microsoft.Win32;
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
using System.Windows.Shapes;

namespace HistorianSdkUtilities.Forms
{
    /// <summary>
    /// Interaction logic for InterfaceFileBackfillWindow.xaml
    /// </summary>
    public partial class InterfaceFileBackfillWindow : Window
    {
        InterfaceFileBackfillViewModel _vm;

        public InterfaceFileBackfillWindow(InterfaceFileBackfillViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;

            this.DataContext = _vm;
        }

        private void btnOpenFileBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();

                // Set filter for file extension and default file extension 
                dlg.DefaultExt = ".xlsx";
                dlg.Filter = "EXCEL (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv";

                // Display OpenFileDialog by calling ShowDialog method 
                Nullable<bool> result = dlg.ShowDialog();

                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;
                    
                    _vm.InputFilePath = filename;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void btnLoadDatafile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.IsDataLoading = true;

                await _vm.LoadSkinnyDataFileAsync();

                MessageBox.Show("Data loaded.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally 
            { 
                _vm.IsDataLoading = false;
            }
        }

        private void fgridTags_SelectionChanged(object sender, C1.WPF.Grid.GridSelectionEventArgs e)
        {

        }

        private async void btnWriteDataToHistorian_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.IsDataLoading = true;

                await _vm.WriteDataToHistorianAsync();

                MessageBox.Show("Import complete.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                _vm.IsDataLoading = false;
            }
        }

        private void btnSaveColumnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.SaveUserCsvColumnAndRowSettings();

                MessageBox.Show("Saved settings to user profile.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                
            }
        }
    }
}
