using LiveCharts; //Core of the library
using LiveCharts.Configurations;
using LiveCharts.Wpf; //The WPF controls
using MaterialSkin;
using MaterialSkin.Controls;
using MeasureM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace HLHApp
{
    public partial class LineForm : MaterialForm
    {
        private List<MeasureModel> measureModels = new List<MeasureModel>();
        private List<MeasureModel> measureModelsPre = new List<MeasureModel>();

        private GrayModel graymodel;  // 声明灰色系统模型，进行时间序列的预测处理
        private Thread thread;

        public ChartValues<MeasureModel> ChartValues { get; set; }
        public ChartValues<MeasureModel> ChartPredictionValues { get; set; }
        public LineForm()
        {
            InitializeComponent();
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);

            graymodel = new GrayModel();
            ShowChart();

        }

        private void ShowChart()
        {
            var mapper = Mappers.Xy<MeasureModel>()
               .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
               .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //the ChartValues property will store our values array
            ChartValues = new ChartValues<MeasureModel>();
            ChartPredictionValues = new ChartValues<MeasureModel>();

            cartesianChart1.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "观测值",
                    Values = ChartValues,
                    PointGeometrySize = 18,
                    StrokeThickness = 3,
                    Stroke=System.Windows.Media.Brushes.ForestGreen,
                },
            };
            cartesianChart2.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "预测值",
                    Values = ChartPredictionValues,
                    PointGeometrySize = 18,
                    StrokeThickness = 3,
                    Stroke=System.Windows.Media.Brushes.ForestGreen,
                },

            };

            cartesianChart1.AxisX.Add(new Axis
            {
                DisableAnimations = true,
                Title = "Time",
                LabelFormatter = value => new System.DateTime((long)value).ToString("mm:ss"),
                Separator = new Separator
                {
                    Step = TimeSpan.FromSeconds(1).Ticks
                }
            });
            cartesianChart1.LegendLocation = LegendLocation.Right;


            cartesianChart2.AxisX.Add(new Axis
            {
                DisableAnimations = true,
                Title = "Time",
                LabelFormatter = value => new System.DateTime((long)value).ToString("mm:ss"),
                Separator = new Separator
                {
                    Step = TimeSpan.FromSeconds(1).Ticks
                }
            });
            cartesianChart2.LegendLocation = LegendLocation.Right;
            SetAxisLimits(System.DateTime.Now);

            thread = new Thread(new ThreadStart(ShowData));
            thread.IsBackground = true;
            thread.Start();  // 设置线程启动，完成对于数据的监控处理 
        }


        private void SetAxisLimits(System.DateTime now)
        {
            cartesianChart1.AxisX[0].MaxValue = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 100ms ahead
            cartesianChart1.AxisX[0].MinValue = now.Ticks - TimeSpan.FromSeconds(19).Ticks; //we only care about the last 8 seconds
            cartesianChart2.AxisX[0].MaxValue = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 100ms ahead
            cartesianChart2.AxisX[0].MinValue = now.Ticks - TimeSpan.FromSeconds(19).Ticks; //we only care about the last 8 seconds
        }

        public void InsertMeasureModel(DateTime date_time, Double value)
        {
            MeasureModel mm = new MeasureModel();
            mm.DateTime = date_time;
            mm.Value = value;
            measureModels.Add(mm);
            graymodel.InsertData(mm);
        }

        private void DrawLine()
        {
            bool preData = true;
            double trueDataNum = 0;
            double preDataNum = 0;
            if (measureModels.Count > 0)
            {
                Action<DateTime> AsyncUIDelegate1 = delegate (DateTime now)
                {
                    cartesianChart1.AxisX[0].MaxValue = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 100ms ahead
                    cartesianChart1.AxisX[0].MinValue = now.Ticks - TimeSpan.FromSeconds(19).Ticks; //we only care about the last 8 seconds
                };//定义一个委托,用于修改坐标轴1
                Action<DateTime> AsyncUIDelegate2 = delegate (DateTime now)
                {
                    cartesianChart2.AxisX[0].MaxValue = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 100ms ahead
                    cartesianChart2.AxisX[0].MinValue = now.Ticks - TimeSpan.FromSeconds(19).Ticks; //we only care about the last 8 seconds
                };//定义一个委托，用于修改坐标轴2
                Action<String> AsyncUIDelegate3 = delegate (String data)
                {
                    textBox1.Clear();
                    textBox1.AppendText(data);
                };//定义一个委托，用于设置准确率
                Action<String> AsyncUIDelegate4 = delegate (String data)
                {
                    textBox2.Clear();
                    textBox2.AppendText(data);
                };//定义一个委托，用于设置灰色系统预测参数A0
                Action<String> AsyncUIDelegate5 = delegate (String data)
                {
                    textBox3.Clear();
                    textBox3.AppendText(data);
                };//定义一个委托，用于设置灰色系统预测参数A1
                Action<String> AsyncUIDelegate6 = delegate (String data)
                {
                    textBox4.Clear();
                    textBox4.AppendText(data);
                };//定义一个委托，用于设置灰色系统预测参数A2     
                try
                {
                    cartesianChart1.Invoke(AsyncUIDelegate1, new object[] { measureModels.Last().DateTime });  // 添加异常声明，进行处理
                }
                catch (Exception)
                {

                }

                foreach (MeasureModel mm in measureModels)
                {
                    ChartValues.Add(mm);
                    trueDataNum = mm.Value;
                    if (ChartValues.Count > 20)
                    {
                        ChartValues.RemoveAt(0);
                    }
                }
                if (graymodel.GetSize() == 30)
                {
                    if (ChartPredictionValues.Count > 0)
                    {
                        for (int i = 0; i < 6; i++)
                            ChartPredictionValues.RemoveAt(ChartPredictionValues.Count - 1);
                    }
                    foreach (MeasureModel mm in graymodel.PredictMeaseureModelRange(7))
                    {
                        if (preData)
                        {
                            preDataNum = mm.Value;
                            preData = false;
                        }
                        ChartPredictionValues.Add(mm);
                    }
                    try
                    {
                        cartesianChart2.Invoke(AsyncUIDelegate2, new object[] { ChartPredictionValues.Last().DateTime });
                    }
                    catch (Exception)
                    {

                    }
                }
                try
                {
                    double precent = Math.Abs(trueDataNum - preDataNum) / trueDataNum;
                    precent = precent * 100;
                    textBox1.Invoke(AsyncUIDelegate3, new object[] { precent.ToString("F2")+" %" });
                    textBox2.Invoke(AsyncUIDelegate4, new object[] { graymodel.Geta0().ToString("F2") });
                    textBox3.Invoke(AsyncUIDelegate5, new object[] { graymodel.Geta1().ToString("F2") });
                    textBox4.Invoke(AsyncUIDelegate6, new object[] { graymodel.Geta2().ToString("F2") });
                }
                catch (Exception)
                {

                }
                measureModels.Clear();
                preData = true;
            }
        }

        private void ShowData()
        {
            while (true)
            {
                DrawLine();  // 绘制数据点的动作
                Thread.Sleep(1000);  // 设置进程等待时间
            }
        }

    }
}
