using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLHApp.ARMAModel
{
    class ARIMAModel
    {
        List<double> dataArray;
        List<double> dataFirDiff;
        List<List<double>> arima;


        public ARIMAModel(List<double> dataArray)
        {
            this.dataArray = new List<double>();
            this.dataFirDiff = new List<double>();
            this.arima = new List<List<double>>();
            foreach (double pn in dataArray)
            {
                this.dataArray.Add(pn);
            }
        }

        public List<double> preFirDiff(List<double> preData)
        {
            List<double> res = new List<double>();
            for (int i = 0; i < preData.Count - 1; i++)
            {
                double tmpData = preData[i + 1] - preData[i];
                res.Add(tmpData);
            }
            return res;
        }

        public List<double> preSeasonDiff(List<double> preData)
        {
            List<double> res = new List<double>();

            for (int i = 0; i < preData.Count - 7; i++)
            {

                double tmpData = preData[i + 7] - preData[i];
                res.Add(tmpData);
            }
            return res;
        }
        public List<double> preDealDiff(int period)
        {
            if (period >= dataArray.Count - 1)
            {
                period = 0;
            }

            switch (period)
            {
                case 0:
                    {
                        return this.dataArray;
                    }
                    break;
                case 1:
                    {
                        List<double> tmp = new List<double>(this.preFirDiff(this.dataArray));
                        foreach (double pn in tmp)
                        {
                            this.dataFirDiff.Add(pn);
                        }
                        return this.dataFirDiff;
                    }
                    break;
                default:
                    {
                        return preSeasonDiff(dataArray);
                    }
                    break;
            }
        }

        public List<int> getARIMAModel(int period, List<List<int>> notModel, bool needNot)
        {

            List<double> data = this.preDealDiff(period);
            //for(int i=0;i<data.size();i++) std::cout<<data[i]<<std::endl;

            double minAIC = 1.7976931348623157E308;
            List<int> bestModel = new List<int>(3);
            for (int i = 0; i < 3; i++)
                bestModel.Add(0);
            int type = 0;
            List<List<double>> coe = new List<List<double>>();
            // model产生, 即产生相应的p, q参数
            int len = data.Count;

            if (len > 5)
            {
                len = 5;
            }

            int size = ((len + 2) * (len + 1)) / 2 - 1;
            List<List<int>> model = new List<List<int>>(size);
            for (int i = 0; i < size; i++)
            {
                List<int> pn = new List<int>(size);
                for (int j = 0; j < size; j++)
                {
                    pn.Add(0);
                }
                model.Add(pn);

            }
            int cnt = 0;
            for (int i = 0; i <= len; ++i)
            {
                for (int j = 0; j <= len - i; ++j)
                {
                    if (i == 0 && j == 0)
                        continue;
                    model[cnt][0] = i;
                    model[cnt++][1] = j;
                }
            }
            //std::cout<<size<<std::endl;
            for (int i = 0; i < cnt; ++i)
            {
                // 控制选择的参数

                bool token = false;
                if (needNot)
                {
                    for (int k = 0; k < notModel.Count; ++k)
                    {
                        if (model[i][0] == notModel[k][0] && model[i][1] == notModel[k][1])
                        {
                            token = true;
                            break;
                        }
                    }
                }
                if (token)
                {
                    continue;
                }

                if (model[i][0] == 0)
                {
                    MAModel ma = new MAModel(data, model[i][1]);

                    //List<List<double>>
                    coe = ma.solveCoeOfMA();

                    // std::cout<<i<<coe.size()<<std::endl;
                    //for(int ks=0;ks<ma.solveCoeOfMA().size();ks++) tmp.push_back(ma.solveCoeOfMA()[ks]);
                    //coe.assign(tmp.begin(),tmp.end());
                    type = 1;
                }
                else if (model[i][1] == 0)
                {
                    ARModel ar = new ARModel(data, model[i][0]);
                    //List<List<double>> tmp(
                    coe = ar.solveCoeOfAR();

                    //   std::cout<<i<<coe.size()<<std::endl;
                    //for(int ks=0;ks<ar.solveCoeOfAR().size();ks++) tmp.push_back(ar.solveCoeOfAR()[ks]);
                    //coe.assign(tmp.begin(),tmp.end());
                    type = 2;
                }
                else
                {
                    //std::cout<<i<<model[i][0]<<" "<<model[i][1]<<std::endl;
                    ARMAModel arma = new ARMAModel(data, model[i][0], model[i][1]); ;

                    //List<List<double>> tmp(
                    coe = arma.solveCoeOfARMA();

                    //  std::cout<<i<<coe.size()<<std::endl;
                    //for(int ks=0;ks<arma.solveCoeOfARMA().size();ks++) tmp.push_back(arma.solveCoeOfARMA()[ks]);
                    //coe.assign(tmp.begin(),tmp.end());
                    type = 3;
                }
                ARMAMath ar_math = new ARMAMath();
                double aic = ar_math.getModelAIC(coe, data, type);
                //std::cout<<aic<<std::endl;
                // 在求解过程中如果阶数选取过长，可能会出现NAN或者无穷大的情况

                if (aic <= 1.7976931348623157E308 && !Double.IsNaN(aic) && aic < minAIC)
                {
                    minAIC = aic;
                    // std::cout<<aic<<std::endl;
                    bestModel[0] = model[i][0];
                    bestModel[1] = model[i][1];
                    bestModel[2] = (int)Math.Round(minAIC);
                    this.arima = coe;
                }
            }
            return bestModel;
        }

        public int aftDeal(int predictValue, int period)
        {
            if (period >= dataArray.Count)
            {
                period = 0;
            }

            switch (period)
            {
                case 0:
                    return (int)predictValue;
                case 1:
                    return (int)(predictValue + dataArray[dataArray.Count - 1]);
                case 2:
                default:
                    return (int)(predictValue + dataArray[dataArray.Count - 7]);
            }
        }

        public double gaussrand()
        {
            Random re = new Random();
            double V1 = 0;
            double V2 = 0;
            double S = 0;
            int phase = 0;
            double X;

            if (phase == 0)
            {
                do
                {
                    double U1 = (double)re.NextDouble();
                    double U2 = (double)re.NextDouble();

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


        public int predictValue(int p, int q, int period)
        {
            List<double> data = new List<double>(this.preDealDiff(period));
            int n = data.Count;
            int predict = 0;
            double tmpAR = 0.0, tmpMA = 0.0;
            List<double> errData = new List<double>(q + 1);
            for (int i = 0; i <= q; i++)
                errData.Add(0);
            if (p == 0)
            {
                List<double> maCoe = new List<double>(this.arima[0]);
                for (int k = q; k < n; ++k)
                {
                    tmpMA = 0;
                    for (int i = 1; i < q; ++i) // 此处做了更改
                    {
                        tmpMA += maCoe[i] * errData[i];
                    }

                    for (int j = q; j > 0; --j)
                    {
                        errData[j] = errData[j - 1];
                    }
                    errData[0] = gaussrand() * Math.Sqrt(maCoe[0]);
                }

                predict = (int)(tmpMA); //产生预测
            }
            else if (q == 0)
            {
                List<double> arCoe = new List<double>(this.arima[0]);

                for (int k = p; k < n; ++k)
                {
                    tmpAR = 0;
                    for (int i = 0; i < p; ++i)
                    {
                        tmpAR += arCoe[i] * data[k - i - 1];
                    }
                }
                predict = (int)(tmpAR);
            }
            else
            {
                List<double> arCoe = new List<double>(this.arima[0]);
                List<double> maCoe = new List<double>(this.arima[1]);

                for (int k = p; k < n; ++k)
                {
                    tmpAR = 0;
                    tmpMA = 0;
                    for (int i = 0; i < p; ++i)
                    {
                        tmpAR += arCoe[i] * data[k - i - 1];
                    }
                    for (int i = 1; i <= q; ++i)
                    {
                        tmpMA += maCoe[i] * errData[i];
                    }
                    for (int i = q; i > 0; --i)
                    {
                        errData[i] = errData[i - 1];
                    }

                    errData[0] = gaussrand() * Math.Sqrt(maCoe[0]);
                }

                predict = (int)(tmpAR + tmpMA);
            }

            return predict;
        }
    }
}
