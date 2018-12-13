using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLHApp.ARMAModel
{
    class ARModel
    {
        List<double> data;
        int p;


        public ARModel(List<double> data, int p)
        {
            this.data = new List<double>();
            foreach (double pn in data)
            {
                this.data.Add(pn);
            }
            this.p = p;
        }

        public List<List<double>> solveCoeOfAR()
        {
            List<List<double>> vec = new List<List<double>>();
            ARMAMath ar_math = new ARMAMath();
            List<double> arCoe = new List<double>(ar_math.computeARCoe(this.data, this.p));
            vec.Add(arCoe);
            return vec;
        }
    }
}
