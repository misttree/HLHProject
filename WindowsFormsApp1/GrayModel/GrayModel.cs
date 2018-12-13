using MeasureM;
using System;
using System.Collections.Generic;
namespace HLHApp
{
    /* 灰色系统预测模型
     * 
     */
    class GrayModel
    {
        private double a0, a1, a2;  // 灰度值参数
        private int size;  // 标记数据长度
        private int data_size;
        private double error;
        private List<Double> data;  // 存储数据区域
        private List<Double> pre_data;  // 存储数据区域
        private DateTime updatetime;
        public GrayModel()
        {
            data = new List<double>();
            pre_data = new List<double>();
            data_size = 30;  // 默认用于预测的数据集的大小为20
        }

        public void SetSize(int size)  // 设置用于预测的数据集的大小
        {
            this.data_size = size;
        }

        public double Geta0()
        {
            return a0;
        }

        public double Geta1()
        {
            return a1;
        }

        public double Geta2()
        {
            return a2;
        }
        public int GetSize()
        {
            return this.data.Count;
        }

        public void InsertData(double value)  // 向用于预测的数据集中插入数据
        {
            data.Add(value);
            if (data.Count > data_size)
                data.RemoveAt(0);
        }

        public void InsertData(MeasureModel mm)
        {
            data.Add(mm.Value);
            if (data.Count > data_size)
                data.RemoveAt(0);
            updatetime = mm.DateTime;
        }
        public void build()  // 运行模型
        {
            double[] x0 = data.ToArray();
            size = x0.Length;
            double[] x1 = new double[size];  // size为数据区域的长度
            x1[0] = x0[0];
            for (int i = 1; i < size; i++)
            {
                x1[i] = x0[i] + x1[i - 1];  // 执行一步累加的计算x1[k]=x0[0]+x0[1]+....x0[k]
            }
            double[,] b = new double[size - 1, 2];
            double[,] bt = new double[2, size - 1];
            double[,] y = new double[size - 1, 1];
            for (int i = 0; i < b.GetLength(0); i++)
            {
                b[i, 0] = -(x1[i] + x1[i + 1]) / 2;
                b[i, 1] = 1;
                bt[0, i] = b[i, 0];
                bt[1, i] = 1;
                y[i, 0] = x0[i + 1];
            }
            double[,] t = new double[2, 2];
            multiply(bt, b, t);  // multiply函数处理
            t = inverse(t);  // inverse函数处理
            double[,] t1 = new double[2, size - 1];
            multiply(t, bt, t1);
            double[,] t2 = new double[2, 1];
            multiply(t1, y, t2);
            a0 = t2[0, 0];
            double u = t2[1, 0];
            a2 = u / a0;
            a1 = x0[0] - a2;
            a0 = -a0;

            error = 0;
            for (int i = 0; i < x0.Length; i++)
            {
                double d = (x0[i] - getX0(i));
                error += d * d;
            }
            error /= x0.Length;
        }

        /// <summary>
        /// 误差
        /// </summary>
        /// <returns></returns>
        public double getError()
        {
            return error;
        }

        double getX1(int k)
        {
            return a1 * Math.Exp(a0 * k) + a2;  // 在此处进行数据的指数化处理
        }

        double getX0(int k)
        {
            // return a0 * a1 * Math.exp(a0 * k);  
            if (k == 0)
                return a1 * Math.Exp(a0 * k) + a2;  // 即a1+a2
            else
                return a1 * (Math.Exp(a0 * k) - Math.Exp(a0 * (k - 1)));  // 在此处输出幂指数的结果
        }

        /// <summary>
        /// 预测后续的值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double nextValue(int index)
        {
            if (index < 0)
                throw new Exception("超出索引范围");
            return getX0(size + index);
        }

        /// <summary>
        /// 预测下一个值
        /// </summary>
        /// <returns></returns>
        public double nextValue()
        {
            return nextValue(0);
        }

        public List<double> PredictRange(int size)  // 用于返回所预测的一段时间的数据
        {
            List<double> predict = new List<double>();
            double temp = 0;
            for (int i = 1; i <= size; i++)
            {
                build();
                temp = nextValue();
                this.data.Add(temp);
                predict.Add(temp);
            }
            data.RemoveRange(data_size, (data.Count - data_size));  // 清空数据集
            return predict;
        }

        public List<MeasureModel> PredictMeaseureModelRange(int size)  // 用于返回所预测的一段时间的数据
        {
            List<MeasureModel> predict = new List<MeasureModel>();
            for (int i = 1; i <= size; i++)
            {
                MeasureModel mm = new MeasureModel();
                build();
                mm.Value = nextValue();
                mm.DateTime = updatetime.AddSeconds(i);
                this.data.Add(mm.Value);
                predict.Add(mm);
            }
            data.RemoveRange(data_size, (data.Count - data_size));  // 清空数据集
            return predict;
        }

        public double PredictRange()  // 用于返回所预测的下一个数据
        {
            build();
            return nextValue();
        }

        static double[,] inverse(double[,] t)  // 将传入的二阶矩阵进行对角化处理
        {
            double[,] a = new double[2, 2];
            double det = t[0, 0] * t[1, 1] - t[0, 1] * t[1, 0];  // 求相应矩阵的行列式
            a[0, 0] = t[1, 1] / det;
            a[0, 1] = -t[1, 0] / det;
            a[1, 0] = -t[0, 1] / det;
            a[1, 1] = t[0, 0] / det;
            return a;
        }

        static void multiply(double[,] left, double[,] right, double[,] dest)
        {
            int n1 = left.GetLength(0);
            int m1 = left.GetLength(1);
            int m2 = right.GetLength(1);
            for (int k = 0; k < n1; k++)
            {
                for (int s = 0; s < m2; s++)
                {
                    dest[k, s] = 0;
                    for (int i = 0; i < m1; i++)
                    {
                        dest[k, s] += left[k, i] * right[i, s];
                    }
                }
            }
        }
    }
}
