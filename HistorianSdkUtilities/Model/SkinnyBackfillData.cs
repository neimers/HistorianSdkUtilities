using dataPARC.Store.EnterpriseCore.Entities;
using dataPARC.Store.EnterpriseCore.TagDataPoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HistorianSdkUtilities.Model
{
    public class SkinnyBackfillData
    {
        public ObservableCollection<BackfillTag> BackfillTags { get; set; }
        
        public SkinnyBackfillData() 
        {
            BackfillTags = new ObservableCollection<BackfillTag>();
        }
    }

    public enum eQualityInputMode
    {
        OpcDa=0,
        BooleanTrueIsGood=1,
        BooleanTrueIsBad=2,
        BitOneIsGood=3, 
        BitOneIsBad=4,
        AllGood=5
    }

    public class BackfillTag : INotifyPropertyChanged
    {
        private TagConfig? _tag;
        public TagConfig? Tag { 
            get
            {
                return _tag;
            }
            set
            {
                _tag = value;
                OnPropertyChanged();
                OnPropertyChanged("TagId");
                OnPropertyChanged("TagName");
                OnPropertyChanged("TagDescription");
                OnPropertyChanged("IsValidTag");
            }
        }

        public SortedList<DateTime, BackfillDataPoint> BackfillPoints { get; set; }

        public int BadValueCount { get; set; }
        public int BadQualityCount { get; set; }
        public int BadTimestampCount { get; set; }
        public int DuplicateTimestampCount { get; set; }
        public int ValidDataPointCount { get; set; }

        private string? _inputDataTagName;
        public string? InputDataTagName 
        {
            get
            {
                return _inputDataTagName;
            }
            set
            {
                _inputDataTagName = value;
                OnPropertyChanged();
            }
        }

        public string? TagId
        {
            get
            {
                return Tag?.Id.ToString() ?? "BAD";
            }
        }

        public string? TagName
        {
            get
            {
                return Tag?.Name ?? InputDataTagName ?? string.Empty;
            }
        }

        public string? TagDescription
        {
            get
            {
                return Tag?.Description ?? string.Empty;
            }
        }

        public bool IsValidTag
        {
            get
            {
                return Tag != null && Tag.Id >= 0;
            }
        }

        public BackfillTag(TagConfig? tag)
        {
            this.Tag = tag;

            BackfillPoints = new SortedList<DateTime, BackfillDataPoint>();

            BadValueCount = 0;
            BadTimestampCount = 0;
            DuplicateTimestampCount = 0;
            ValidDataPointCount = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class BackfillDataPoint
    {
        public DateTime? LocalTimestamp { get; set; }
        public DateTime? UtcTimestamp { get; set; }
        public Object? Value { get; set; }
        public int? Quality { get; set; }
        public int? RowNumber { get; set; }
        public bool ValidBackfillPoint { get; set; }
        public ITagDataPoint? StoreTagDataPoint { get; set; }

        public BackfillDataPoint()
        {
            ValidBackfillPoint = false;
        }
    }
}
