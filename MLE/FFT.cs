using System;
using System.Numerics;

namespace Wiener_Filtering
{
    /// <summary>
    /// Быстрое преобразование Фурье.
    /// </summary>
    public static class FFTClass
    {
        /// <summary>
        /// Быстрое преобразование Фурье. Реализация рекурсивного алгоритма Кули-Тьюки.
        /// </summary>
        /// <param name="signal">Входной сигнал в Complex для преобразования в его частотный спектр</param>
        /// <param name="forward">Должен ли он быть прямым или обратным преобразованием Фурье</param>
        private static void FFT(ref Complex[] signal, bool forward)
        {
            var n = signal.Length;
            if (n < 2)
                return;
            var a0 = new Complex[n / 2];
            var a1 = new Complex[n / 2];
            for (int i = 0, j = 0; i < n; i += 2, ++j)
            {
                a0[j] = signal[i];
                a1[j] = signal[i + 1];
            }
            FFT(ref a0, forward);
            FFT(ref a1, forward);
            var ang = 2 * Math.PI / n * (forward ? 1 : -1);
            var w = new Complex(1, 0);
            var wn = new Complex(Math.Cos(ang), Math.Sin(ang));
            for (int i = 0; i < n / 2; ++i)
            {
                signal[i] = a0[i] + w * a1[i];
                signal[i + n / 2] = a0[i] - w * a1[i];
                if (!forward)
                {
                    signal[i] /= 2;
                    signal[i + n / 2] /= 2;
                }
                w *= wn;
            }
        }
        /// <summary>
        /// Реализация алгоритма БПФ Кули-Тьюки.
        /// </summary>
        /// <param name="signal">Входной сигнал в Complex для преобразования в его частотный спектр</param>
        /// <param name="forward">Должен ли он быть прямым или обратным преобразованием Фурье</param>
        public static Complex[] FFT(Complex[] signal, bool forward = true)
        {
            var spectrum = new Complex[signal.Length];
            signal.CopyTo(spectrum, 0);
            FFT(ref spectrum, forward);
            return spectrum;
        }
    }
}