using System.ComponentModel;
using ContentExtractor;
using Xamarin.Forms;

namespace Noveler
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Noveler.MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            this.BindingContext = this;

            //init for quick demo
            UrlTemplate = "https://ck101.com/forum.php?mod=viewthread&tid={0}&page={1}";
            ThreadId = "4757264";
            KeyWord = "postmessage";
            FromPageIndex = "1";
            ToPageIndex = "4";
            PreviewText = "No Data Loading...";
        }

        #region Binding

        private string _urlTemplate;
        public string UrlTemplate
        {
            get
            { return _urlTemplate; }
            set
            {
                if (value != _urlTemplate)
                {
                    _urlTemplate = value;
                    OnPropertyChanged(nameof(UrlTemplate));
                }
            }
        }

        private string _threadId;
        public string ThreadId
        {
            get
            { return _threadId; }
            set
            {
                if (value != _threadId)
                {
                    _threadId = value;
                    OnPropertyChanged(nameof(ThreadId));
                }
            }
        }

        private string _keyWord;
        public string KeyWord
        {
            get
            { return _keyWord; }
            set
            {
                if (value != _keyWord)
                {
                    _keyWord = value;
                    OnPropertyChanged(nameof(KeyWord));
                }
            }
        }

        private string _fromPageIndex;
        public string FromPageIndex
        {
            get
            { return _fromPageIndex; }
            set
            {
                if (value != _fromPageIndex)
                {
                    _fromPageIndex = value;
                    OnPropertyChanged(nameof(FromPageIndex));
                }
            }
        }

        private string _toPageIndex;
        public string ToPageIndex
        {
            get
            { return _toPageIndex; }
            set
            {
                if (value != _toPageIndex)
                {
                    _toPageIndex = value;
                    OnPropertyChanged(nameof(ToPageIndex));
                }
            }
        }

        private double _progressRate;
        public double ProgressRate
        {
            get
            { return _progressRate; }
            set
            {
                if (value != _progressRate)
                {
                    _progressRate = value;
                    OnPropertyChanged(nameof(ProgressRate));
                }
            }
        }

        private string _previewText;
        public string PreviewText
        {
            get
            { return _previewText; }
            set
            {
                if (value != _previewText)
                {
                    _previewText = value;
                    OnPropertyChanged(nameof(PreviewText));
                }
            }
        }

        #endregion

        private ContentExtractor.FetchJob _currJob;

        private void Handle_Clicked(object sender, System.EventArgs e)
        {
            if (!int.TryParse(FromPageIndex, out int fromIdx))
                return;
            if (!int.TryParse(ToPageIndex, out int toIdx))
                return;
            if (!int.TryParse(ThreadId, out int threadId))
                return;

            _currJob = new FetchJob(fromIdx, toIdx, threadId, KeyWord, UrlTemplate);

            _currJob.OnProcessStatus += (double progressRate, string previewMsg) => 
            {
                this.ProgressRate = progressRate;
                this.PreviewText = previewMsg;
            };

            _currJob.OnProcessCompleted += (bool isAllDone) => 
            {
                // go to display data, must be invoked on main thread
                var tempData = _currJob.GetFilterContent();
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() => 
                {
                    Navigation.PushAsync(new NovelDisplayPage(tempData));
                });
            };

            _currJob.ProcessAsync();
        }
    }
}
