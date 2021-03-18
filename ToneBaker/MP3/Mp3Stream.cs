using System;
using System.Collections.Generic;
using ToneBaker.PCM;

namespace ToneBaker.MP3 {
    public sealed class Mp3FileFormat {

    }


    public sealed class Mp3Stream : IDisposable {
        internal bool _disposed = false;
        public Mp3Stream(ref List<PCMSample> rawAudioStream) { }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        internal void Dispose(bool disposing) {
            if(!this._disposed) {
                if(disposing) { /* set members null */ }
                // Indicate the instance has been disposed.
                this._disposed = true;
            }
        }
    }
}
