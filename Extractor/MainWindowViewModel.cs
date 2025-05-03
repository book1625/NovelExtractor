using CommunityToolkit.Mvvm.Input;
using ContentExtractor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Extractor;

public class MainWindowViewModel:INotifyPropertyChanged
{
    /// <summary>
    /// 下載網址的取得模式
    /// </summary>
    public enum UrlMode
    {
        Direct,
        HostBase,
        PageBase
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="display"></param>
    public MainWindowViewModel(Action<string> display)
    {
        targetUrl = string.Empty;
        bookName = string.Empty;
        author = string.Empty;
        _currJob = null;

        DisplayMessage = display;
    }

    #region Public Properties

    /// <summary>
    /// 下載清單
    /// </summary>
    public ObservableCollection<DownloadItem> DownloadList { get; set; } = new ObservableCollection<DownloadItem>();

    /// <summary>
    /// 訊息顯示函式
    /// </summary>
    public Action<string> DisplayMessage { get; set; }

    private string targetUrl;

    /// <summary>
    /// 目標網址
    /// </summary>
    public string TargetUrl
    {
        get => targetUrl;
        set
        {
            if (value == targetUrl) return;
            targetUrl = value;
            OnPropertyChanged();
        }
    }

    private int currentIndex;

    /// <summary>
    /// 當前的工作項
    /// </summary>
    public int CurrentIndex
    {
        get => currentIndex;
        set
        {
            if (currentIndex == value) return;
            currentIndex = value;
            OnPropertyChanged();
        }
    }

    private int allCount;

    /// <summary>
    /// 工作項總量
    /// </summary>
    public int AllCount
    {
        get => allCount;
        set
        {
            if (allCount == value) return;
            allCount = value;
            OnPropertyChanged();
        }
    }

    private int failCount;

    public int FailCount
    {
        get => failCount;
        set
        {
            if (failCount == value) return;
            failCount = value;
            OnPropertyChanged();
        }
    }

    private string bookName;

    /// <summary>
    /// 書名
    /// </summary>
    public string BookName
    {
        get => bookName;
        set
        {
            if (value == bookName) return;
            bookName = value;
            OnPropertyChanged();
        }
    }

    private string author;

    /// <summary>
    /// 作者名
    /// </summary>
    public string Author
    {
        get => author;
        set
        {
            if (author == value) return;
            author = value;
            OnPropertyChanged();
        }
    }

    private string chunkSize = "0";

    /// <summary>
    /// 切檔回數
    /// </summary>
    public string ChunkSize
    {
        get => chunkSize;
        set
        {
            if (chunkSize == value) return;
            chunkSize = value;
            OnPropertyChanged();
        }
    }

    private bool isReservedList;

    /// <summary>
    /// 是否反轉清單
    /// </summary>
    public bool IsReservedList
    {
        get => isReservedList;
        set
        {
            if (isReservedList == value) return;
            isReservedList = value;
            OnPropertyChanged();
        }
    }

    private int pageParseDepth = 5;

