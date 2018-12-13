using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 用于异常值检测的类
// 采用正态分布的3σ原则进行检测处理

namespace HLHApp
{
    class OutlierCheck
    {
        private List<double> Values;
        public int CheckSize = 500;
        public double Mean = 0;
        public double Variance = 0;

        public OutlierCheck()
        {
            Values = new List<double>();
        }

        public void InsertValue(double value)
        {
            Values.Add(value);
            if (Values.Count > CheckSize)
                Values.RemoveAt(0);  // 数据量较大时，去除位于尾部的数据
        }

        public double BuildMean()
        {
            double sum = 0;
            foreach (double a in Values)
                sum += a;
            Mean = sum / Values.Count;
            return Mean;
        }

        public double BuildVariance()
        {
            BuildMean();
            Variance = 0;
            foreach (double a in Values)
            {
                Variance += Math.Pow(Math.Abs(Mean - a), 2);
            }
            return Variance;
        }

        public double MinValue()
        {
            BuildMean();
            BuildVariance();
            return Mean - 3 * Math.Sqrt(Variance);
        }

        public double MaxValue()
        {
            BuildMean();
            BuildVariance();
            return Mean + 3 * Math.Sqrt(Variance);
        }

        public List<double> RangeValue()
        {
            List<double> rangeValue = new List<double>();
            rangeValue.Add(MinValue());
            rangeValue.Add(MaxValue());
            return rangeValue;
        }
    }
}
