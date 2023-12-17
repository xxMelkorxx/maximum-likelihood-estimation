using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MLE;

public enum ModulationType
{
    ASK,
    FSK,
    PSK
}

public class SignalGenerator
{
    private const double pi2 = 2 * Math.PI;

    /// <summary>
    /// Битовая последовательность.
    /// </summary>
    private List<int> bitsSequence { get; }

    /// <summary>
    /// Длина битовой последовательности.
    /// </summary>
    public int Nb => bitsSequence.Count;

    /// <summary>
    /// Число отсчётов.
    /// </summary>
    public int CountNumbers { get; set; }

    public int CountBits { get; set; }

    /// <summary>
    /// Битрейт.
    /// </summary>
    public int BPS { get; }

    /// <summary>
    /// Тип модуляции сигнала.
    /// </summary>
    private ModulationType Type { get; }

    /// <summary>
    /// Амплитуда несущего сигнала.
    /// </summary>
    public double a0 { get; }

    /// <summary>
    /// Частота несущего сигнала.
    /// </summary>
    public double f0 { get; }

    /// <summary>
    /// Начальная фаза несущего сигнала.
    /// </summary>
    public double phi0 { get; }

    /// <summary>
    /// Частота дискретизации.
    /// </summary>
    public double fd => (double)BPS * CountNumbers / CountBits;

    /// <summary>
    /// Шаг по времени.
    /// </summary>
    public double dt => 1d / fd;

    /// <summary>
    /// Временной отрезок одного бита.
    /// </summary>
    public double tb => 1d / BPS;

    public double doppler { get; set; }

    public int StartBit { get; set; }

    /// <summary>
    /// Цифровой сигнал.
    /// </summary>
    public List<Pair<double>> bitsSignal { get; }

    /// <summary>
    /// Искомый сигнал.
    /// </summary>
    public List<Pair<Complex>> desiredSignal { get; private set; }

    /// <summary>
    /// Исследуемый сигнал.
    /// </summary>
    public List<Pair<Complex>> researchedSignal { get; private set; }

    public SignalGenerator(IReadOnlyDictionary<string, object> @params)
    {
        Type = (ModulationType)@params["modulationType"];
        CountNumbers = (int)@params["countNumbers"];
        CountBits = (int)@params["countBits"];
        BPS = (int)@params["bps"];
        a0 = (double)@params["a0"];
        f0 = (double)@params["f0"];
        phi0 = (double)@params["phi0"];
        doppler = (double)@params["doppler"];
        StartBit = (int)@params["startBit"];

        bitsSequence = new List<int>();
        ((List<int>)@params["bitsSequence"]).ForEach(b => bitsSequence.Add(b));

        bitsSignal = new List<Pair<double>>();
        desiredSignal = new List<Pair<Complex>>();
        researchedSignal = new List<Pair<Complex>>();
    }

    public void GenerateSignals(Dictionary<string, object> @params)
    {
        // Генерация длинного сигнала.
        var longBitsSequence = new List<int>();
        GenerateBitsSequence(StartBit).ToList().ForEach(b => longBitsSequence.Add(b == '1' ? 1 : 0));
        bitsSequence.ForEach(b => longBitsSequence.Add(b));
        GenerateBitsSequence(CountBits - Nb - StartBit).ToList().ForEach(b => longBitsSequence.Add(b == '1' ? 1 : 0));

        for (var i = 0; i < CountNumbers; i++)
        {
            var ti = dt * i;
            var bidx = (int)(ti / tb);
            var bi = longBitsSequence[bidx];
            var yi = Type switch
            {
                ModulationType.ASK => (bi == 0 ? (double)@params["A1"] : (double)@params["A2"]) * Complex.Exp(Complex.ImaginaryOne * (pi2 * (f0 + doppler) * ti + phi0)),
                ModulationType.FSK => a0 * Complex.Exp(Complex.ImaginaryOne * (pi2 * (f0 + (bi == 0 ? -1 : 1) * (double)@params["dF"] + doppler) * ti + phi0)),
                ModulationType.PSK => a0 * Complex.Exp(Complex.ImaginaryOne * (pi2 * (f0 + doppler) * ti + phi0 + (bi == 1 ? Math.PI : 0))),
                _ => 0
            };
            researchedSignal.Add(new Pair<Complex>(ti, yi));

            // Вставка сигнала.
            if (bidx >= StartBit && bidx < StartBit + Nb)
            {
                yi = Type switch
                {
                    ModulationType.ASK => (bi == 0 ? (double)@params["A1"] : (double)@params["A2"]) * Complex.Exp(Complex.ImaginaryOne * (pi2 * f0 * ti + phi0)),
                    ModulationType.FSK => a0 * Complex.Exp(Complex.ImaginaryOne * (pi2 * (f0 + (bi == 0 ? -1 : 1) * (double)@params["dF"]) * ti + phi0)),
                    ModulationType.PSK => a0 * Complex.Exp(Complex.ImaginaryOne * (pi2 * f0 * ti + phi0 + (bi == 1 ? Math.PI : 0))),
                    _ => 0
                };
                bitsSignal.Add(new Pair<double>(ti - StartBit * tb, bi));
                desiredSignal.Add(new Pair<Complex>(ti - StartBit * tb, yi));
            }
        }
    }

