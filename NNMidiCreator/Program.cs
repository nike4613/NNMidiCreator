using MidiParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NNMidiCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Stream strm = File.Open("paradise-city.mid", FileMode.Open);

            var midi = new MidiFile(strm);

            midi.Load();

            Console.WriteLine(midi);

            File.WriteAllText("tostring.txt",midi.ToString(true));
        }
    }
}
