using System;
using System.Collections.Generic;

namespace ToneBaker.PCM {

    /// <summary>
    /// Provides methods used to generate sequences of PCMSample objects that can be put into a raw audio stream.
    /// </summary>
    /// <see cref="PCMSample"/>
    /// <see cref="AudioFormat"/>
    public static class WaveGenerator {

        /// <summary>
        /// Amount of periods per generated wave for which the signal is attenuated. This is in hopes to
        /// reduce any hard clicks between generated signals.
        /// </summary>
        /// <see cref="CreateNewWave(AudioFormat, WaveType, double, double, double)"/>
        public static int ATTENUATION_CYCLES = 2;

        /// <summary>
        /// Defines multiple different waveforms available for the generator to build.
        /// </summary>
        public enum WaveType { SINE, SAWTOOTH, SQUARE, TRIANGLE, WHITE_NOISE };

        /// <summary>
        /// Generates a new wave-form as an audio stream (list of PCMSample objects).
        /// </summary>
        /// <param name="audioFormat">The format used to sample the waveform.</param>
        /// <param name="wavePattern">The type of wave to generate.</param>
        /// <param name="amplitudePerc">A percentage of the audio format's MaxAmplitude (0 to 100%).</param>
        /// <param name="durationSec">How long (in seconds) the sound will be generated for.</param>
        /// <param name="frequencyHz">The frequency at which the sound should be played.</param>
        /// <returns>A new list of PCMSample objects that represent the sound.</returns>
        /// <see cref="PCMSample"/>
        /// <see cref="AudioFormat"/>
        public static List<PCMSample> CreateNewWave(AudioFormat audioFormat, WaveType wavePattern,
                double amplitudePerc, double durationSec, double frequencyHz) {
            // Cap the amplitude at 100%.
            amplitudePerc = Math.Min(100.0, Math.Abs(amplitudePerc));
            // Samples required to complete one wave period at the given frequency.
            double samplesPerWaveCycle = (double)(audioFormat.SampleRate / frequencyHz);
            // Total amount of samples for the wave duration in its entirety.
            double totalSamples = durationSec * (double)audioFormat.SampleRate;
            // Additional amount of samples to append onto the stream until the final wave/period completes.
            //   This is aimed at keeping sudden adjacent tones from causing hard key clicks when transitioning.
            double extraSamples = samplesPerWaveCycle - (totalSamples % samplesPerWaveCycle);
            double waveAttenuationSamplesCount = (samplesPerWaveCycle * WaveGenerator.ATTENUATION_CYCLES);
            // Short-handing some variables from the audio format as needed...
            int sampleRate = audioFormat.SampleRate;
            // Get the peak amplitude permitted based on the amplitudePerc parameter.
            double peakAmplitude = audioFormat.GetPeakAmplitude(amplitudePerc);
            // Initialize the new stream object.
            var newWaveStream = new List<PCMSample>();
            // Create the wave stream based on the requested wave type.
            var prng = new Random(); //PRNG as needed for noise and dither
            Func<int, double> sampleCalculation = wavePattern switch {
                // f(x) = amplitude * SIN(2pi * x * frequency / sampleRate)
                WaveType.SINE => (currSample =>
                    peakAmplitude * Math.Sin((2*Math.PI * currSample * frequencyHz) / sampleRate)
                ),
                // f(x) = abs(amplitude), when 0 <= x < period/2 ;; f(x) = neg(amplitude), when period/2 <= x < period
                // My implementation factors amplitude out of: f(x) = (({2*[amp/samplesPerPeriod]*x + amp} % [amp*2]) - (amp)
                WaveType.SAWTOOTH => (currSample =>
                    ((peakAmplitude * ((2 * currSample / samplesPerWaveCycle) + 1)) % (peakAmplitude * 2)) - peakAmplitude
                ),
                // f(x) = x, when 0 <= x < pi ;; f(x) = x-2pi, when pi <= x =< 2pi
                WaveType.SQUARE => (currSample =>
                    (currSample % samplesPerWaveCycle) < (samplesPerWaveCycle / 2) ? peakAmplitude : 0 - peakAmplitude
                ),
                // f(x) = [2*amp / pi] * [ arcsin( sin( {2pi * freq}/sampleRate ) * x ) ]
                // Definitely had to look this one up...
                WaveType.TRIANGLE => (currSample => 
                    ((2 * peakAmplitude) / Math.PI) 
                    * Math.Asin(Math.Sin(((2 * Math.PI * frequencyHz) / sampleRate) * currSample))
                ),
                // f(x) = random(x); f(x) constrained to [-MinPeak, MaxPeak]
                WaveType.WHITE_NOISE => (currSample =>
                    (prng.NextDouble()*(audioFormat.MaxSampleValue - audioFormat.MinSampleValue)) + audioFormat.MinSampleValue
                ),
                // If no other enum matched, throw exception
                _ => throw new Exception("CreateNewWave: Invalid wave pattern selection: " + wavePattern.ToString())
            };
            for(int currentSample = 0; currentSample < totalSamples + extraSamples; currentSample++) {
                double sampleValue = sampleCalculation(currentSample);
                if(currentSample < waveAttenuationSamplesCount) {
                    // For the first few wave cycles, attenuate the max amplitude by a
                    //   ratio to gradually introduce the signal.
                    //   Again, this is aimed to help prevent clicking in the resulting sample
                    sampleValue *= (double)((double)currentSample / waveAttenuationSamplesCount);
                } else if(currentSample > ((totalSamples + extraSamples) - waveAttenuationSamplesCount)) {
                    // Likewise, attenuate the end of the signal for the sample.
                    sampleValue *= (double)((double)((totalSamples + extraSamples) - currentSample) / waveAttenuationSamplesCount);
                }
                newWaveStream.Add(new PCMSample(audioFormat, (int)sampleValue));
            }
            // Finally, return the new stream.
            return newWaveStream;
        }

    }

}
