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
using System.Timers;

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

        private Timer timer;

        //描画用変数
        private float currentMilliSec = 0f;
        private int currentFrame = 0;


        //
        //CONST values
        //
        #region
        //30fpsの時1フレームのmillisec
        const float ONE_FLAME = 1000 / 30.0f;

        //GoProの解像度
        const float INPUT_VIDEO_WIDTH = 1280f;
        const float INPUT_VIDEO_HEIGHT = 720f;

        //OpenposeのJSONに含まれる配列の要素index
        const int NOSE = 0;
        const int REAR = 16;
        const int LEAR = 17;
        const int RWRIST = 4;
        const int LWRIST = 7;
        #endregion



        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new Timer(ONE_FLAME);
            timer.Elapsed += this.Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var openposeOutput = jsonSequence.OpenposeOutputs.ElementAt(currentFrame);
            DrawKeyPoint(openposeOutput.Peoples);
            currentFrame++;
        }

        private void RecordCurrentTimeAndFrame()
        {
            currentMilliSec = MoviePlayer.Clock.CurrentTime.Value.Milliseconds;
            currentFrame = (int)(currentMilliSec / ONE_FLAME);
        }

        //
        //動画再生関連の処理
        //
        #region
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
                timer.Start();
            }
            else //すでに再生されている場合は，一時停止か再び再生か
            {
                if (isPlaying)
                {
                    storyboard.Pause(this);
                    timer.Stop();
                }
                else
                {
                    storyboard.Resume(this);
                    timer.Start();
                }
                Play.Content = isPlaying ? "Pause" : "Play";
                isPlaying = !isPlaying;
            }
            RecordCurrentTimeAndFrame();
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
            timer.Stop();
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
                timer.Stop();
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
            RecordCurrentTimeAndFrame();
            timer.Start();
        }

        #endregion

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
                timer.Stop();
            }

            PlayMovie();
            timer.Start();
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
            foreach (var fileName in openFileDialog.FileNames)
            {
                string jsonContent;
                using (var sr = new StreamReader(fileName))
                {
                    jsonContent = sr.ReadLine();
                }
                OpenposeOutput output = JsonConvert.DeserializeObject<OpenposeOutput>(jsonContent);
                jsonSequence.OpenposeOutputs.Add(output);
            }
        }

        //
        //Canvas描画系メソッド
        //
        #region
        private void DrawKeyPoint(List<People> peoples)
        {
            CanvasBody.Children.Clear();

            //必要なkey_pointを人数分描画
            foreach (var people in peoples)
            {
                var points = people.Pose_keypoints_2d;
                DrawEllipse(points[NOSE * 3], points[NOSE * 3 + 1], 10, Brushes.Blue);
                DrawEllipse(points[REAR * 3], points[REAR * 3 + 1], 10, Brushes.Yellow);
                DrawEllipse(points[LEAR * 3], points[LEAR * 3 + 1], 10, Brushes.Yellow);
                DrawEllipse(points[RWRIST * 3], points[RWRIST * 3 + 1], 10, Brushes.Pink);
                DrawEllipse(points[LWRIST * 3], points[LWRIST * 3 + 1], 10, Brushes.Pink);
            }


        }

        private void DrawEllipse(float key_pointX, float key_pointY, int R, Brush brush)
        {
            var ellipse = new Ellipse() { Width = R, Height = R, Fill = brush };

            var x = key_pointX * CanvasBody.ActualWidth / INPUT_VIDEO_WIDTH;
            var y = key_pointY * CanvasBody.ActualHeight / INPUT_VIDEO_HEIGHT;
            Canvas.SetLeft(ellipse, x - (R / 2));
            Canvas.SetTop(ellipse, y - (R / 2));

            CanvasBody.Children.Add(ellipse);
        }
        #endregion
    }
}
