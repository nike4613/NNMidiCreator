using SharpLearning.Neural;
using SharpLearning.Neural.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NNMidiCreator
{
    public class NNLearner
    {

        public static NeuralNet GetNNForm()
        {
            var nnf = new NeuralNet();

            nnf.Add(new InputLayer(1));

            return nnf;
        }

    }
}
