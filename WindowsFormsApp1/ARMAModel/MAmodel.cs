using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLHApp.ARMAModel
{
    class MAModel
    {
        List<double> data;
        int p;

        public MAModel(List<double> data, int p)
        {
            this.data = data;
            this.p = p;
        }
        public List<List<double>> solveCoeOfMA()
        {
            List<List<double>> vec = new List<List<double>>();
            ARMAMath ar_math = new ARMAMath();
            List<double> maCoe = new List<double>(ar_math.computeMACoe(this.data, this.p));
            vec.Add(maCoe);
            return vec;
        }
    }
}
