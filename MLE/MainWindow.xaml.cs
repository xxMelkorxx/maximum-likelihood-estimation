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

        public MainWindow()
        {
            InitializeComponent();

            _bgGenerateSignal = (BackgroundWorker)FindResource("BackgroundWorkerGenerateSignal");
            // _bgResearch = (BackgroundWorker)FindResource("BackgroundWorkerConductResearch");
        }

        private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
        {
            OnGenerateSignal(null, null);
            
            SetUpChart(ChartDesiredSignal, "Искомый сигнал", "Время, с", "Амплитуда");
            SetUpChart(ChartResearchedSignal, "Исследуемый сигнал", "Время, с", "Амплитуда");
            
            OnGenerateSignal(null, null);
        }

        #region ################# GENERATE SIGNALS #################

        private void OnGenerateSignal(object sender, EventArgs e)
        {
            if (_bgGenerateSignal.IsBusy)
                return;

            _params1 = new Dictionary<string, object>
            {
                ["a0"] = NudA0.Value ?? 1,
                ["f0"] = NudF0.Value ?? 1000,
                ["phi0"] = NudPhi0.Value ?? 0,
                ["fd"] = NudFd.Value ?? 1,
                ["startBit"] = NudStartBit.Value ?? 100,
                ["countBits"] = NudCountBits.Value ?? 200,
                ["modulationType"] = _modulationType,
                ["isNoise"] = CbIsNoise.IsChecked ?? false,
                ["SNR"] = NudSnr.Value ?? 5
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
            
            ButtonGenerateSignal.IsEnabled = false;
            _bgGenerateSignal.RunWorkerAsync();
        }

        private void OnDoWorkBackgroundWorkerGenerateSignal(object sender, DoWorkEventArgs e)
        {
            try
            {
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка!", exception.Message);
            }
        }

        private void OnRunWorkerCompletedBackgroundWorkerGenerateSignal(object sender, RunWorkerCompletedEventArgs e) { ButtonGenerateSignal.IsEnabled = true; }

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

        private void OnCheckedCheckBoxIsNoise(object sender, RoutedEventArgs e)
        {
            NudSnr.IsEnabled = CbIsNoise.IsChecked ?? false;
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