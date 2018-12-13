using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLHApp.ARMAModel
{
    class ARMAMath
    {
        double avgData(List<double> dataArray)
        {
            return this.sumData(dataArray) / dataArray.Count;
        }  // 求数据的平均值

        double sumData(List<double> dataArray)
        {
            double sumData = 0;
            for (int i = 0; i < dataArray.Count; i++)
                sumData += dataArray[i];
            return sumData;
        }  // 求数据和

        double stderrData(List<double> dataArray)
        {
            return Math.Sqrt(this.varerrData(dataArray));
        }  // 进行数据的标准化处理

        double varerrData(List<double> dataArray)
        {
            if (dataArray.Count <= 1)
                return 0.0;
            double variance = 0;
            double avgsumData = this.avgData(dataArray);
            for (int i = 0; i < dataArray.Count; i++)
            {
                dataArray[i] -= avgsumData;
                variance += dataArray[i] * dataArray[i];
            }
            return variance / (dataArray.Count - 1);
        }  // 未知处理步骤

        List<double> autocorData(List<double> dataArray, int order)
        {
            List<double> autoCor = new List<double>();
            List<double> autoCov = new List<double>(this.autocovData(dataArray, order));
            double varData = this.varerrData(dataArray);
            if (varData != 0)
            {
                for (int i = 0; i < order; i++)
                {
                    autoCor[i] = autoCov[i] / varData;
                }
            }
            return autoCor;
        }  // 处理至此函数

        List<double> autocovData(List<double> dataArray, int order)
        {
            List<double> autoCov = new List<double>(order + 1);
            for (int i = 0; i <= order; i++)
            {
                autoCov.Add(0);
            }
            double mu = this.avgData(dataArray);
            for (int i = 0; i <= order; i++)
            {
                autoCov[i] = 0.0;
                for (int j = 0; j < dataArray.Count - i; j++)
                {
                    autoCov[i] += (dataArray[j + i] - mu) * (dataArray[j] - mu);
                }
                autoCov[i] /= (dataArray.Count - i);
            }
            return autoCov;
        }

        double mutalCorr(List<double> dataFir, List<double> dataSec)
        {
            double sumX = 0.0;
            double sumY = 0.0;
            double sumXY = 0.0;
            double sumXSq = 0.0;
            double sumYSq = 0.0;
            int len = 0;
            if (dataFir.Count != dataSec.Count)
                len = (int)Math.Min(dataFir.Count, dataSec.Count);
            else
                len = dataFir.Count;  // 此处在C++处不同

            for (int i = 0; i < len; i++)
            {
                sumX += dataFir[i];
                sumY += dataSec[i];
                sumXY += dataFir[i] * dataSec[i];
                sumXSq += dataFir[i] * dataFir[i];
                sumYSq += dataSec[i] * dataSec[i];
            }

            double numerator = sumXY - sumX * sumY / len;
            double denominator = Math.Sqrt((sumXSq - sumX * sumX / len) * (sumYSq - sumY * sumY / len));

            if (denominator == 0) return 0.0;
            return numerator / denominator;
        }

        double gaussrand0()
        {
            double V1 = 0;
            double V2 = 0;
            double S = 0;
            int phase = 0;
            double X;
            Random random = new Random();
            if (phase == 0)
            {
                do
                {
                    double U1 = (double)random.NextDouble();
                    double U2 = (double)random.NextDouble();

                    V1 = 2 * U1 - 1;
                    V2 = 2 * U2 - 1;
                    S = V1 * V1 + V2 * V2;
                } while (S >= 1 || S == 0);

                X = V1 * Math.Sqrt(-2 * Math.Log(S) / S);
            }
            else
                X = V2 * Math.Sqrt(-2 * Math.Log(S) / S);

            phase = 1 - phase;

            return X;
        }

        public double getModelAIC(List<List<double>> vec, List<double> data, int type)
        {
            int n = data.Count;
            int p = 0;
            int q = 0;
            double tmpAR = 0.0, tmpMA = 0.0;
            double sumErr = 0.0;

            if (type == 1)
            {
                List<double> maCoe = vec[0];
                q = (int)maCoe.Count;
                List<double> errData = new List<double>(q);
                for (int i = 0; i < q; i++)
                    errData.Add(0);
                for (int i = q - 1; i < n; i++)
                {
                    tmpMA = 0.0;
                    for (int j = 1; j < q; j++)
                    {
                        tmpMA += maCoe[j] * errData[j];
                    }
                    for (int j = q - 1; j > 0; j--)
                    {
                        errData[j] = errData[j - 1];
                    }
                    errData[0] = gaussrand0() * Math.Sqrt(maCoe[0]);
                    sumErr += (data[i] - tmpMA) * (data[i] - tmpMA);
                }
                return (n - (q - 1)) * Math.Log(sumErr / (n - (q - 1))) + (q + 1) * 2;
            }
            else if (type == 2)
            {
                List<double> arCoe = vec[0];
                p = (int)arCoe.Count;

                for (int i = p - 1; i < n; ++i)
                {
                    tmpAR = 0.0;
                    for (int j = 0; j < p - 1; ++j)
                    {
                        tmpAR += arCoe[j] * data[i - j - 1];
                    }
                    sumErr += (data[i] - tmpAR) * (data[i] - tmpAR);
                }
                //			return Math.log(sumErr) + (p + 1) * 2 / n;
                return (n - (p - 1)) * Math.Log(sumErr / (n - (p - 1))) + (p + 1) * 2;
            }
            else
            {
                List<double> arCoe = vec[0];
                List<double> maCoe = vec[1];
                p = (int)arCoe.Count;
                q = (int)maCoe.Count;
                List<double> errData = new List<double>(q);
                for (int i = 0; i < q; i++)
                    errData.Add(0);
                for (int i = p - 1; i < n; ++i)
                {
                    tmpAR = 0.0;
                    for (int j = 0; j < p - 1; ++j)
                    {
                        tmpAR += arCoe[j] * data[i - j - 1];
                    }
                    tmpMA = 0.0;
                    for (int j = 1; j < q; ++j)
                    {
                        tmpMA += maCoe[j] * errData[j];
                    }

                    for (int j = q - 1; j > 0; --j)
                    {
                        errData[j] = errData[j - 1];
                    }

                    errData[0] = gaussrand0() * Math.Sqrt(maCoe[0]);

                    sumErr += (data[i] - tmpAR - tmpMA) * (data[i] - tmpAR - tmpMA);
                }
                //			return Math.log(sumErr) + (q + p + 1) * 2 / n;
                return (n - (q + p - 1)) * Math.Log(sumErr / (n - (q + p - 1))) + (p + q) * 2;
            }
        }

        List<List<double>> LevinsonSolve(List<double> garma)
        {
            int order = garma.Count - 1;
            List<List<double>> result = new List<List<double>>(order + 1);  // 此处略有修改

            for (int i = 0; i < order + 1; i++)
            {
                List<double> pn = new List<double>(order + 1);
                for (int j = 0; j <= order; j++)
                {
                    pn.Add(0);
                }
                result.Add(pn);
            }



            List<double> sigmaSq = new List<double>(order + 1);
            for (int i = 0; i <= order; i++)
            {
                sigmaSq.Add(0);
            }

            sigmaSq[0] = garma[0];
            result[1][1] = garma[1] / sigmaSq[0];
            sigmaSq[1] = sigmaSq[0] * (1.0 - result[1][1] * result[1][1]);
            for (int k = 1; k < order; ++k)
            {
                double sumTop = 0.0;
                double sumSub = 0.0;
                for (int j = 1; j <= k; ++j)
                {
                    sumTop += garma[k + 1 - j] * result[k][j];
                    sumSub += garma[j] * result[k][j];
                }
                result[k + 1][k + 1] = (garma[k + 1] - sumTop) / (garma[0] - sumSub);
                for (int j = 1; j <= k; ++j)
                {
                    result[k + 1][j] = result[k][j] - result[k + 1][k + 1] * result[k][k + 1 - j];
                }
                sigmaSq[k + 1] = sigmaSq[k] * (1.0 - result[k + 1][k + 1] * result[k + 1][k + 1]);
            }
            result[0] = sigmaSq;

            return result;
        }
        public List<double> computeARCoe(List<double> dataArray, int p)
        {
            List<double> garma = this.autocovData(dataArray, p);

            List<List<double>> result = new List<List<double>>(this.LevinsonSolve(garma));

            List<double> ARCoe = new List<double>(p + 1);
            for (int i = 0; i <= p; i++)
            {
                ARCoe.Add(0);
            }
            for (int i = 0; i < p; i++)
            {
                ARCoe[i] = result[p][i + 1];

            }
            ARCoe[p] = result[0][p];
            return ARCoe;
        }
        public List<double> computeMACoe(List<double> dataArray, int q)
        {
            int p = (int)Math.Log(dataArray.Count);

            //		System.out.println("The best p is " + p);
            // 求取系数
            List<double> bestGarma = new List<double>(this.autocovData(dataArray, p));
            List<List<double>> bestResult = new List<List<double>>(this.LevinsonSolve(bestGarma));

            List<double> alpha = new List<double>(p + 1);
            for (int j = 0; j <= p; j++)
            {
                alpha.Add(0);
            }
            alpha[0] = -1;
            for (int i = 1; i <= p; ++i)
            {
                alpha[i] = bestResult[p][i];
            }

            List<double> paraGarma = new List<double>(q + 1);
            for (int i = 0; i <= q; i++)
                paraGarma.Add(0);
            for (int k = 0; k <= q; ++k)
            {
                double sum = 0.0;
                for (int j = 0; j <= p - k; ++j)
                {
                    sum += alpha[j] * alpha[k + j];
                }
                paraGarma[k] = sum / bestResult[0][p];
            }

            List<List<double>> tmp = new List<List<double>>(this.LevinsonSolve(paraGarma));
            List<double> MACoe = new List<double>(q + 1);
            for (int i = 1; i <= q; i++)
            {
                MACoe.Add(0);
            }
            for (int i = 1; i < MACoe.Count; ++i)
            {
                MACoe[i] = -tmp[q][i];
            }
            MACoe[0] = 1 / tmp[0][q];       //噪声参数

            return MACoe;
        }
        public List<double> computeARMACoe(List<double> dataArray, int p, int q)
        {
            List<double> allGarma = new List<double>(this.autocovData(dataArray, p + q));
            List<double> garma = new List<double>(p + 1);
            for (int i = 0; i <= p; i++)
                garma.Add(0);
            for (int i = 0; i < garma.Count; ++i)
            {
                garma[i] = allGarma[q + i];
            }
            List<List<double>> arResult = new List<List<double>>(this.LevinsonSolve(garma));

            // AR
            List<double> ARCoe = new List<double>(p + 1);
            for (int i = 0; i <= p; i++)
            {
                ARCoe.Add(0);
            }
            for (int i = 0; i < p; ++i)
            {
                ARCoe[i] = arResult[p][i + 1];
            }
            ARCoe[p] = arResult[0][p];
            //		double [] ARCoe = this.YWSolve(garma);

            // MA
            List<double> alpha = new List<double>(p + 1);
            for (int i = 0; i <= p; i++)
                alpha.Add(0);
            alpha[0] = -1;
            for (int i = 1; i <= p; ++i)
            {
                alpha[i] = ARCoe[i - 1];
            }

            List<double> paraGarma = new List<double>(q + 1);
            for (int i = 0; i <= q; i++)
                paraGarma.Add(0);
            for (int k = 0; k <= q; ++k)
            {
                double sum = 0.0;
                for (int i = 0; i <= p; ++i)
                {
                    for (int j = 0; j <= p; ++j)
                    {
                        sum += alpha[i] * alpha[j] * allGarma[Math.Abs(k + i - j)];
                    }
                }
                paraGarma[k] = sum;
            }
            List<List<double>> maResult = new List<List<double>>(this.LevinsonSolve(paraGarma));
            List<double> MACoe = new List<double>(q + 1);
            for (int i = 0; i <= q; i++)
                MACoe.Add(0);
            for (int i = 1; i <= q; ++i)
            {
                MACoe[i] = maResult[q][i];
            }
            MACoe[0] = maResult[0][q];

            //		double [] tmp = this.YWSolve(paraGarma);
            //		double [] MACoe = new double[q + 1];
            //		System.arraycopy(tmp, 0, MACoe, 1, tmp.length - 1);
            //		MACoe[0] = tmp[tmp.length - 1];

            List<double> ARMACoe = new List<double>(p + q + 2);
            for (int i = 0; i < p + q + 2; i++)
                ARMACoe.Add(0);
            for (int i = 0; i < ARMACoe.Count; ++i)
            {
                if (i < ARCoe.Count)
                {
                    ARMACoe[i] = ARCoe[i];
                }
                else
                {
                    ARMACoe[i] = MACoe[i - ARCoe.Count];
                }
            }
            return ARMACoe;
        }
    }
}
