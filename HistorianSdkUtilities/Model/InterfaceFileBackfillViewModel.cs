using dataPARC.Store.EnterpriseCore.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using C1.Excel;
using System.Windows;
using System.Collections.ObjectModel;
using dataPARC.Authorization.CertificateValidation;
using dataPARC.Store.SDK;
using dataPARC.Store.EnterpriseCore.History.Inputs;
using Microsoft.IdentityModel.Tokens;
using dataPARC.Store.EnterpriseCore.TagDataPoints;
using dataPARC.TimeSeries.Core.DataPoints;
using dataPARC.Store.EnterpriseCore.History.Entities;
using HistorianSdkUtilities.Properties;
using Grpc.Core;
using dataPARC.Store.EnterpriseCore.DataPoints.DataPointSets;
using dataPARC.Store.EnterpriseCore;

namespace HistorianSdkUtilities.Model
{
    public class InterfaceFileBackfillViewModel : INotifyPropertyChanged
    {
        public InterfaceConfig TargetInterfaceConfig { get; set;}

        public string HostName { get; set;}
        public int Port { get; set;}
        public string DisplayedInterfaceName { 
            get 
            {
                return TargetInterfaceConfig.GroupName + "." + TargetInterfaceConfig.Name;
            } 
        }

        public int InterfaceId
        {
            get
            {
                return TargetInterfaceConfig.Id;
            }
        }

        private string? _inputFilePath;
        public string? InputFilePath
        {
            get
            {
                return _inputFilePath;
            }
            set
            {
                _inputFilePath = value;
                OnPropertyChanged();
                OnPropertyChanged("IsLoadDataButtonEnabled");
            }
        }

