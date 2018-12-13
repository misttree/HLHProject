using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLHApp.ARMAModel
{
    class ARMAModel
    {

        List<double> data;
        int p;
        int q;


        public ARMAModel(List<double> data, int p, int q)
        {
            this.data = data;
            this.p = p;
            this.q = q;
        }

        public List<List<double>> solveCoeOfARMA()
        {
            List<List<double>> vec = new List<List<double>>();
            ARMAMath ar_math = new ARMAMath();

            List<double> armaCoe = new List<double>(ar_math.computeARMACoe(this.data, p, q));
            List<double> arCoe = new List<double>(this.p + 1);
            List<double> maCoe = new List<double>(this.q + 1);
            for (int i = 0; i <= this.p; i++)
                arCoe.Add(0);
            for (int i = 0; i <= this.q; i++)
                maCoe.Add(0);
            for (int i = 0; i < arCoe.Count; i++)
                arCoe[i] = armaCoe[i];
            for (int i = 0; i < maCoe.Count; i++)
                maCoe[i] = armaCoe[i + this.p + 1];
            vec.Add(arCoe);
            vec.Add(maCoe);

            return vec;
        }
    }
}
