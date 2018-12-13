using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 采用计算网络通讯中的RTT时延的方式来计算时间序列中的异常值
namespace HLHApp
{
    class OutlierRTTCheck
    {
        private double MeansAttribute = 0.125;  // 用于计算均值的参数,设置默认值为0.125 
        private double VarianceAttribute = 0.25;  // 用于计算方差的参数，设置默认值为0.25
        private List<double> ValueList;
        private double Means = 0;
        private double Variance = 0;
        private int Size = 0;  // 用于监控防止Build函数被重复调用，导致计算错误

        public OutlierRTTCheck()
        {
            ValueList = new List<double>();
        }

        private void Build()
        {
            if (Size != ValueList.Count)  // 防止重复调用，计算错误
            {
                Size = ValueList.Count;
                if (Size == 1)
                {
                    Means = ValueList.ElementAt(0);
                    Variance = 0;
                }
                else
                {
                    Means = (1 - MeansAttribute) * Means + MeansAttribute * ValueList.Last();
                    Variance = (1 - VarianceAttribute) * Variance + VarianceAttribute * Math.Abs(Means - ValueList.Last());
                }
            }
        }

         // 返回true代表可以进行下一步处理，不是异常值
         // 返回false代表是异常值，需要进行异常值处理
        
        public bool OutlierCheck(double value)  
        {
            if (value >= MinValue() && value <= MaxValue())
                return true;
            else
                return false;
        }

        public double MinValue()
        {
            return Means - 4 * Variance;
        }

        public double MaxValue()
        {
            return Means + 4 * Variance;
        }

        public List<double> RangeValue()
        {
            List<double> rangeValue = new List<double>();
            rangeValue.Add(MinValue());
            rangeValue.Add(MaxValue());
            return rangeValue;
        }

        public void InsertValue(double value)
        {
            this.ValueList.Add(value);
            Build();
        }

        public double GetMeans()
        {
            return this.Means;
        }

        public double GetVariance()
        {
            return this.Variance;
        }

        public double GetMeansAttribute()
        {
            return this.MeansAttribute;
        }

        public void SetMeansAttribute(double MeansAttribute)
        {
            this.MeansAttribute = MeansAttribute;
        }

        public double GetVarianceAttribute()
        {
            return this.VarianceAttribute;
        }

        public void SetVarianceAttribute(double VarianceAttribute)
        {
            this.VarianceAttribute = VarianceAttribute;
        }
    }
}
