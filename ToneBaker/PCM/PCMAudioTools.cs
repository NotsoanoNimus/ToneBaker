﻿namespace ToneBaker.PCM;

using System;
using System.Collections.Generic;

/// <summary>
/// A class that provides static methods for manipulating PCM-encoded audio data.
/// </summary>
public static class PCMAudioTools
{
    /// <summary>
    /// Allows for quick initialization of an empty audio stream when creating and modifying audio from scratch.
    /// </summary>
    /// <param name="audioFormat">The format of the new audio stream to work with.</param>
    /// <returns>A "List" object of PCMSample objects containing only a single, empty sample.</returns>
    /// <see cref="PCMSample"/>
    public static List<PCMSample> InitializeStream(AudioFormat audioFormat)
    {
        return new List<PCMSample>(new [] { new PCMSample(audioFormat, 0) });
    }

    /// <summary>
    /// Mixes different audio streams, represented as lists of PCMSample objects and their corresponding amplitude weightings, into an overall stream of sound.
    /// The resulting stream duration will equal the longest stream in the "samples" list, so varying stream durations can be easily mixed together; simply note that
    /// the final stream starts all component streams at the "0" duration mark.
    /// </summary>
    /// <param name="audioFormat">The format used for aggregating all stream data. This MUST be the same format used across all streams -- any stream NOT matching the provided format will be DISCARDED from the final result.</param>
    /// <param name="peakOutputAmplitudePercentage">A scaling percentage for the final stream's peak output amplitude. Defaults to 100%, which represents no change in the mixed stream's volume.</param>
    /// <param name="componentStreamCollection">Any amount of tuples consisting of the amplitude (volume) weighting within all mixed samples, and the audio sample itself. NOTE: The weighting is NOT a percentage value!</param>
    /// <returns>An audio stream (stream of samples) equal to the combination of all provided component streams at their given amplitude weightings.</returns>
    /// <see cref="PCMSample"/>
    /// <seealso cref="InterlaceSamples(AudioFormat, ref List{PCMSample}, (double startTimeSeconds, double amplitudePercentage, List{PCMSample} audioStream)[])"/>
    public static List<PCMSample> MixSamples(
        AudioFormat audioFormat,
        double peakOutputAmplitudePercentage = 100.0,
        params (double, List<PCMSample>)[] componentStreamCollection)
    {
        // Cap the peak audio amplitude at 100%.
        peakOutputAmplitudePercentage = Math.Min(Math.Abs(peakOutputAmplitudePercentage), 100.0);
        
        // Create some tracking local variables.
        var longestSample = 0; //the longest component stream length
        
        // The combined weights of all provided component streams (used for weight ratio calculations)
        var totalAmplitudeWeights = 0.0;
        
        // Filter all component streams for (1) empty lists, and (2) bad formatting. And while iterating (if the object isn't skipped), get stats.
        var filteredComponentStreams = new List<(double, List<PCMSample>)>();

        foreach (var (amplitudeWeight, componentStream) in componentStreamCollection)
        {
            if (
                componentStream.Count < 1
                || amplitudeWeight == 0.0
                || componentStream[0].SampleFormat != audioFormat)
            {
                continue;
            }

            filteredComponentStreams.Add((amplitudeWeight, componentStream));
            
            // Mark the amplitude weighting and get the longest sample.
            totalAmplitudeWeights += amplitudeWeight;
            
            longestSample = Math.Max(componentStream.Count, longestSample);
        }

        // Ready the output samples stream and initialize all values to 0.
        var finalStream = new PCMSample[longestSample];
        for (var i = 0; i < longestSample; i++)
        {
            finalStream[i] = new PCMSample(audioFormat, 0);
        }

        // If the total amplitude weightings add up to zero for some reason, or no streams are left over after filtering,
        //   return an empty stream at the right sample length.
        if (totalAmplitudeWeights <= 0.0 || filteredComponentStreams.Count <= 0)
        {
            return new List<PCMSample>(finalStream);
        }

        // Iterate each component stream...
        for (var j = 0; j < filteredComponentStreams.Count; j++)
        {
            var (amplitudeWeight, componentStream) = filteredComponentStreams[j];
            
            // Get the ratio of stream weight to total as a percentage.
            var amplitudeRatioPerc = (amplitudeWeight / totalAmplitudeWeights) * 100.0;
            
            // Use that percentage to change the stream's volume (will always be < 100% for multiple component streams).
            ChangeVolume(amplitudeRatioPerc, ref componentStream);
            
            // For each sample in the stream, use the PCMSample "CombineWaveSamples" method to add onto the finalStream's value.
            for (var k = 0; k < componentStream.Count; k++)
            {
                finalStream[k] = PCMSample.CombineWaveSamples(audioFormat, finalStream[k], componentStream[k]);
            }
        }

        // Convert the finalStream into a list object.
        var finalStreamAsList = new List<PCMSample>(finalStream);
        
        // Adjust the finalStream object's samples to the final volume scaling.
        ChangeVolume(peakOutputAmplitudePercentage, ref finalStreamAsList);
        
        // Finally, return the completed stream.
        return finalStreamAsList;
    }

