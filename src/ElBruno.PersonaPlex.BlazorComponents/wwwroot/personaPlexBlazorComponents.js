const microphoneState = {
    stream: null,
    audioContext: null,
    sourceNode: null,
    analyserNode: null,
    processorNode: null,
    silentGainNode: null,
    buffers: [],
    isRecording: false
};

const waveformState = new WeakMap();

export async function startRecording() {
    await ensureMicrophoneCapture();

    if (microphoneState.audioContext.state === "suspended") {
        await microphoneState.audioContext.resume();
    }

    microphoneState.buffers = [];
    microphoneState.isRecording = true;
}

export async function stopRecording() {
    if (!microphoneState.audioContext || !microphoneState.isRecording) {
        return "";
    }

    microphoneState.isRecording = false;
    const mergedSamples = mergeBuffers(microphoneState.buffers);
    const resampled = resampleTo24kHz(mergedSamples, microphoneState.audioContext.sampleRate);
    return encodeWavDataUrl(resampled, 24000);
}

export async function endMicrophoneCapture() {
    if (microphoneState.stream) {
        microphoneState.stream.getTracks().forEach(track => track.stop());
    }

    if (microphoneState.processorNode) {
        microphoneState.processorNode.disconnect();
    }

    if (microphoneState.silentGainNode) {
        microphoneState.silentGainNode.disconnect();
    }

    if (microphoneState.sourceNode) {
        microphoneState.sourceNode.disconnect();
    }

    if (microphoneState.analyserNode) {
        microphoneState.analyserNode.disconnect();
    }

    if (microphoneState.audioContext) {
        await microphoneState.audioContext.close();
    }

    microphoneState.stream = null;
    microphoneState.audioContext = null;
    microphoneState.sourceNode = null;
    microphoneState.analyserNode = null;
    microphoneState.processorNode = null;
    microphoneState.silentGainNode = null;
    microphoneState.buffers = [];
    microphoneState.isRecording = false;
}

export async function attachMicrophoneWaveform(canvas, options) {
    await ensureMicrophoneCapture();
    detachWaveform(canvas);

    const context = canvas.getContext("2d");
    const analyser = microphoneState.analyserNode;
    const data = new Uint8Array(analyser.fftSize);
    const color = options?.color ?? "#0d6efd";
    const barWidth = Number(options?.barWidth ?? 3);

    const render = () => {
        const { width, height } = resizeCanvas(canvas);
        context.clearRect(0, 0, width, height);

        analyser.getByteTimeDomainData(data);
        context.fillStyle = color;

        const step = Math.max(1, Math.floor(data.length / Math.max(1, Math.floor(width / (barWidth * 1.5)))));
        let x = 0;
        for (let index = 0; index < data.length && x < width; index += step) {
            const amplitude = Math.abs((data[index] - 128) / 128);
            const barHeight = Math.max(4, amplitude * height);
            const y = (height - barHeight) / 2;
            context.fillRect(x, y, barWidth, barHeight);
            x += barWidth + 2;
        }

        const handle = requestAnimationFrame(render);
        waveformState.set(canvas, handle);
    };

    render();
}

export async function renderPlaybackWaveform(canvas, audioDataUrl, options) {
    detachWaveform(canvas);
    const context = canvas.getContext("2d");
    const { width, height } = resizeCanvas(canvas);
    context.clearRect(0, 0, width, height);

    if (!audioDataUrl) {
        return;
    }

    const audioContext = new AudioContext();
    try {
        const response = await fetch(audioDataUrl);
        const buffer = await response.arrayBuffer();
        const audioBuffer = await audioContext.decodeAudioData(buffer.slice(0));
        const samples = audioBuffer.getChannelData(0);
        const color = options?.color ?? "#0d6efd";
        const barWidth = Number(options?.barWidth ?? 3);
        const step = Math.max(1, Math.floor(samples.length / Math.max(1, Math.floor(width / (barWidth * 1.5)))));

        context.fillStyle = color;
        let x = 0;
        for (let index = 0; index < samples.length && x < width; index += step) {
            const amplitude = Math.abs(samples[index]);
            const barHeight = Math.max(4, amplitude * height);
            const y = (height - barHeight) / 2;
            context.fillRect(x, y, barWidth, barHeight);
            x += barWidth + 2;
        }
    } finally {
        await audioContext.close();
    }
}

