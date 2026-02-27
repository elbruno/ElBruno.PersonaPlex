// Audio recorder using the browser MediaRecorder API.
// Records mono audio and sends it back to Blazor as a byte array.
window.audioRecorder = {
    mediaRecorder: null,
    audioChunks: [],
    dotNetRef: null,

    start: async function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.audioChunks = [];

        const stream = await navigator.mediaDevices.getUserMedia({
            audio: {
                channelCount: 1,
                sampleRate: 24000,
                echoCancellation: true,
                noiseSuppression: true
            }
        });

        this.mediaRecorder = new MediaRecorder(stream, {
            mimeType: MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
                ? 'audio/webm;codecs=opus'
                : 'audio/webm'
        });

        this.mediaRecorder.ondataavailable = (e) => {
            if (e.data.size > 0) {
                this.audioChunks.push(e.data);
            }
        };

        this.mediaRecorder.onstop = async () => {
            const blob = new Blob(this.audioChunks, { type: 'audio/webm' });
            const buffer = await blob.arrayBuffer();
            const bytes = new Uint8Array(buffer);

            // Stop all tracks to release the microphone
            stream.getTracks().forEach(t => t.stop());

            // Send to Blazor
            if (this.dotNetRef) {
                await this.dotNetRef.invokeMethodAsync('OnAudioRecorded', bytes);
            }
        };

        this.mediaRecorder.start();
    },

    stop: function () {
        if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
            this.mediaRecorder.stop();
        }
    }
};
