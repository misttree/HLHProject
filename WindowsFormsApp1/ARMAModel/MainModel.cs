using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLHApp.ARMAModel
{
    /* ARMA模型：本身具有能够执行多点预测的能力
     * 
     */
    class MainModel
    {

        List<double> dataArray;
        private double p;
        private double q;

        public double Get_p()
        {
            return this.p;
        }

        public double Get_q()
        {
            return this.q;
        }
        public MainModel()
        {
            this.dataArray = new List<double>();
        }

        public void Add(double pn)
        {
            this.dataArray.Add(pn);
            if (this.dataArray.Count > 500)
                this.Pop();
        }

        public void Pop()
        {
            this.dataArray.RemoveAt(0);
        }

        public int Size()
        {
            return this.dataArray.Count;
        }
        public double predict()
        {
            ARIMAModel arima = new ARIMAModel(dataArray);
            int period = 7;
            int modelCnt = 5;
            int cnt = 0;
            List<List<int>> list = new List<List<int>>();
            List<int> tmpPredict = new List<int>(modelCnt);
            for (int i = 0; i < modelCnt; i++)
                tmpPredict.Add(0);
            for (int k = 0; k < modelCnt; ++k)          //控制通过多少组参数进行计算最终的结果
            {
                List<int> bestModel = arima.getARIMAModel(period, list, (k == 0) ? false : true);
                //std::cout+bestModel.size()+std::endl;

                if (bestModel.Count == 0)
                {
                    tmpPredict[k] = (int)dataArray[dataArray.Count - period];
                    cnt++;
                    break;
                }
                else
                {
                    //std::cout+bestModel[0]+bestModel[1]+std::endl;
                    int predictDiff = arima.predictValue(bestModel[0], bestModel[1], period);
                    //std::cout+"fuck"+std::endl;
                    tmpPredict[k] = arima.aftDeal(predictDiff, period);
                    cnt++;
                }
                this.p = bestModel[0];
                this.q = bestModel[1];
                //cout + bestModel[0] + " " + bestModel[1] + endl;  // 输出ARMA模型的选择 
                list.Add(bestModel);
            }

            double sumPredict = 0.0;
            for (int k = 0; k < cnt; ++k)
            {
                sumPredict += ((double)tmpPredict[k]) / (double)cnt;
            }
            return sumPredict;
            /*cout + "end prediction" + endl;*/

        }
    }
}