    /// <summary>
    /// Взаимная корреляция искомого и исследуемого сигнала.
    /// </summary>
    /// <returns></returns>
    public void MLE()
    {
        for (var i = 0; i < researchedSignal.Count - desiredSignal.Count + 1; i++)
        {
            var array = new List<Complex>();
            var corr = Complex.Zero;
            for (var j = 0; j < desiredSignal.Count; j++)
                corr += researchedSignal[i + j].Y * desiredSignal[j].Y;
            array.Add(corr / desiredSignal.Count);

            var fft = FFTClass.FFT(array.ToArray());
        }
    }   
    
    /// <summary>
    /// Генерация случайного числа с нормальным распределением.
    /// </summary>
    /// <param name="min">Минимальное число (левая граница)</param>
    /// <param name="max">Максимальное число (правая граница)</param>
    /// <param name="n">Количество случайных чисел, которые необходимо суммировать для достижения нормального распределения</param>
    /// <returns>Случайное нормально распределённое число</returns>
    private static double GetNormalRandom(double min, double max, int n = 12)
    {
        var rnd = new Random(Guid.NewGuid().GetHashCode());
        var sum = 0d;
        for (var i = 0; i < n; i++)
            sum += rnd.NextDouble() * (max - min) + min;
        return sum / n;
    }

    /// <summary>
    /// Генерация отнормированного белого шума.
    /// </summary>
    /// <param name="countNumbers">Число отсчётов</param>
    /// <param name="energySignal">Энергия сигнала, на который накладывается шум</param>
    /// <param name="snrDb">Уровень шума в дБ</param>
    /// <returns></returns>
    private static IEnumerable<Complex> GenerateNoise(int countNumbers, double energySignal, double snrDb)
    {
        var noise = new List<double>();
        for (var i = 0; i < countNumbers; i++)
            noise.Add(GetNormalRandom(-1d, 1d));

        // Нормировка шума.
        var snr = Math.Pow(10, -snrDb / 10);
        var norm = Math.Sqrt(snr * energySignal / noise.Sum(y => y * y));

        return noise.Select(y => new Complex(y * norm, 0)).ToList();
    }

    /// <summary>
    /// Генерация битовой последовательности.
    /// </summary>
    /// <param name="countBits">кол-во битов последовательности</param>
    /// <returns></returns>
    public static string GenerateBitsSequence(int countBits)
    {
        var rnd = new Random(Guid.NewGuid().GetHashCode());
        var bits = string.Empty;
        for (var i = 0; i < countBits; i++)
            bits += rnd.Next(0, 2);
        return bits;
    }

    /// <summary>
    /// Наложить шум на сигнал.
    /// </summary>
    /// <param name="snrDb"></param>
    /// <returns></returns>
    public void MakeNoise(double snrDb)
    {
        // Наложение шума на искомый сигнал.
        desiredSignal = desiredSignal.Zip(
                GenerateNoise(researchedSignal.Count, researchedSignal.Sum(p => p.Y.Imaginary * p.Y.Imaginary), snrDb),
                (p, n) => new Pair<Complex>(p.X, p.Y + n))
            .ToList();

        // Наложение шума на исследуемый сигнал.
        researchedSignal = researchedSignal.Zip(
                GenerateNoise(researchedSignal.Count, researchedSignal.Sum(p => p.Y.Real * p.Y.Real + p.Y.Imaginary * p.Y.Imaginary), snrDb),
                (p, n) => new Pair<Complex>(p.X, p.Y + n))
            .ToList();
    }
}