    /// <summary>
    /// Used to combine multiple PCM audio streams with varying volumes and starting locations, into a single resulting PCM stream at the given final amplitude percentage.
    /// </summary>
    /// <param name="audioFormat">The format all given streams are REQUIRED to use. If a component stream does NOT use this format, an exception will be raised.</param>
    /// <param name="peakOutputAmplitudePercentage">The max amplitude percentage (i.e. volume) of the final, interlaced sample. Defaults to 100%.</param>
    /// <param name="componentStreams">All streams to be interlaced; composed of (1) the start time in seconds into the resulting audio stream, (2) the final volume percentage of the component stream, and (3) the component PCM stream itself.</param>
    /// <returns>A single PCM stream composed of all component streams at their specified starting locations.</returns>
    public static List<PCMSample> InterlaceSamples(
        AudioFormat audioFormat,
        double peakOutputAmplitudePercentage = 100.0,
        params (double startTimeSeconds, double amplitudePercentage, List<PCMSample> audioStream)[] componentStreams
    )
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Converts a stream of PCMSample objects into a raw byte array.
    /// </summary>
    /// <param name="audioStream">The stream to convert.</param>
    /// <returns>A raw byte array representing the provided PCMSample stream.</returns>
    public static byte[] ToByteArray(ref PCMSample[] audioStream)
    {
        var returnStream = new List<byte>();
        
        foreach (var sample in audioStream)
        {
            returnStream.AddRange(sample.Sample);
        }

        return returnStream.ToArray();
    }

    /// <summary>
    /// Create and return an audio stream of the specified duration that has no sound.
    /// </summary>
    /// <param name="audioFormat">The format in which the silence should be created.</param>
    /// <param name="spaceDurationSec">The amount of time in seconds for which to create total silence.</param>
    /// <returns>Zeroed set of PCMSample objects that will not make any sound if played.</returns>
    /// <see cref="PCMSample"/>
    public static List<PCMSample> CreateEmptySpace(AudioFormat audioFormat, double spaceDurationSec)
    {
        var totalSamples = Math.Ceiling(spaceDurationSec * audioFormat.SampleRate);
        
        var silentAudioStream = new PCMSample[(int)totalSamples];
        
        for (var i = 0; i < (int)totalSamples; i++)
        {
            silentAudioStream[i] = new PCMSample(audioFormat, 0);
        }

        return new List<PCMSample>(silentAudioStream);
    }

