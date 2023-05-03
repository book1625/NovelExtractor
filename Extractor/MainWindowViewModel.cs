using CommunityToolkit.Mvvm.Input;
using ContentExtractor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Extractor;

public class MainWindowViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="display"></param>
    public MainWindowViewModel(Action<string> display)
    {
        _targetUrl = string.Empty;
        _bookName = string.Empty;
        _author = string.Empty;
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

    private string _targetUrl;

    /// <summary>
    /// 目標網址
    /// </summary>
    public string TargetUrl
    {
        get => _targetUrl;
        set
        {
            if (value == _targetUrl) return;
            _targetUrl = value;
            OnPropertyChanged();
        }
    }

    private int _currentIndex;

    /// <summary>
    /// 當前的工作項
    /// </summary>
    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (_currentIndex == value) return;
            _currentIndex = value;
            OnPropertyChanged();
        }
    }

    private int _allCount;

    /// <summary>
    /// 工作項總量
    /// </summary>
    public int AllCount
    {
        get => _allCount;
        set
        {
            if (_allCount == value) return;
            _allCount = value;
            OnPropertyChanged();
        }
    }

    private string _bookName;

    /// <summary>
    /// 書名
    /// </summary>
    public string BookName
    {
        get => _bookName;
        set
        {
            if (value == _bookName) return;
            _bookName = value;
            OnPropertyChanged();
        }
    }

    private string _author;

    /// <summary>
    /// 作者名
    /// </summary>
    public string Author
    {
        get => _author;
        set
        {
            if (_author == value) return;
            _author = value;
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

        parse.ParseDownloadList();
        var download = parse.GetDownloadList();

        var tarList = download.Select(d => new Tuple<string, string>($"{url.Scheme}://{url.Host}{d.Item1}", d.Item2))
#if DEBUG
            .Take(5) //這是為了好測試
#endif
            .ToList();

        _currJob = new FetchJob(tarList);
        _currJob.OnProcessStatus += CurrJob_OnProcessStatus;
        _currJob.OnProcessCompleted += CurrJob_OnProcessCompleted;

        DownloadList.Clear();
        _currJob.GetAllFetchStatus().Select(state => new DownloadItem()
            {
                Index = state.Item1,
                IsFetched = state.Item2,
                Title = state.Item3,
                RoundCount = state.Item4
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
        _currJob?.ProcessAsync();
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

        var resultText = _currJob.SaveToTxtFile(AppDomain.CurrentDomain.BaseDirectory, BookName, Author) ? "成功" : "失敗";
        DisplayMessage($@"TXT 存檔結果 {resultText}");

        resultText = _currJob.SaveToEpubFile(AppDomain.CurrentDomain.BaseDirectory, BookName, Author) ? "成功" : "失敗";
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
public class DownloadItem : INotifyPropertyChanged
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