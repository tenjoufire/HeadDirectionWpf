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
using System.Windows.Threading;
using System.Windows.Forms;

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

        private System.Timers.Timer timer;
        private DispatcherTimer dispatcherTimer;

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
        const int REYE = 14;
        const int LEYE = 15;

        //joint attentionが現れると仮定するY座標
        const float JOINT_ATTENTION_Y = 300f;
        #endregion



        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //timer = new System.Timers.Timer(1000);
            //timer.Elapsed += this.Timer_Elapsed;

            dispatcherTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, (int)ONE_FLAME)
            };
            dispatcherTimer.Tick += this.DispatcherTimer_Tick;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            var openposeOutput = jsonSequence.OpenposeOutputs.ElementAt(currentFrame);
            DrawKeyPoint(openposeOutput.Peoples);
            currentFrame++;
        }

        [STAThread]
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

            //key_point描画関係
            //currentFrame = 0;
            dispatcherTimer.Start();
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

            dispatcherTimer.Stop();

        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            //何も再生されていないときは新しく再生を始める
            if (storyboard == null)
            {
                PlayMovie();
                //timer.Start();
            }
            else //すでに再生されている場合は，一時停止か再び再生か
            {
                if (isPlaying)
                {
                    storyboard.Pause(this);
                    //timer.Stop();
                    dispatcherTimer.Stop();
                }
                else
                {
                    storyboard.Resume(this);
                    //timer.Start();
                    dispatcherTimer.Start();
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
            //timer.Stop();
            dispatcherTimer.Stop();
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
                //timer.Stop();
                dispatcherTimer.Stop();
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
            //timer.Start();
            dispatcherTimer.Start();
        }

        #endregion

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            //ファイルを開くダイアログボックスを表示
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
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
                //timer.Stop();
            }

            PlayMovie();
            //timer.Start();
        }


        private void LoadJson_Click(object sender, RoutedEventArgs e)
        {
            jsonSequence = new OpenposeJsonSequence();
            jsonSequence.OpenposeOutputs = new List<OpenposeOutput>();
            var fileDirPath = "";

            //フォルダを選択するダイアログの表示
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            var result = folderBrowserDialog.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                fileDirPath = folderBrowserDialog.SelectedPath;
            }
            else
            {
                return;
            }

            string[] fileNames = Directory.GetFiles(fileDirPath, "*_keypoints.json");

            //すべてのフレームのJSONファイルを選択する
            foreach (var fileName in fileNames)
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
        [STAThread]
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
                DrawEllipse(points[REYE * 3], points[REYE * 3 + 1], 10, Brushes.Violet);
                DrawEllipse(points[LEYE * 3], points[LEYE * 3 + 1], 10, Brushes.Violet);
                DrawLine(points);
                DrawLine2(points);
            }
        }

        private double ConvertOpenposeToCanvasCoordinateX(double value)
        {
            return value * CanvasBody.ActualWidth / INPUT_VIDEO_WIDTH;
        }

        private double ConvertOpenposeToCanvasCoordinateY(double value)
        {
            return value * CanvasBody.ActualHeight / INPUT_VIDEO_HEIGHT;
        }

        [STAThread]
        private void DrawEllipse(float key_pointX, float key_pointY, int R, Brush brush)
        {
            //欠損値に関しては描画しない
            if (key_pointX <= 0f || key_pointY <= 0f) return;

            var ellipse = new Ellipse() { Width = R, Height = R, Fill = brush };

            var x = ConvertOpenposeToCanvasCoordinateX(key_pointX);
            var y = ConvertOpenposeToCanvasCoordinateY(key_pointY);
            Canvas.SetLeft(ellipse, x - (R / 2));
            Canvas.SetTop(ellipse, y - (R / 2));

            CanvasBody.Children.Add(ellipse);
        }

        private void DrawLine(float[] points)
        {
            if (points[NOSE * 3] <= 0 || points[NOSE * 3 + 1] <= 0 || points[REAR * 3] <= 0 || points[REAR * 3 + 1] <= 0 || points[LEAR * 3] <= 0 || points[LEAR * 3 + 1] <= 0) return;
            var NOSEPoint = new Point(points[NOSE * 3], points[NOSE * 3 + 1]);
            var REARPoint = new Point(points[REAR * 3], points[REAR * 3 + 1]);
            var LEARPoint = new Point(points[LEAR * 3], points[LEAR * 3 + 1]);

            var earCenterX = (ConvertOpenposeToCanvasCoordinateX(REARPoint.X) + ConvertOpenposeToCanvasCoordinateX(LEARPoint.X)) / 2;
            var earCenterY = (ConvertOpenposeToCanvasCoordinateY(REARPoint.Y) + ConvertOpenposeToCanvasCoordinateY(LEARPoint.Y)) / 2;

            var katamuki = (ConvertOpenposeToCanvasCoordinateY(NOSEPoint.Y) - earCenterY) / (ConvertOpenposeToCanvasCoordinateX(NOSEPoint.X) - earCenterX);
            var jointX = (JOINT_ATTENTION_Y - earCenterY) / katamuki + earCenterX;
            var jointY = katamuki * (jointX - earCenterX) + earCenterY;

            var line = new Line()
            {
                X1 = earCenterX,
                Y1 = earCenterY,
                X2 = jointX,
                Y2 = jointY,
                Stroke = Brushes.Red,
                StrokeThickness = 5
            };
            //Canvas.SetLeft(line, 0);
            //Canvas.SetTop(line, 0);
            //line.StrokeThickness = 2;
            CanvasBody.Children.Add(line);
        }

        private void DrawLine2(float[] points)
        {
            if (points[NOSE * 3] <= 0 || points[NOSE * 3 + 1] <= 0 || points[REYE * 3] <= 0 || points[REYE * 3 + 1] <= 0 || points[LEYE * 3] <= 0 || points[LEYE * 3 + 1] <= 0) return;
            Point NOSEPoint = new Point(points[NOSE * 3], points[NOSE * 3 + 1]);
            Point REYEPoint = new Point(points[REYE * 3], points[REYE * 3 + 1]);
            Point LEYEPoint = new Point(points[LEYE * 3], points[LEYE * 3 + 1]);

            var eyeCenterX = (ConvertOpenposeToCanvasCoordinateX(REYEPoint.X) + ConvertOpenposeToCanvasCoordinateX(LEYEPoint.X)) / 2;
            var eyeCenterY = (ConvertOpenposeToCanvasCoordinateY(REYEPoint.Y) + ConvertOpenposeToCanvasCoordinateY(LEYEPoint.Y)) / 2;

            var katamuki = (ConvertOpenposeToCanvasCoordinateY(NOSEPoint.Y) - eyeCenterY) / (ConvertOpenposeToCanvasCoordinateX(NOSEPoint.X) - eyeCenterX);
            var jointX = (JOINT_ATTENTION_Y - eyeCenterY) / katamuki + eyeCenterX;
            if (double.IsPositiveInfinity(katamuki)) katamuki = 0;
            var jointY = katamuki * (jointX - eyeCenterX) + eyeCenterY;

            var line = new Line()
            {
                X1 = eyeCenterX,
                Y1 = eyeCenterY,
                X2 = jointX,
                Y2 = jointY,
                Stroke = Brushes.YellowGreen,
                StrokeThickness = 5
            };
            //Canvas.SetLeft(line, 0);
            //Canvas.SetTop(line, 0);
            //line.StrokeThickness = 2;
            CanvasBody.Children.Add(line);
        }


        /// <summary>
        /// 直線を計算するためのkey_point3つ ×2 から2直線の交点の座標を計算するメソッド
        /// </summary>
        /// <param name="threePoints1">直線1の座標3つ{NOSE,RIGHT,LEFT}の順</param>
        /// <param name="threePoints2">直線2の座標3つ{NOSE,RIGHT,LEFT}の順</param>
        /// <returns>交点の座標</returns>
        private Point CalcIntersectionPoint(Point[] threePoints1, Point[] threePoints2)
        {
            var intersectionPoint = new  Point();

            var centerX1 = (ConvertOpenposeToCanvasCoordinateX(threePoints1[1].X) + ConvertOpenposeToCanvasCoordinateX(threePoints1[2].X)) / 2;
            var centerY1 = (ConvertOpenposeToCanvasCoordinateY(threePoints1[1].Y) + ConvertOpenposeToCanvasCoordinateY(threePoints1[2].Y)) / 2;
            var katamuki1 = (ConvertOpenposeToCanvasCoordinateY(threePoints1[0].Y) - centerY1) / (ConvertOpenposeToCanvasCoordinateX(threePoints1[0].X) - centerX1);

            var centerX2 = (ConvertOpenposeToCanvasCoordinateX(threePoints2[1].X) + ConvertOpenposeToCanvasCoordinateX(threePoints2[2].X)) / 2;
            var centerY2 = (ConvertOpenposeToCanvasCoordinateY(threePoints2[1].Y) + ConvertOpenposeToCanvasCoordinateY(threePoints2[2].Y)) / 2;
            var katamuki2 = (ConvertOpenposeToCanvasCoordinateY(threePoints2[0].Y) - centerY2) / (ConvertOpenposeToCanvasCoordinateX(threePoints2[0].X) - centerX2);

            //y = ax - ax1 + y1 と y = Ax - Ax2 + y2 から2直線の交点の座標
            var b = katamuki1 * centerX1 * -1 + centerY1;
            var B = katamuki2 * centerX2 * -1 + centerY2;

            if (katamuki1 == katamuki2) return new Point(0,0);

            intersectionPoint.X = (B - b) / (katamuki1 - katamuki2);
            intersectionPoint.Y = ((katamuki1 * B) - (katamuki2 * b)) / (katamuki1 - katamuki2);

            return intersectionPoint;
        }
        #endregion
    }
}