    /// <summary>
    /// Changes the volume for a referenced audio stream based on the provided percentage. This is NOT multiplicative: the percentage
    /// provided in the parameter is based on 0 to 100% of the POSSIBLE volume of the audio format.
    /// </summary>
    /// <param name="newAmplitudePerc">The new amplitude percentage, between 0 and 100%.</param>
    /// <param name="audioStream">A pointer to an audio stream whose values should be altered to match the new amplitude scaling.</param>
    public static void ChangeVolume(double newAmplitudePerc, ref List<PCMSample> audioStream)
    {
        if (audioStream?.Count <= 0)
        {
            throw new Exception("ChangeVolume: The stream to modify must have at least one sample.");
        }

        var audioFormat = audioStream[0].SampleFormat;
        var maxAmplitude = audioFormat.GetPeakAmplitude();
        var minAmplitude = 0 - maxAmplitude - 1; //min peak is always negated positive peak, minus 1
        var newAmplitudeRatio = (0.01 * Math.Min(100.0, Math.Abs(newAmplitudePerc))); //from % to ratio
        foreach (var sample in audioStream)
        {
            // Change the amplitude per-channel.
            var channelValues = sample.GetAllChannelValues();
            
            for (var channel = 0; channel < channelValues.Length; channel++)
            {
                var newSampleValueForChannel = (int)Math.Floor(channelValues[channel] * newAmplitudeRatio);
                
                // Constrain the value between the peak amplitudes.
                newSampleValueForChannel =
                    (int)Math.Min(maxAmplitude, Math.Max(minAmplitude, newSampleValueForChannel));
                
                sample.SetChannelValue(channel, ref newSampleValueForChannel);
            }
        }
    }

    /// <summary>
    /// Appends streams onto the referenced "base" stream; does not mix streams.
    /// </summary>
    /// <param name="streamToExtend">Pointer to a stream of samples to extend.</param>
    /// <param name="appendedStreams">An open-ended list of streams to add. The list is appended from index 0 upwards, meaning the first stream in the params will be the first stream appended.</param>
    public static void AppendSamples(
        ref List<PCMSample> streamToExtend,
        params (double?, List<PCMSample>)[] appendedStreams)
    {
        if (streamToExtend?.Count <= 0)
        {
            throw new Exception("AppendSamples: The stream to extend must have at least one sample in the dataset.");
        }

        for (var i = 0; i < appendedStreams?.Length; i++)
        {
            // Get the stream.
            var (newVolumePercentage, appendMe) = appendedStreams[i];

            var volAdj = Math.Min(Math.Abs(newVolumePercentage ?? 100.0), 100.0);
            
            if (appendMe.Count <= 0)
            {
                continue;
            }
            
            if (appendMe[0].SampleFormat != streamToExtend?[0].SampleFormat)
            {
                continue;
            }

            // Set the volume (unchanged if the % provided is missing/null).
            ChangeVolume(volAdj, ref appendMe);
            
            // Append the samples onto the original stream being extended.
            streamToExtend?.AddRange(appendMe);
        }
    }

    /// <summary>
    /// Crops the provided audio stream (by reference) to the specified duration. This operation is intended to be SUBTRACTIVE ONLY
    /// and will not extend silence onto the ends of audio streams if the newDuration parameter is greater than the length of the stream.
    /// </summary>
    /// <param name="audioStream"></param>
    /// <param name="newDuration"></param>
    /// <seealso cref="CreateEmptySpace(AudioFormat, double)"/>
    public static void CropStream(ref List<PCMSample> audioStream, double newDuration)
    {
        if (audioStream.Count <= 0)
        {
            throw new ArgumentException(
                "CropStream: the audioStream reference must point to a stream that contains at least one sample.");
        }
        
        if (newDuration <= 0.0)
        {
            throw new ArgumentException(
                "CropStream: the requested new duration cannot be equal to or less than 0 seconds.");
        }

        var fmt = audioStream[0].SampleFormat;
        
        // stream in sec = currSamples / [(samples/sec) * numChannels]
        var streamCurrentDuration = audioStream.Count / (double)(fmt.SampleRate * fmt.ChannelCount);
        
        // Do nothing if requested duration is > current
        if (streamCurrentDuration <= newDuration)
        {
            return;
        }

        var newDurationSampleCount = (int)(newDuration * fmt.SampleRate * fmt.ChannelCount);
        
        audioStream.RemoveRange(newDurationSampleCount, audioStream.Count);
    }

    /// <summary>
    /// Normalizes the peak amplitudes within the referenced audio stream.
    /// </summary>
    /// <param name="audioFormat">The format to use for normalization.</param>
    /// <param name="audioStream"></param>
    public static void NormalizePeaks(ref List<PCMSample> audioStream)
    {
        throw new NotImplementedException();
    }
}
