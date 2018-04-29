using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace HeadDirectionWpf
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        //ストーリーボード
        Storyboard storyboard = null;

        string movieFileName = "";

        //再生中かどうか
        bool isPlaying = false;

        //OpenposeのJsonファイル
        private OpenposeJsonSequence jsonSequence;
        //private OpenposeOutput openposeOutput;
       // private People people;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void PlayMovie()
        {
            //メディアタイムラインを作成
            MediaTimeline mediaTimeline = new MediaTimeline(new Uri(movieFileName));
            mediaTimeline.CurrentTimeInvalidated += new EventHandler(mediaTimeline_CurrentTimeInvalidated);
            Storyboard.SetTargetName(mediaTimeline, MoviePlayer.Name);

            //ストーリーボードを作成・開始
            storyboard = new Storyboard();
            storyboard.Children.Add(mediaTimeline);
            storyboard.Begin(this, true);

            //コントロールの変更
            SliderTime.IsEnabled = true;
            Play.IsEnabled = true;
            Stop.IsEnabled = true;

            isPlaying = true;

        }

        private void StopMovie()
        {
            //ストーリーボードの停止
            storyboard.Stop(this);
            storyboard.Children.Clear();
            storyboard = null;

            //コントロールの変更
            SliderTime.Value = 0.0;
            SliderTime.IsEnabled = false;
            Stop.IsEnabled = false;

            isPlaying = false;

        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            //何も再生されていないときは新しく再生を始める
            if (storyboard == null)
            {
                PlayMovie();
            }
            else //すでに再生されている場合は，一時停止か再び再生か
            {
                if (isPlaying)
                {
                    storyboard.Pause(this);
                }
                else
                {
                    storyboard.Resume(this);
                }
                Play.Content = isPlaying ? "Pause" : "Play";
                isPlaying = !isPlaying;
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            //ファイルを開くダイアログボックスを表示
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "動画ファイル(*.avi;*.wmv;*.mpg;*.mpeg;*.mp4;*.mkv;*.m2ts;*.flv)|*.avi;*.wmv;*.mpg;*.mpeg;*.mp4;*.mkv;*.m2ts;*.flv";
            if (openFileDialog.ShowDialog() != true)
                return;

            //ファイル名を保存する
            movieFileName = openFileDialog.FileName;
            //開いたファイルを表示
            FileName.Text = movieFileName.Split("\\"[0]).LastOrDefault();

            if (storyboard != null)
            {
                StopMovie();
            }

            PlayMovie();
        }

        private void mediaTimeline_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            if (storyboard != null)
            {
                SliderTime.Value = MoviePlayer.Clock.CurrentTime.Value.TotalMilliseconds;

                if (MoviePlayer.NaturalDuration.HasTimeSpan && MoviePlayer.Clock.CurrentTime.Value.TotalMilliseconds == MoviePlayer.NaturalDuration.TimeSpan.TotalMilliseconds)
                {
                    StopMovie();
                }
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            StopMovie();
        }

        private void MoviePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            SliderTime.Maximum = MoviePlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        private void SliderTime_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (isPlaying)
            {
                storyboard.Pause(this);
            }
        }

        private void SliderTime_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //動画をシークする
            storyboard.Seek(this, new TimeSpan((long)Math.Floor(SliderTime.Value * TimeSpan.TicksPerMillisecond)), TimeSeekOrigin.BeginTime);

            if (isPlaying)
            {
                storyboard.Resume(this);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void LoadJson_Click(object sender, RoutedEventArgs e)
        {
            jsonSequence = new OpenposeJsonSequence();
            jsonSequence.OpenposeOutputs = new List<OpenposeOutput>();
            //ファイルを開くダイアログボックスを表示
            OpenFileDialog openFileDialog = new OpenFileDialog();
            
            //複数ファイル選択可
            openFileDialog.Multiselect = true;
            //openFileDialog.Filter = "動画ファイル(*.avi;*.wmv;*.mpg;*.mpeg;*.mp4;*.mkv;*.m2ts;*.flv)|*.avi;*.wmv;*.mpg;*.mpeg;*.mp4;*.mkv;*.m2ts;*.flv";
            if (openFileDialog.ShowDialog() != true)
                return;

            //すべてのフレームのJSONファイルを選択する
            foreach(var fileName in openFileDialog.FileNames)
            {
                string jsonContent;
                using(var sr = new StreamReader(fileName))
                {
                    jsonContent = sr.ReadLine();
                }
                OpenposeOutput output = JsonConvert.DeserializeObject<OpenposeOutput>(jsonContent);
                jsonSequence.OpenposeOutputs.Add(output);
            }
        }
    }
}
