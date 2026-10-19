using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using PaceBuddies.Animation;
using PaceBuddies.Services;

namespace PaceBuddies.Views;

public partial class MainWindow : Window
{
    private readonly RuntimeCoordinator coordinator;
    private readonly DispatcherTimer editTimer=new(){Interval=TimeSpan.FromMilliseconds(350)};
    private readonly MetricTile cpu,memory,gpu;
    private bool loading=true;
    private bool dirty;
    private BuddyRuntime? selected;
    private readonly WindowsStartupRegistration startup;
    private bool startupEnabled;
    public bool AllowClose { get; set; }
    public MainWindow(RuntimeCoordinator coordinator,WindowsStartupRegistration? startup=null)
    {
        this.coordinator=coordinator;
        this.startup=startup??new();
        InitializeComponent();
        AssetWarning.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding(nameof(BuddyRuntime.Warning)));
        AssetWarning.SetBinding(VisibilityProperty,new System.Windows.Data.Binding(nameof(BuddyRuntime.HasWarning)){Converter=new BooleanToVisibilityConverter()});
        FitToWorkArea(SystemParameters.WorkArea);
        StartupPanel.IsExpanded=Height>=650;
        SizeChanged+=(_,e)=>{if((e.PreviousSize.Height>=650)!=(e.NewSize.Height>=650))StartupPanel.IsExpanded=e.NewSize.Height>=650;};
        cpu=new("CPU","處理器","#3976B9");memory=new("RAM","實體記憶體","#A97946");gpu=new("GPU","最忙碌引擎","#578C81");
        MetricHost.Children.Add(cpu);MetricHost.Children.Add(memory);MetricHost.Children.Add(gpu);
        Roster.ItemsSource=coordinator.Buddies;
        coordinator.Buddies.CollectionChanged+=(_,_)=>BuddyCount.Text=$"{coordinator.Buddies.Count} / 20";
        BuddyCount.Text=$"{coordinator.Buddies.Count} / 20";
        editTimer.Tick+=(_,_)=>{editTimer.Stop();ApplyDraft();};
        coordinator.Sampled+=sample=>{cpu.Update(sample.Cpu,"處理器忙碌時間（可能與工作管理員的效能校正數字不同）");memory.Update(sample.Memory,"已使用的實體記憶體比例");gpu.Update(sample.Gpu,sample.GpuStatus);UpdateCurve();};
        coordinator.Notice+=ShowNotice;
        coordinator.ExternalSettingsChanging+=FlushEdits;
        coordinator.ExternalSettingsChanged+=LoadSelected;
        coordinator.BuddyOpened+=OpenBuddy;
        coordinator.TaskbarStatusChanged+=text=>DockStatus.Text=text;
        DockStatus.Text=coordinator.TaskbarStatus;
        try{startupEnabled=this.startup.IsEnabled;}
        catch(Exception ex) when(ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {StartupBox.IsEnabled=false;ShowNotice("無法讀取開機啟動設定："+ex.Message);}
        StartupBox.IsChecked=startupEnabled;
        BackgroundBox.IsChecked=coordinator.Settings.LoginStartHidden;
        BackgroundBox.IsEnabled=StartupBox.IsChecked==true;
        IsVisibleChanged+=(_,_)=>coordinator.SetManagerVisible(IsVisible&&WindowState!=WindowState.Minimized);
        StateChanged+=(_,_)=>coordinator.SetManagerVisible(IsVisible&&WindowState!=WindowState.Minimized);
        loading=false;
        if(coordinator.Buddies.Count>0) Roster.SelectedIndex=0;else LoadSelected();
    }
    private void OnStartupChanged(object sender,RoutedEventArgs e)
    {
        if(loading)return;
        try
        {
            var hidden=BackgroundBox.IsChecked==true;
            startup.SetEnabled(StartupBox.IsChecked==true,Environment.ProcessPath!,hidden);
            startupEnabled=StartupBox.IsChecked==true;
            coordinator.Settings.LoginStartHidden=hidden;coordinator.SaveSoon();
            BackgroundBox.IsEnabled=StartupBox.IsChecked==true;
        }
        catch(Exception ex) when(ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {loading=true;StartupBox.IsChecked=startupEnabled;BackgroundBox.IsChecked=coordinator.Settings.LoginStartHidden;BackgroundBox.IsEnabled=startupEnabled;loading=false;ShowNotice("無法更新開機啟動設定："+ex.Message);}
    }
    public void ShowNotice(string message) {Status.Text=message;Status.Foreground=new SolidColorBrush(Color.FromRgb(164,100,96));}
    public void FitToWorkArea(Rect area)
    {
        var availableWidth=Math.Max(1,area.Width-28);var availableHeight=Math.Max(1,area.Height-28);
        MinWidth=Math.Min(940,availableWidth);MinHeight=Math.Min(650,availableHeight);
        MaxWidth=area.Width;MaxHeight=area.Height;
        Width=Math.Min(1160,availableWidth);Height=Math.Min(850,availableHeight);
    }
    private void LoadSelected()
    {
        loading=true;
        Editor.Visibility=selected==null?Visibility.Collapsed:Visibility.Visible;
        EmptyState.Visibility=selected==null?Visibility.Visible:Visibility.Collapsed;
        Editor.DataContext=selected;
        if(selected!=null)
        {
            var b=selected.Config;Editor.DataContext=selected;
            SelectedTitle.Text=b.Name;SelectedInfo.Text=$"{selected.SourceName} 的專屬夥伴 · 所有設定都只影響這一隻";
            NameBox.Text=b.Name;SourceBox.SelectedIndex=(int)b.Source;
            SpriteBox.SelectedIndex=!string.IsNullOrEmpty(b.GifPath)?3:b.Builtin switch{"blob"=>1,"bird"=>2,_=>0};
            DesktopBox.IsChecked=b.ShowDesktop;TrayBox.IsChecked=b.ShowTray;PauseBox.IsChecked=b.Paused;
            LabelBox.SelectedIndex=(int)b.Label;MinSlider.Value=b.MinimumSpeed;MaxSlider.Value=b.MaximumSpeed;
            ResponseSlider.Value=b.ResponseSeconds;SizeSlider.Value=b.Size;
            TopmostBox.IsChecked=b.Topmost;LockBox.IsChecked=b.Locked;ThroughBox.IsChecked=b.ClickThrough;
        }
        loading=false;dirty=false;UpdateValueLabels();UpdateCurve();
    }
    private void OnSelected(object sender,SelectionChangedEventArgs e)
    {
        if(!ReferenceEquals(e.OriginalSource,Roster))return;
        if(selected!=null&&!loading){editTimer.Stop();ApplyDraft();}
        selected=Roster.SelectedItem as BuddyRuntime;LoadSelected();
    }
    private void OnEdited(object sender,RoutedEventArgs e)
    {
        if(loading||selected==null)return;
        dirty=true;UpdateValueLabels();UpdateCurve();editTimer.Stop();editTimer.Start();
    }
    private void UpdateValueLabels()
    {
        if(MinValue==null)return;
        MinValue.Text=$"{MinSlider.Value:0.0}×";MaxValue.Text=$"{Math.Max(MinSlider.Value,MaxSlider.Value):0.0}×";
        ResponseValue.Text=$"{ResponseSlider.Value:0.0}s";SizeValue.Text=$"{SizeSlider.Value:0}";
        SpeedRange.Text=$"{MinSlider.Value:0.0}× → {Math.Max(MinSlider.Value,MaxSlider.Value):0.0}×";
    }
    public void FlushEdits() {editTimer.Stop();ApplyDraft();}
    private void ApplyDraft()
    {
        if(loading||selected==null||!dirty)return;
        dirty=false;
        var b=selected.Config;
        b.Name=NameBox.Text;b.Source=(MetricSource)Math.Max(0,SourceBox.SelectedIndex);
        b.ShowDesktop=DesktopBox.IsChecked==true;b.ShowTray=TrayBox.IsChecked==true;b.Paused=PauseBox.IsChecked==true;
        b.Label=(LabelMode)Math.Max(0,LabelBox.SelectedIndex);
        b.MinimumSpeed=MinSlider.Value;b.MaximumSpeed=Math.Max(MinSlider.Value,MaxSlider.Value);
        b.ResponseSeconds=ResponseSlider.Value;b.Size=(int)SizeSlider.Value;
        b.Topmost=TopmostBox.IsChecked==true;b.Locked=LockBox.IsChecked==true;b.ClickThrough=ThroughBox.IsChecked==true;
        if(MaxSlider.Value<b.MinimumSpeed){loading=true;MaxSlider.Value=b.MinimumSpeed;loading=false;}
        coordinator.Apply(selected);
        SelectedTitle.Text=b.Name;SelectedInfo.Text=$"{selected.SourceName} 的專屬夥伴 · 所有設定都只影響這一隻";
        Status.Text="設定會自動儲存 · 關閉視窗後，夥伴仍會繼續陪伴。";Status.Foreground=(Brush)FindResource("Muted");
    }
    private void OnSpriteChanged(object sender,SelectionChangedEventArgs e)
    {
        if(loading||selected==null||SpriteBox.SelectedIndex<0||SpriteBox.SelectedIndex==3)return;
        FlushEdits();
        var kind=new[]{"cat","blob","bird"}[SpriteBox.SelectedIndex];
        coordinator.SetBuiltin(selected,kind);
    }
    private void UpdateCurve()
    {
        if(Curve==null||Curve.ActualWidth<=0||MinSlider==null)return;
        var w=Math.Max(1,Curve.ActualWidth-10);
        double Y(double speed)=>42-speed/8*38;
        var min=MinSlider.Value;var max=Math.Max(min,MaxSlider.Value);
        CurveLine.Points=new PointCollection(new[]{new Point(5,Y(min)),new Point(w,Y(max))});
        var load=selected==null?0:coordinator.Snapshot.Get((MetricSource)Math.Max(SourceBox.SelectedIndex,0))??0;
        Canvas.SetLeft(CurveDot,load/100*(w-5));Canvas.SetTop(CurveDot,Y(Speed.Map(load,min,max))-5);
    }
    private void OnCurveSize(object sender,SizeChangedEventArgs e)=>UpdateCurve();
    private void OnAdd(object sender,RoutedEventArgs e)
    {
        FlushEdits();
        try {Roster.SelectedItem=coordinator.Add();NameBox.Focus();NameBox.SelectAll();}
        catch(InvalidOperationException ex){ShowNotice(ex.Message);}
    }
    private void OnRemove(object sender,RoutedEventArgs e)
    {
        if(selected==null)return;
        if(MessageBox.Show(this,$"要移除「{selected.Name}」嗎？匯入的原始 GIF 不會被刪除。","移除夥伴",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        editTimer.Stop();var previous=selected;selected=null;coordinator.Remove(previous);
        if(coordinator.Buddies.Count>0)Roster.SelectedIndex=0;else LoadSelected();
    }
    private async void OnImport(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog{Filter="GIF 動畫 (*.gif)|*.gif",Title="選擇夥伴的 GIF"};
        if(dialog.ShowDialog(this)==true)await ImportGif(dialog.FileName);
    }
    public async Task ImportGif(string path)
    {
        if(!ImportButton.IsEnabled){ShowNotice("GIF 正在匯入，請稍候再匯入下一個。");return;}
        if(selected==null){Roster.SelectedItem=coordinator.Buddies.Count>0?coordinator.Buddies[0]:coordinator.Add();}
        FlushEdits();var buddy=selected!;
        ImportButton.IsEnabled=false;ShowNotice("正在準備 GIF…");
        try
        {
            if(!await coordinator.LoadAppearanceAsync(buddy,path,true))return;
            if(ReferenceEquals(selected,buddy)){FlushEdits();LoadSelected();}
            Status.Text="GIF 已匯入，原始檔案移動後仍可使用。";Status.Foreground=(Brush)FindResource("Muted");
        }
        catch(Exception ex) when(ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException or System.Runtime.InteropServices.ExternalException)
        {ShowNotice("GIF 匯入失敗："+ex.Message);}
        finally{ImportButton.IsEnabled=true;}
    }
    private void OnDragOver(object sender,DragEventArgs e)
    {e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
    private async void OnDrop(object sender,DragEventArgs e)
    {if(e.Data.GetData(DataFormats.FileDrop) is string[] {Length:>0} files){e.Handled=true;await ImportGif(files[0]);}}
    private void OnSave(object sender,RoutedEventArgs e){FlushEdits();if(coordinator.Save())Status.Text="已儲存所有夥伴設定。";}
    private void OnResetPosition(object sender,RoutedEventArgs e){FlushEdits();if(selected!=null)coordinator.ResetPosition(selected);}
    private void OnHide(object sender,RoutedEventArgs e){FlushEdits();Hide();}
    private void OnMinimize(object sender,RoutedEventArgs e)=>WindowState=WindowState.Minimized;
    private void OnTitleDrag(object sender,MouseButtonEventArgs e)
    {if(e.OriginalSource is TextBlock or Grid or StackPanel){if(e.ClickCount==2)WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;else DragMove();}}
    private void OnExit(object sender,RoutedEventArgs e)=>((App)Application.Current).ExitApplication();
    private void OnClosing(object? sender,CancelEventArgs e){if(!AllowClose){FlushEdits();e.Cancel=true;Hide();}}
    public void Reopen() {FlushEdits();LoadSelected();Show();if(WindowState==WindowState.Minimized)WindowState=WindowState.Normal;Activate();}
    public void OpenBuddy(BuddyRuntime buddy){FlushEdits();Roster.SelectedItem=buddy;Reopen();}
    public void ExerciseEditorForCheck()
    {
        Roster.SelectedIndex=0;NameBox.Text="麻糬 · 測試";SourceBox.SelectedIndex=0;MinSlider.Value=.6;MaxSlider.Value=4.5;
        LabelBox.SelectedIndex=1;SizeSlider.Value=104;FlushEdits();
    }
}