    /// <summary>
    /// 解析深度
    /// </summary>
    public int PageParseDepth
    {
        get => pageParseDepth;
        set
        {
            if (pageParseDepth == value) return;
            pageParseDepth = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<UrlMode> urlModes = new() { UrlMode.Direct, UrlMode.HostBase, UrlMode.PageBase };

    /// <summary>
    /// 下載網址的組合模式
    /// </summary>
    public ObservableCollection<UrlMode> UrlModes
    {
        get => urlModes;
        set
        {
            if (urlModes == value) return;
            urlModes = value;
            OnPropertyChanged();
        }
    }

    private UrlMode selectedUrlMode = UrlMode.HostBase;

    /// <summary>
    /// 選定的網址的組合模式
    /// </summary>
    public UrlMode SelectedUrlMode
    {
        get => selectedUrlMode;
        set
        {
            if (selectedUrlMode == value) return;
            selectedUrlMode = value;
            OnPropertyChanged();
        }
    }

    private bool isRandomDelay = false;

    /// <summary>
    /// 下載時是否隨機延遲
    /// </summary>
    public bool IsRandomDelay
    {
        get => isRandomDelay;
        set
        {
            if (isRandomDelay == value) return;
            isRandomDelay = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Public command

    /// <summary>
    /// 解析下載
    /// </summary>
    public RelayCommand ParseCommand => new(() =>
    {
        if (string.IsNullOrEmpty(TargetUrl))
        {
            DisplayMessage("請先填入目標網址");
            return;
        }

        if (!Uri.TryCreate(TargetUrl, UriKind.Absolute, out var url))
        {
            DisplayMessage("目標網址格式不正確");
            return;
        }

        var parse = new PageFetchItem()
        {
            Url = TargetUrl
        };

        parse.ParseDownloadList(pageParseDepth);
        var download = parse.GetDownloadList(isReservedList);

        //這裡目前有三種不同的組下載連結手法
        //最常前的是網站的 host，再加上一段相對路徑
        //另一種是代換最後一個 segment
        //還有一種最直白，就是直接給完整連結

        List<Tuple<string, string>> tarList;

        switch (selectedUrlMode)
        {
            case UrlMode.Direct:
                tarList = download.Select(d => new Tuple<string, string>(d.Item1, d.Item2)).ToList();
                break;
            case UrlMode.HostBase:
                tarList = download.Select(d => new Tuple<string, string>($"{url.Scheme}://{url.Host}{d.Item1}", d.Item2)).ToList();
                break;
            case UrlMode.PageBase:
                tarList = download.Select(d => new Tuple<string, string>(url.AbsoluteUri.Replace(url.Segments[^1], d.Item1), d.Item2)).ToList();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _currJob = new FetchJob(tarList);
        _currJob.OnProcessStatus += CurrJob_OnProcessStatus;
        _currJob.OnProcessCompleted += CurrJob_OnProcessCompleted;

        DownloadList.Clear();
        _currJob.GetAllFetchStatus().Where(state => Uri.IsWellFormedUriString(state.Item5, UriKind.Absolute)).Select(state => new DownloadItem()
        {
            Index = state.Item1,
            IsFetched = state.Item2,
            Title = state.Item3,
            RoundCount = state.Item4,
            Link = new Uri(state.Item5)
        })
            .ToList()
            .ForEach(d => DownloadList.Add(d));

        AllCount = DownloadList.Count;
    }, () => true);

    /// <summary>
    /// 運行下載工作
    /// </summary>
    public RelayCommand RunCommand => new(() =>
    {
        if (_currJob is null)
        {
            DisplayMessage("沒有可運行的下載清單");
        }
        DisplayMessage("運行所有下載工作");
        _currJob?.ProcessAsync(IsRandomDelay);
    }, () => true);

    /// <summary>
    /// 暫停下載工作
    /// </summary>
    public RelayCommand StopCommand => new(() =>
    {
        if (_currJob is null)
        {
            DisplayMessage("沒有正在運行的下載");
        }
        DisplayMessage("暫停所有下載工作");
        _currJob?.CancelProcessAsync();
    }, () => true);

    /// <summary>
    /// 輸出檔案
    /// </summary>
    public RelayCommand SaveCommand => new(() =>
    {
        if (_currJob is null)
        {
            DisplayMessage("沒有可以儲存的內容");
            return;
        }

        if (string.IsNullOrWhiteSpace(BookName) || string.IsNullOrWhiteSpace(Author))
        {
            DisplayMessage("請先確認「書名」與「作者名」非空值");
            return;
        }

        if (!int.TryParse(ChunkSize, out var size))
        {
            DisplayMessage("請先確認切檔回數值是否正常");
            return;
        }

        var resultText = _currJob.SaveToTxtFile(AppDomain.CurrentDomain.BaseDirectory, BookName, Author) ? "成功" : "失敗";
        DisplayMessage($@"TXT 存檔結果 {resultText}");

        resultText = _currJob.SaveToEpubFile(AppDomain.CurrentDomain.BaseDirectory, BookName, Author, size) ? "成功" : "失敗";
        DisplayMessage($@"EPUB 存檔結果 {resultText}");
    }, () => true);

    #endregion

    #region Fetch Job

    //當前的工作物件
    private FetchJob? _currJob;

    //工作進度事件
    private void CurrJob_OnProcessStatus(double progressRate, PageFetchItem item)
    {
        CurrentIndex = item.Index;

        DownloadList.Where(d => d.Index == item.Index).ToList().ForEach(d =>
        {
            CurrentIndex = item.Index;
            d.IsFetched = item.IsFetched;
            d.RoundCount = item.GetRoughContextLen();
        });

        FailCount = DownloadList.Count(d => !d.IsFetched);
    }

    //工作完成事件
    private void CurrJob_OnProcessCompleted(bool isAllDone)
    {
        DisplayMessage(isAllDone ? "執行完成，所有工作都已成功" : "執行完成，但部份工作無法成功");
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

/// <summary>
/// 顯示用下載物件
/// </summary>
public class DownloadItem:INotifyPropertyChanged
{
    /// <summary>
    /// 識別索引碼
    /// </summary>
    public int Index { get; set; }

    private string? _title;

    /// <summary>
    /// 章節名稱
    /// </summary>
    public string? Title
    {
        get => _title;
        set
        {
            if (value == _title) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    private bool _isFetched;

    /// <summary>
    /// 是否有捉到最大本文
    /// </summary>
    public bool IsFetched
    {
        get => _isFetched;
        set
        {
            if (value == _isFetched) return;
            _isFetched = value;
            OnPropertyChanged();
        }
    }

    private int _roundCount;

    /// <summary>
    /// 章節評估字數
    /// </summary>
    public int RoundCount
    {
        get => _roundCount;
        set
        {
            if (value == _roundCount) return;
            _roundCount = value;
            OnPropertyChanged();
        }
    }

    private Uri _link;

    /// <summary>
    /// 下載超連結
    /// </summary>
    public Uri Link
    {
        get => _link;
        set
        {
            if (value == _link) return;
            _link = value;
            OnPropertyChanged();
        }
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}