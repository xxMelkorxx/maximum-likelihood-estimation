using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ScottPlot;
using ScottPlot.Control;

namespace MLE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _bgGenerateSignal; //_bgResearch;
        private SignalGenerator _signalGenerator;
        private ModulationType _modulationType;
        private Dictionary<string, object> _params1, _params2;
        private bool _isNoise;
        private int _maxIndex;

        public MainWindow()
        {
            InitializeComponent();

            _bgGenerateSignal = (BackgroundWorker)FindResource("BackgroundWorkerGenerateSignal");
            // _bgResearch = (BackgroundWorker)FindResource("BackgroundWorkerConductResearch");
        }

        private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
        {
            RbIsAsk.IsChecked = true;

            SetUpChart(ChartBitSequence, "Битовая последовательность", "Время, с", "Амплитуда");
            SetUpChart(ChartDesiredSignal, "Искомый сигнал", "Время, с", "Амплитуда");
            SetUpChart(ChartResearchedSignal, "Исследуемый сигнал", "Время, с", "Амплитуда");

            OnClickButtonGenerateBitsSequence(null, null);
        }

        #region ################# GENERATE SIGNALS #################

        private void OnGenerateSignal(object sender, EventArgs e)
        {
            if (_bgGenerateSignal.IsBusy)
                return;

            _params1 = new Dictionary<string, object>
            {
                ["countNumbers"] = (int)Math.Pow(2, NudCountNumbers.Value ?? 15),
                ["countBits"] = NudCountBits.Value ?? 200,
                ["bps"] = NudBps.Value ?? 10,
                ["a0"] = NudA0.Value ?? 1,
                ["f0"] = NudF0.Value ?? 1000,
                ["phi0"] = NudPhi0.Value ?? 0,
                ["startBit"] = NudStartBit.Value ?? 100,
                ["modulationType"] = _modulationType,
                ["SNR"] = NudSnr.Value ?? 5,
                ["doppler"] = NudFDoppler.Value ?? 50
            };
            switch (_modulationType)
            {
                case ModulationType.ASK:
                    _params1["A1"] = NudA1.Value ?? 5;
                    _params1["A2"] = NudA2.Value ?? 15;
                    break;
                case ModulationType.FSK:
                    _params1["dF"] = NudDeltaF.Value ?? 50;
                    break;
                case ModulationType.PSK:
                    break;
                default:
                    throw new ArgumentException("Параметр не инициализирован");
            }

            // Получение битовой последовательности.
            var bitsSequence = new List<int>();
            TbBitsSequence.Text.Replace(" ", "").ToList().ForEach(b => bitsSequence.Add(b == '1' ? 1 : 0));
            _params1["bitsSequence"] = bitsSequence;

            ButtonGenerateSignal.IsEnabled = false;
            _bgGenerateSignal.RunWorkerAsync();
        }

        private void OnDoWorkBackgroundWorkerGenerateSignal(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Формирование модулированного сигнала.
                _signalGenerator = new SignalGenerator(_params1);
                _signalGenerator.GenerateSignals(_params1);

                // Наложение шума на сигналы.
                if (_isNoise)
                    _signalGenerator.MakeNoise((double)_params1["SNR"]);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка!", exception.Message);
            }
        }

        private void OnRunWorkerCompletedBackgroundWorkerGenerateSignal(object sender, RunWorkerCompletedEventArgs e)
        {
            ChartDesiredSignal.Visibility = Visibility.Visible;
            ChartResearchedSignal.Visibility = Visibility.Visible;
            ChartCrossCorrelation.Visibility = Visibility.Visible;
            ChartResearch.Visibility = Visibility.Collapsed;

            // Очистка графиков.
            ChartBitSequence.Plot.Clear();
            ChartDesiredSignal.Plot.Clear();
            ChartResearchedSignal.Plot.Clear();
            ChartCrossCorrelation.Plot.Clear();

            // График битовой последовательности.
            ChartBitSequence.Plot.AddSignalXY(
                _signalGenerator.bitsSignal.Select(p => p.X).ToArray(),
                _signalGenerator.bitsSignal.Select(p => p.Y).ToArray()
            );
            ChartBitSequence.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.bitsSignal.Max(p => p.X), yMin: -2, yMax: 2);
            ChartBitSequence.Refresh();

            // График искомого сигнала.
            ChartDesiredSignal.Plot.AddSignalXY(
                _signalGenerator.desiredSignal.Select(p => p.X).ToArray(),
                _signalGenerator.desiredSignal.Select(p => p.Y.Real).ToArray()
            );
            var yMax = _signalGenerator.desiredSignal.Max(p => double.Abs(p.Y.Real));
            ChartDesiredSignal.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.desiredSignal.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
            ChartDesiredSignal.Refresh();

            // График исследуемого сигнала.
            ChartResearchedSignal.Plot.AddSignalXY(
                _signalGenerator.researchedSignal.Select(p => p.X).ToArray(),
                _signalGenerator.researchedSignal.Select(p => p.Y.Real).ToArray()
            );
            ChartResearchedSignal.Plot.SetAxisLimits(xMin: 0, xMax: _signalGenerator.researchedSignal.Max(p => p.X), yMin: -yMax * 1.5, yMax: yMax * 1.5);
            ChartResearchedSignal.Plot.AddVerticalLine((int)_params1["startBit"] * _signalGenerator.tb, Color.Green);
            ChartResearchedSignal.Plot.AddVerticalLine((int)_params1["startBit"] + _signalGenerator.Nb * _signalGenerator.tb, Color.Green);
            ChartResearchedSignal.Refresh();

            ButtonGenerateSignal.IsEnabled = true;
        }

        #endregion

        #region ################# ONCHECKED #################

        private void OnCheckedRbIsAsk(object sender, RoutedEventArgs e)
        {
            _modulationType = ModulationType.ASK;
            GbAskParams.IsEnabled = true;
            GbFskParams.IsEnabled = false;
        }

        private void OnCheckedRbIsFsk(object sender, RoutedEventArgs e)
        {
            _modulationType = ModulationType.FSK;
            GbAskParams.IsEnabled = false;
            GbFskParams.IsEnabled = true;
        }

        private void OnCheckedRbIsPsk(object sender, RoutedEventArgs e)
        {
            _modulationType = ModulationType.PSK;
            GbAskParams.IsEnabled = false;
            GbFskParams.IsEnabled = false;
        }

        private void OnCheckedCbIsNoise(object sender, RoutedEventArgs e)
        {
            _isNoise = true;
            NudSnr.IsEnabled = true;
            OnGenerateSignal(null, null);
        }

        private void OnUncheckedCbIsNoise(object sender, RoutedEventArgs e)
        {
            _isNoise = false;
            NudSnr.IsEnabled = false;
            OnGenerateSignal(null, null);
        }

        #endregion

        #region ################# GENERATE BIT SEQUENCE #################

        private void OnClickButtonAddZero(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Text += '0';
            ButtonGenerateSignal.IsEnabled = true;
        }

        private void OnClickButtonAddOne(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Text += '1';
            ButtonGenerateSignal.IsEnabled = true;
        }

        private void OnClickButtonClearBits(object sender, RoutedEventArgs e)
        {
            TbBitsSequence.Clear();
            ButtonGenerateSignal.IsEnabled = false;
        }

        private void OnClickButtonGenerateBitsSequence(object sender, RoutedEventArgs e)
        {
            var length = NudNb.Value ?? 16;
            var bits = SignalGenerator.GenerateBitsSequence(length);

            TbBitsSequence.Clear();
            TbBitsSequence.Text = bits;

            ButtonGenerateSignal.IsEnabled = true;
            OnGenerateSignal(null, null);
        }

        #endregion

        private static void SetUpChart(IPlotControl chart, string title, string labelX, string labelY)
        {
            chart.Plot.Title(title);
            chart.Plot.XLabel(labelX);
            chart.Plot.YLabel(labelY);
            chart.Plot.XAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
            chart.Plot.YAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
            chart.Plot.XAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
            chart.Plot.YAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
            chart.Plot.Margins(x: 0.0, y: 0.8);
            chart.Plot.SetAxisLimits(xMin: 0);
            chart.Configuration.Quality = QualityMode.High;
            chart.Configuration.DpiStretch = false;
            chart.Refresh();
        }
    }
}