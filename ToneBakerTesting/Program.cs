using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToneBaker.PCM;
using ToneBaker.WAV;

namespace ToneBakerTesting {
    class Program {
        static void Main(string[] args) {
            // First round of testing for RIFF streaming.
            Console.WriteLine("Configuring new Audio format: 16kHz, 16-bit depth, 2 channels");
            var wavFormat = new AudioFormat(16000, 16, 2);
            var wavType = WaveGenerator.WaveType.SQUARE;
            Console.WriteLine("Creating a triad chord 3.2s long, with one note extended one second longer than the others.");
            var lowTriad = WaveGenerator.CreateNewWave(wavFormat, wavType, 100.0, 3.2, 220);
            var medTriad = WaveGenerator.CreateNewWave(wavFormat, wavType, 100.0, 4.2, 261.6256);
            var highTriad = WaveGenerator.CreateNewWave(wavFormat, wavType, 100.0, 3.2, 329.6276);
            Console.WriteLine("Mixing the samples for the triad.");
            var chord = PCMAudioTools.MixSamples(wavFormat, 100.0, (10.0, lowTriad), (30.0, medTriad), (20.0, highTriad));
            Console.WriteLine("Changing the audio stream's volume.");
            PCMAudioTools.ChangeVolume(100.0, ref chord); // testing volume change
            Console.WriteLine("Creating mock Emergency Alert System tone: 2.2s @1200Hz");
            var EAS = WaveGenerator.CreateNewWave(wavFormat, wavType, 100.0, 2.2, 1200);
            Console.WriteLine("Creating mock sine wave for CW: 1.0s @780Hz.");
            var CWTone = WaveGenerator.CreateNewWave(wavFormat, wavType, 100.0, 1.0, 680);
            Console.WriteLine("Stringing it all together into one sequence.");
            PCMAudioTools.AppendSamples(ref chord, (100.0, EAS), (20.0, CWTone));
            Console.WriteLine("PLAYING");
            PlaySoundData(ref chord);

            // New sequence of tests...


            // Always hold at the end
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
        private static void PlaySoundData(ref List<PCMSample> audioStream) {
            using(var finalFile = new RiffStream(audioStream)) {
                using(System.IO.MemoryStream m = new System.IO.MemoryStream(finalFile.GetRawWaveStream())) {
                    using(System.Media.SoundPlayer player = new System.Media.SoundPlayer(m)) {
                        player.PlaySync();
                    }
                }
                // Can also write the file from memory to disk...
                //System.IO.File.WriteAllBytes(Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\testingCode.wav", finalFile.GetRawWaveStream());
            }
        }
    }
}