export function detachWaveform(canvas) {
    const handle = waveformState.get(canvas);
    if (handle) {
        cancelAnimationFrame(handle);
        waveformState.delete(canvas);
    }
}

async function ensureMicrophoneCapture() {
    if (microphoneState.stream) {
        return;
    }

    const stream = await navigator.mediaDevices.getUserMedia({
        audio: {
            channelCount: 1,
            echoCancellation: true,
            noiseSuppression: true
        }
    });

    const AudioContextCtor = window.AudioContext || window.webkitAudioContext;
    const audioContext = new AudioContextCtor();
    const sourceNode = audioContext.createMediaStreamSource(stream);
    const analyserNode = audioContext.createAnalyser();
    analyserNode.fftSize = 2048;

    const processorNode = audioContext.createScriptProcessor(4096, 1, 1);
    processorNode.onaudioprocess = event => {
        if (!microphoneState.isRecording) {
            return;
        }

        microphoneState.buffers.push(new Float32Array(event.inputBuffer.getChannelData(0)));
    };

    const silentGainNode = audioContext.createGain();
    silentGainNode.gain.value = 0;

    sourceNode.connect(analyserNode);
    sourceNode.connect(processorNode);
    processorNode.connect(silentGainNode);
    silentGainNode.connect(audioContext.destination);

    microphoneState.stream = stream;
    microphoneState.audioContext = audioContext;
    microphoneState.sourceNode = sourceNode;
    microphoneState.analyserNode = analyserNode;
    microphoneState.processorNode = processorNode;
    microphoneState.silentGainNode = silentGainNode;
}

function resizeCanvas(canvas) {
    const width = canvas.clientWidth || canvas.width || 320;
    const height = canvas.clientHeight || canvas.height || 60;
    if (canvas.width !== width) {
        canvas.width = width;
    }

    if (canvas.height !== height) {
        canvas.height = height;
    }

    return { width, height };
}

function mergeBuffers(buffers) {
    const totalLength = buffers.reduce((sum, buffer) => sum + buffer.length, 0);
    const output = new Float32Array(totalLength);
    let offset = 0;

    for (const buffer of buffers) {
        output.set(buffer, offset);
        offset += buffer.length;
    }

    return output;
}

function resampleTo24kHz(samples, inputRate) {
    if (!samples.length || inputRate === 24000) {
        return samples;
    }

    const outputLength = Math.max(1, Math.round(samples.length * 24000 / inputRate));
    const output = new Float32Array(outputLength);
    const ratio = (samples.length - 1) / Math.max(1, outputLength - 1);

    for (let index = 0; index < outputLength; index++) {
        const position = index * ratio;
        const low = Math.floor(position);
        const high = Math.min(samples.length - 1, low + 1);
        const weight = position - low;
        output[index] = samples[low] * (1 - weight) + samples[high] * weight;
    }

    return output;
}

function encodeWavDataUrl(samples, sampleRate) {
    const wav = encodeWav(samples, sampleRate);
    let binary = "";
    for (let index = 0; index < wav.length; index++) {
        binary += String.fromCharCode(wav[index]);
    }

    return `data:audio/wav;base64,${btoa(binary)}`;
}

function encodeWav(samples, sampleRate) {
    const buffer = new ArrayBuffer(44 + samples.length * 2);
    const view = new DataView(buffer);

    writeString(view, 0, "RIFF");
    view.setUint32(4, 36 + samples.length * 2, true);
    writeString(view, 8, "WAVE");
    writeString(view, 12, "fmt ");
    view.setUint32(16, 16, true);
    view.setUint16(20, 1, true);
    view.setUint16(22, 1, true);
    view.setUint32(24, sampleRate, true);
    view.setUint32(28, sampleRate * 2, true);
    view.setUint16(32, 2, true);
    view.setUint16(34, 16, true);
    writeString(view, 36, "data");
    view.setUint32(40, samples.length * 2, true);

    let offset = 44;
    for (let index = 0; index < samples.length; index++) {
        const sample = Math.max(-1, Math.min(1, samples[index]));
        view.setInt16(offset, sample < 0 ? sample * 0x8000 : sample * 0x7FFF, true);
        offset += 2;
    }

    return new Uint8Array(buffer);
}

function writeString(view, offset, value) {
    for (let index = 0; index < value.length; index++) {
        view.setUint8(offset + index, value.charCodeAt(index));
    }
}
