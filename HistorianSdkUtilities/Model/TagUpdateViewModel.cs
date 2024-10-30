using dataPARC.Authorization.CertificateValidation;
using dataPARC.Store.EnterpriseCore.DataPoints;
using dataPARC.Store.EnterpriseCore.DataPoints.TagDataPoints;
using dataPARC.Store.EnterpriseCore.DataPoints.Unions;
using dataPARC.Store.EnterpriseCore.Entities;
using dataPARC.Store.EnterpriseCore.History.Entities;
using dataPARC.Store.EnterpriseCore.History.Enums;
using dataPARC.Store.EnterpriseCore.History.Inputs;
using dataPARC.Store.EnterpriseCore.TagDataPoints;
using dataPARC.Store.SDK;
using dataPARC.TimeSeries.Core.DataPoints;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HistorianSdkUtilities.Model
{
    public class TagUpdateViewModel : INotifyPropertyChanged
    {

        private TagConfig _tagConfig;
        public TagConfig Tag
        {
            get { return _tagConfig; }
            set
            {
                _tagConfig = value;
                OnPropertyChanged();
            }
        }

        private string _host;
        public string HostName
        {
            get { return _host; }
            set { _host = value; OnPropertyChanged(); }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set { _port = value; OnPropertyChanged(); }
        }

        private DateTime? _readStartTime { get; set; }
        public DateTime? ReadStartTime
        {
            get
            {
                return _readStartTime;
            }
            set
            {
                _readStartTime = value;
                OnPropertyChanged();
                OnPropertyChanged("ReadStartTimeUTC");
            }
        }

        private DateTime? _readEndTime { get; set; }
        public DateTime? ReadEndTime
        {
            get
            {
                return _readEndTime;
            }
            set
            {
                _readEndTime = value;
                OnPropertyChanged();
                OnPropertyChanged("ReadEndTimeUTC");
            }
        }

        public DateTime? ReadStartTimeUTC
        {
            get
            {
                return _readStartTime?.ToUniversalTime();
            }
            set
            {
                _readStartTime = value;
                OnPropertyChanged();
            }
        }
        
        public DateTime? ReadEndTimeUTC
        {
            get
            {
                return _readEndTime?.ToUniversalTime();
            }
        }

        private DateTime? _dataWriteTimestamp { get; set; }
        public DateTime? DataWriteTimestamp
        {
            get
            {
                return _dataWriteTimestamp;
            }
            set
            {
                _dataWriteTimestamp = value;
                OnPropertyChanged();
                OnPropertyChanged("IsValidDataPointToWrite");
                OnPropertyChanged("IsWriteDataPointButtonEnabled");
                OnPropertyChanged("DataWriteTimestampUTC");
            }
        }

        public DateTime? DataWriteTimestampUTC
        {
            get
            {
                return _dataWriteTimestamp?.ToUniversalTime();
            }
        }

        private object? _dataWriteValue { get; set; }
        public object? DataWriteValue
        {
            get { return _dataWriteValue; }
            set 
            { 
                _dataWriteValue = value; 
                OnPropertyChanged(); 
                OnPropertyChanged("IsValidDataPointToWrite");
                OnPropertyChanged("IsWriteDataPointButtonEnabled");
            }
        }

        private Quality? _dataWriteQuality { get; set; }
        public Quality? DataWriteQuality
        {
            get { return _dataWriteQuality; }
            set 
            { 
                _dataWriteQuality = value; 
                OnPropertyChanged(); 
                OnPropertyChanged("IsValidDataPointToWrite");
                OnPropertyChanged("IsWriteDataPointButtonEnabled");
            }
        }
        
        public ObservableCollection<Quality> QualityOptions
        {
            get
            {
                return new ObservableCollection<Quality>() { Quality.Good, Quality.Bad, Quality.Uncertain };
            }
        }

        public bool IsWriteDataPointButtonEnabled
        {
            get
            {
                return IsValidDataPointToWrite && IsButtonsAreEnabled;
            }
        }

        public bool IsButtonsAreEnabled
        {
            get
            {
                return !IsOperationPending;
            }
        }

        private bool _isOperationPending;
        private bool IsOperationPending
        {
            get { return _isOperationPending; }
            set
            {
                if (_isOperationPending != value)
                {
                    _isOperationPending = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsButtonsAreEnabled");
                    OnPropertyChanged("IsWriteDataPointButtonEnabled");
                }
            }
        }

        private ObservableCollection<BindableTagDataPoint>? _tagDataPoints;
        public ObservableCollection<BindableTagDataPoint>? TagDataPoints
        {
            get
            {                
                return _tagDataPoints;
            }
        }

        public ObservableCollection<BindableTagDataPoint>? selectedTagDataPoints;
        public ObservableCollection<BindableTagDataPoint>? SelectedTagDataPoints
        {
            get
            {
                return selectedTagDataPoints;
            }

        }

        public void NotifySelectedTagDataPointsUpdated()
        {
            OnPropertyChanged("SelectedTagDataPoints");
        }

        /// <summary>
        /// This constructor is mostly just to make it possible to do design time work on the XAML, use other constructor.
        /// </summary>
        public TagUpdateViewModel()
        {            
            _tagConfig = new TagConfig();
            _tagConfig.Name = "Test Tag";
            _tagConfig.InterfaceName = "TEST-INTERFACE";
            _tagConfig.Description = "Some tag description";
            _tagConfig.AllowWrites = true;
            _tagConfig.DataType = dataPARC.TimeSeries.Core.Enums.ValueType.String;

            _host = "localhost";
            _port = 12340;

            ReadStartTime = DateTime.Now.Date;
            ReadEndTime = DateTime.Now;

            selectedTagDataPoints = new ObservableCollection<BindableTagDataPoint>();            
        }

        public TagUpdateViewModel(TagConfig tagConfig, string host, int port)
        {          
            _tagConfig = tagConfig;
            _host = host;
            _port = port;

            ReadStartTime = DateTime.Now.Date;
            ReadEndTime = DateTime.Now;

            selectedTagDataPoints = new ObservableCollection<BindableTagDataPoint>();

            DataWriteQuality = Quality.Good; // default this in for convenience, since most of the time we'd write good data            
        }

        public bool IsValidDataPointToWrite
        {
            get
            {
                return DataWriteValue != null && DataWriteTimestamp.HasValue && DataWriteQuality.HasValue;
            }
        }

        public async Task DeleteSelectedTagDataAsync()
        {
            try
            {
                if(SelectedTagDataPoints == null || SelectedTagDataPoints.Count == 0)
                {
                    MessageBox.Show("Must select data points to delete first.");
                    return;
                }

                IsOperationPending = true;

                var dlgResult = MessageBox.Show("Delete " + SelectedTagDataPoints.Count.ToString() + " data points?", "Delete Data?", MessageBoxButton.OKCancel);

                if (dlgResult == MessageBoxResult.OK)
                {
                    List<DateTime> timestamps = new List<DateTime>();

                    foreach (BindableTagDataPoint dataPoint in SelectedTagDataPoints)
                    {
                        if (dataPoint != null && dataPoint.DataPoint != null)
                        {
                            timestamps.Add(dataPoint.DataPoint.Time);
                        }                        
                    }

                    await using var client = new WriteClient(HostName, Port, CertificateValidation.AcceptAllCertificates);

                    var deleteStatus1 = await client.DeleteDataAsync(Tag.Id, timestamps, awaitCompletion: true);                    
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsOperationPending = false;                
            }
        }

        public async Task DeleteSelectedTagDataRangeAsync()
        {
            try
            {
                if (ReadStartTime == null || ReadEndTime == null || ReadStartTime > ReadEndTime)
                {
                    MessageBox.Show("Invalid time range selected for deletion.");
                    return;
                }

                IsOperationPending = true;

                var dlgResult = MessageBox.Show("Really delete all data between " + ReadStartTime.ToString() + " and " + ReadEndTime.ToString() + " for tag? This may delete data not shown in the grid.", "Delete Data?", MessageBoxButton.OKCancel);

                if (dlgResult == MessageBoxResult.OK)
                {                   
                    await using var client = new WriteClient(HostName, Port, CertificateValidation.AcceptAllCertificates);

                    var deleteStatus2 = await client.DeleteDataAsync(Tag.Id, ReadStartTime.Value.ToUniversalTime(), ReadEndTime.Value.ToUniversalTime());                    
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsOperationPending = false;
            }
        }

        public async Task WriteTagDataPointAsync()
        {
            try
            {
                if(DataWriteQuality == null || DataWriteTimestampUTC == null || DataWriteValue == null)
                {
                    return;
                }

                IsOperationPending = true;
                ITagDataPoint? dp = null;                                
                
                await using var client = new WriteClient(HostName, Port, CertificateValidation.AcceptAllCertificates);

                switch (Tag.DataType)
                {
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Boolean:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointBoolean(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToBoolean(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Byte:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointByte(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToByte(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.DateTime:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointDateTime(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToDateTime(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Decimal:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointDecimal(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToDecimal(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Double:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointDouble(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToDouble(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Int16:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointInt16(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToInt16(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Int32:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointInt32(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToInt32(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Int64:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointInt64(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToInt64(DataWriteValue));
                        break;                    
                    case dataPARC.TimeSeries.Core.Enums.ValueType.SByte:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointSByte(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToSByte(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.Single:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointSingle(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToSingle(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.String:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointString(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToString(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.UInt16:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointUInt16(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToUInt16(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.UInt32:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointUInt32(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToUInt32(DataWriteValue));
                        break;
                    case dataPARC.TimeSeries.Core.Enums.ValueType.UInt64:
                        dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointUInt64(Tag.Id, DataWriteTimestampUTC.Value, DataWriteQuality.Value, true, Convert.ToUInt64(DataWriteValue));
                        break;
                }

                if (dp != null)
                {
                    WriteDataResult writeRes1 = await client.WriteDataPointAsync(dp, false);

                    if (writeRes1.ExceptionMessages != null && writeRes1.ExceptionMessages.Count > 0) 
                    {
                        foreach(string ex in writeRes1.ExceptionMessages)
                        {
                            MessageBox.Show(ex);
                        }                        
                    }
                    
                    MessageBox.Show("Wrote " + (writeRes1.NumPointsReceived - writeRes1.NumFailedPoints).ToString() + " data point(s) successfully.");
                }
                else
                {
                    MessageBox.Show("Unsupported tag data type: " + Tag.DataType.ToString());
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsOperationPending = false;
            }
        }

        public async Task FetchTagDataAsync()
        {
            try
            {
                IsOperationPending = true;

                //_tagDataPoints = null;
                if (_tagDataPoints != null)
                {
                    _tagDataPoints.Clear();
                }
                else
                {
                    _tagDataPoints = new ObservableCollection<BindableTagDataPoint>();
                }

                if(!ReadStartTime.HasValue || !ReadEndTime.HasValue)
                {
                    MessageBox.Show("Must select a start and end time first.");
                    return;
                }

                await using var client = new ReadClient(HostName, Port, CertificateValidation.AcceptAllCertificates);

                var readParams1 = new ReadRawParameters(new TagQueryIdentifier(Tag.Id), ReadStartTime.Value.ToUniversalTime(), ReadEndTime.Value.ToUniversalTime());

                var readRes1 = await client.ReadRawAsync(readParams1);                

                if (readRes1.Status == ReadRawStatus.Successful)
                {
                    foreach(IDataPoint dp in readRes1.DataPoints)
                    {
                        _tagDataPoints.Add(new BindableTagDataPoint(dp));
                    }
                    //_tagDataPoints = new ObservableCollection<IDataPoint>([.. readRes1.DataPoints]);                    
                }
                else
                {
                    // Something went wrong, check against other ReadRawStatus cases for more info.
                    if(readRes1.Status != ReadRawStatus.NoValueFound)
                    {
                        // probably don't need to notify about no data being found, but any other status probably should be flagged
                        MessageBox.Show(readRes1.Status.ToString());
                    }
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsOperationPending = false;
                OnPropertyChanged("TagDataPoints");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class BindableTagDataPoint : INotifyPropertyChanged
    {
        private IDataPoint? _dataPoint;
        public IDataPoint? DataPoint 
        {
            get
            {
                return _dataPoint;
            }
            set
            {
                _dataPoint = value;
                OnPropertyChanged();
                OnPropertyChanged("LocalDateTime");
            }
        }

        public DateTime? TimestampUTC
        {
            get
            {
                return _dataPoint?.Time;
            }
        }

        public DateTime? TimestampLocal
        {
            get
            {
                return _dataPoint?.Time.ToLocalTime();
            }
        }

        public string? TagQuality
        {
            get
            {
                return _dataPoint?.Quality.ToString();
            }
        }

        public string? TagValueString
        {
            get
            {
                return _dataPoint?.Value.ToString();
            }
        }

        public object? TagValue
        {
            get
            {
                return _dataPoint?.Value;
            }
        }        

        public BindableTagDataPoint(IDataPoint dataPoint)
        {
            _dataPoint = dataPoint;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