        private TimeZoneInfo? _inputDataTimeZoneInfo;
        public TimeZoneInfo? InputDataTimeZoneInfo
        {
            get
            {
                return _inputDataTimeZoneInfo;
            }
            set
            {
                _inputDataTimeZoneInfo = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TimeZoneInfo> AvailableTimeZones { get; set; }

        private int? _tagColumnNumber;
        public int TagColumnNumber 
        {
            get
            {
                return _tagColumnNumber ?? 1;
            }
            set
            {
                _tagColumnNumber = value;
                OnPropertyChanged();
            } 
        }

        private int? _timestampColumnNumber;
        public int TimestampColumnNumber
        {
            get
            {
                return _timestampColumnNumber ?? 1;
            }
            set
            {
                _timestampColumnNumber = value;
                OnPropertyChanged();
            }
        }

        private int? _valueColumnNumber;
        public int ValueColumnNumber
        {
            get
            {
                return _valueColumnNumber ?? 2;
            }
            set
            {
                _valueColumnNumber = value;
                OnPropertyChanged();
            }
        }

        private int? _qualityColumnNumber;
        public int QualityColumnNumber
        {
            get
            {
                return _qualityColumnNumber ?? -1;
            }
            set
            {
                _qualityColumnNumber = value;
                OnPropertyChanged();
            }
        }

        private int? _startRowNumber;
        public int StartRowNumber
        {
            get
            {
                return _startRowNumber ?? 2;
            }
            set
            {
                _startRowNumber = value;
                OnPropertyChanged();
            }
        }

        public SkinnyBackfillData BackfillData { get; set; }

        private bool? _isDataLoading;
        public bool IsDataLoading 
        {
            get
            {
                return _isDataLoading ?? false;
            }
            set
            {
                _isDataLoading = value;
                OnPropertyChanged();
                OnPropertyChanged("IsLoadDataButtonEnabled");
                OnPropertyChanged("IsWriteDataButtonEnabled");
            }
        }

        public bool IsLoadDataButtonEnabled
        {
            get
            {
                return !string.IsNullOrWhiteSpace(InputFilePath) && (!IsDataLoading);
            }
        }

        public bool IsWriteDataButtonEnabled
        {
            get
            {
                if (IsDataLoading)
                {
                    return false;
                }
                
                if(BackfillData != null && BackfillData.BackfillTags != null && BackfillData.BackfillTags.Count > 0)
                {
                    return true;
                }

                return false;
            }
        }

        private float? _percentJobComplete;
        public float? PercentJobComplete
        {
            get
            {
                return _percentJobComplete ?? 0;
            }
            set
            {
                _percentJobComplete = value;
                OnPropertyChanged();
            }
        }

        private string? _jobStatus;
        public string? JobStatus
        {
            get
            {
                return _jobStatus;
            }
            set
            {
                _jobStatus = value;
                OnPropertyChanged();
            }
        }

        public async Task WriteDataToHistorianAsync()
        {            
            dataPARC.Store.EnterpriseCore.TagDataPoints.Sets.TagDataPointSet dpSet;
            
            var dlgResult = MessageBox.Show("Import data for all tags?", "Import Data?", MessageBoxButton.OKCancel);

            if (dlgResult != MessageBoxResult.OK)
            {
                return;
            }

            List<ITagDataPoint> tagDataPoints = new List<ITagDataPoint>();

            List<Task> WriteTasks = new List<Task>();

            await using var client = new WriteClient(HostName, Port, CertificateValidation.AcceptAllCertificates);
            
            JobStatus = "Queueing write jobs...";

            foreach(BackfillTag tag in BackfillData.BackfillTags)
            {
                foreach(KeyValuePair<DateTime, BackfillDataPoint> kvp in tag.BackfillPoints)
                {
                    if(kvp.Value.ValidBackfillPoint && kvp.Value.StoreTagDataPoint != null)
                    {
                        tagDataPoints.Add(kvp.Value.StoreTagDataPoint);
                        //WriteTasks.Add(client.WriteDataPointAsync(kvp.Value.StoreTagDataPoint, true));
                    }
                }
            }

            dpSet = new dataPARC.Store.EnterpriseCore.TagDataPoints.Sets.TagDataPointSet(tagDataPoints);

            WriteDataResult result = await client.WriteDataPointSetAsync(dpSet, true);

            MessageBox.Show("Recieved " + result.NumPointsReceived.ToString() + " points, " + result.NumFailedPoints.ToString() + " failed to write.");
        }

        public async Task LoadSkinnyDataFileAsync()
        {
            // these are all zero based
            int tagCol = TagColumnNumber - 1;
            int tstampCol = TimestampColumnNumber - 1;
            int valCol = ValueColumnNumber - 1;
            int qualCol = QualityColumnNumber - 1;
            int startRow = StartRowNumber - 1;

            if(InputDataTimeZoneInfo == null)
            {
                MessageBox.Show("Must select a time zone for input data.");
                return;
            }

            if(tagCol < 0 || tstampCol < 0 || valCol < 0 || startRow < 0)
            {
                MessageBox.Show("Tag, Timestamp, and Value column numbers, along with start row must all be greater than zero.");
                return;
            }

            Dictionary<string, BackfillTag> tagDic = new Dictionary<string, BackfillTag>();

            string? tagName;
            TagConfig? foundTag;
            DateTime tstamp;
            string? dataWriteValue;
            int qualCode;
            Quality quality;

            BackfillData.BackfillTags.Clear();

            if (System.IO.File.Exists(InputFilePath))
            {
                using (C1.Excel.C1XLBook book = new C1XLBook())
                {
                    book.Load(InputFilePath);

                    await using var client = new ConfigurationClient(HostName, Port, CertificateValidation.AcceptAllCertificates);

                    var readParams = new ReadTagListParameters(new InterfaceQueryIdentifier(TargetInterfaceConfig.Id));

                    var tagReadRes = await client.GetTagsAsync(readParams);

                    List<TagConfig> interfaceTags = tagReadRes.Value;
                    
                    C1.Excel.XLSheet sheet = book.Sheets[0];
                    
                    // first find all the tags and try to determine if they are valid
                    for(int x = startRow; x < sheet.Rows.Count; x++)
                    {
                        if(sheet[x, tagCol] != null && sheet[x, tstampCol] != null && sheet[x, valCol] != null && !string.IsNullOrEmpty(sheet[x, tstampCol].Value.ToString()))
                        {
                            tagName = sheet[x, tagCol].Value.ToString()?.ToUpperInvariant();
                            dataWriteValue = sheet[x, valCol].Value.ToString();

                            if (string.IsNullOrWhiteSpace(tagName))
                            {
                                continue;
                            }

                            // look for our tag, if we have it already move on, if we don't, make sure we can find it in the interface tag list
                            if (!tagDic.ContainsKey(tagName))
                            {
                                foundTag = null;
                                foundTag = interfaceTags.Where(t => t.Name.ToUpperInvariant() == tagName).FirstOrDefault();

                                tagDic.Add(tagName, new BackfillTag(foundTag));
                            }

                            BackfillDataPoint backfillDataPoint = new BackfillDataPoint();
                            backfillDataPoint.RowNumber = x;                            

                            // try to parse out data for this record, assuming it actually works for the tag in question
                            if (tagDic[tagName].Tag != null)
                            {                                
                                ITagDataPoint? dp = null;
                                
                                TagConfig? tag = tagDic[tagName].Tag;

                                try
                                {
                                    // first try to parse out the timestamp, if this doesn't work, then we need to just log a bad point, and move on.
                                    tstamp = Convert.ToDateTime(sheet[x, tstampCol].Value.ToString());
                                    
                                    backfillDataPoint.LocalTimestamp = tstamp;
                                    backfillDataPoint.UtcTimestamp = TimeZoneInfo.ConvertTimeToUtc(tstamp, InputDataTimeZoneInfo);

                                    if(qualCol >= 0 && sheet[x, qualCol] != null && !string.IsNullOrWhiteSpace(sheet[x, qualCol].Value.ToString()))
                                    {
                                        qualCode = Convert.ToInt16(sheet[x, qualCol].Value.ToString());

                                        if (qualCode >= 192)
                                        {
                                            quality = Quality.Good;
                                        }
                                        else if(qualCode >= 64)
                                        {
                                            quality = Quality.Uncertain;
                                        }
                                        else
                                        {
                                            quality = Quality.Bad;
                                        }
                                    }
                                    else
                                    {
                                        quality = Quality.Good;
                                    }

                                    if (!tagDic[tagName].BackfillPoints.ContainsKey(tstamp))
                                    {
                                        tagDic[tagName].BackfillPoints.Add(tstamp, backfillDataPoint);
                                    }
                                    else
                                    {
                                        tagDic[tagName].DuplicateTimestampCount++;
                                    }

                                    try
                                    {
                                        switch (tag?.DataType)
                                        {
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Boolean:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointBoolean(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToBoolean(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Byte:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointByte(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToByte(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.DateTime:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointDateTime(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToDateTime(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Decimal:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointDecimal(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToDecimal(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Double:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointDouble(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToDouble(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Int16:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointInt16(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToInt16(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Int32:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointInt32(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToInt32(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Int64:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointInt64(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToInt64(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.SByte:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointSByte(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToSByte(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.Single:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointSingle(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToSingle(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.String:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointString(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToString(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.UInt16:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointUInt16(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToUInt16(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.UInt32:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointUInt32(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToUInt32(dataWriteValue));
                                                break;
                                            case dataPARC.TimeSeries.Core.Enums.ValueType.UInt64:
                                                dp = new dataPARC.Store.EnterpriseCore.TagDataPoints.TagDataPointUInt64(tag.Id, backfillDataPoint.UtcTimestamp.Value, quality, true, Convert.ToUInt64(dataWriteValue));
                                                break;
                                        }

                                        backfillDataPoint.StoreTagDataPoint = dp;
                                        tagDic[tagName].ValidDataPointCount++;
                                        backfillDataPoint.ValidBackfillPoint = true;
                                    }
                                    catch
                                    {
                                        // to do: maybe log errors with row number or something for inspection by user
                                        tagDic[tagName].BadValueCount++;
                                    }

                                }
                                catch
                                {
                                    // to do: maybe log errors with row number or something for inspection by user
                                    tagDic[tagName].BadTimestampCount++;
                                }                                                                
                            }
                            else
                            {
                                tagDic[tagName].BadValueCount++;
                            }
                        }                        
                    }
                }                                                    
            }
            else
            {
                MessageBox.Show("Selected file path not found.");
                return;
            }

            foreach(KeyValuePair<string, BackfillTag> kvp in tagDic)
            {
                kvp.Value.InputDataTagName = kvp.Key;
                BackfillData.BackfillTags.Add(kvp.Value);
            }

            OnPropertyChanged("IsWriteDataButtonEnabled");
        }

        /// <summary>
        /// Load with dummy data, for design time use only.
        /// </summary>
        public InterfaceFileBackfillViewModel()
        {
            TargetInterfaceConfig = new InterfaceConfig();
            TargetInterfaceConfig.Name = "ExampleInterface";
            TargetInterfaceConfig.GroupName = "ExampleGroupName";
            HostName = "HISTORIAN-SERVER";
            Port = 12340;
            InputFilePath = @"C:\Temp\some_file.xlsx";
            BackfillData = new SkinnyBackfillData();

            AvailableTimeZones = new ObservableCollection<TimeZoneInfo>();

            foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
            {
                AvailableTimeZones.Add(tz);

                if(tz.Id == TimeZoneInfo.Local.Id)
                {
                    InputDataTimeZoneInfo = tz;
                }
            }
            
            TagColumnNumber = 1;
            TimestampColumnNumber = 2;
            ValueColumnNumber = 3;
            QualityColumnNumber = 4;
            StartRowNumber = 2;
        }

        public InterfaceFileBackfillViewModel(InterfaceConfig interfaceConfig, string hostName, int port)
        {
            TargetInterfaceConfig = interfaceConfig;
            HostName = hostName;
            Port = port;
            InputFilePath = string.Empty;
            BackfillData = new SkinnyBackfillData();

            AvailableTimeZones = new ObservableCollection<TimeZoneInfo>();

            foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
            {
                AvailableTimeZones.Add(tz);

                if (tz.Id == TimeZoneInfo.Local.Id)
                {
                    InputDataTimeZoneInfo = tz;
                }
            }

            // throw some defaults in here just in case
            TagColumnNumber = 1;
            TimestampColumnNumber = 2;
            ValueColumnNumber = 3;
            QualityColumnNumber = 4;
            StartRowNumber = 2;
            
            // load the above CSV file settings from the user profile
            LoadUserCsvColumnAndRowSettings();
        }

        /// <summary>
        /// Get user profile specific settings around how the CSV file's columns are ordered.
        /// </summary>
        private void LoadUserCsvColumnAndRowSettings()
        {
            TagColumnNumber = Settings.Default.FlatCsvTagColumnNumber;
            TimestampColumnNumber = Settings.Default.FlatCsvTimestampColumnNumber;
            ValueColumnNumber = Settings.Default.FlatCsvValueColumnNumber;
            QualityColumnNumber = Settings.Default.FlatCsvQualityColumnNumber;
            StartRowNumber = Settings.Default.FlatCsvStartRowNumber;
        }
        public void SaveUserCsvColumnAndRowSettings()
        {
            try
            {
                Settings.Default.FlatCsvTagColumnNumber = TagColumnNumber;
                Settings.Default.FlatCsvTimestampColumnNumber = TimestampColumnNumber;
                Settings.Default.FlatCsvValueColumnNumber = ValueColumnNumber;
                Settings.Default.FlatCsvQualityColumnNumber = QualityColumnNumber;
                Settings.Default.FlatCsvStartRowNumber = StartRowNumber;

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to update default settings, error: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
