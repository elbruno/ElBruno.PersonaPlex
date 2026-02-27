// Voice conversation using the browser Web Speech API.
// SpeechRecognition for STT, SpeechSynthesis for TTS.
window.voiceChat = {
    recognition: null,
    dotNetRef: null,
    isListening: false,
    autoSpeak: true,

    // Check if the browser supports speech recognition
    isSupported: function () {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    },

    // Start listening for speech input
    start: function (dotNetRef) {
        if (!this.isSupported()) {
            dotNetRef.invokeMethodAsync('OnSpeechError',
                'Speech recognition is not supported in this browser. Use Chrome or Edge.');
            return;
        }

        this.dotNetRef = dotNetRef;

        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        this.recognition = new SpeechRecognition();
        this.recognition.continuous = false;
        this.recognition.interimResults = true;
        this.recognition.lang = 'en-US';
        this.recognition.maxAlternatives = 1;

        this.recognition.onstart = () => {
            this.isListening = true;
        };

        this.recognition.onresult = (event) => {
            let interimTranscript = '';
            let finalTranscript = '';

            for (let i = event.resultIndex; i < event.results.length; i++) {
                const transcript = event.results[i][0].transcript;
                if (event.results[i].isFinal) {
                    finalTranscript += transcript;
                } else {
                    interimTranscript += transcript;
                }
            }

            // Send interim results for live preview in the input field
            if (interimTranscript) {
                this.dotNetRef.invokeMethodAsync('OnInterimSpeechResult', interimTranscript);
            }

            // Send final result to trigger the message send
            if (finalTranscript) {
                this.dotNetRef.invokeMethodAsync('OnFinalSpeechResult', finalTranscript);
            }
        };

        this.recognition.onerror = (event) => {
            this.isListening = false;
            let errorMsg = 'Speech recognition error';
            switch (event.error) {
                case 'no-speech':
                    errorMsg = 'No speech detected. Try again.';
                    break;
                case 'audio-capture':
                    errorMsg = 'No microphone found. Check your audio settings.';
                    break;
                case 'not-allowed':
                    errorMsg = 'Microphone access denied. Please allow microphone access in your browser settings.';
                    break;
                case 'network':
                    errorMsg = 'Network error during speech recognition.';
                    break;
                default:
                    errorMsg = 'Speech recognition error: ' + event.error;
            }
            this.dotNetRef.invokeMethodAsync('OnSpeechError', errorMsg);
        };

        this.recognition.onend = () => {
            this.isListening = false;
            this.dotNetRef.invokeMethodAsync('OnSpeechEnded');
        };

        this.recognition.start();
    },

    // Stop listening
    stop: function () {
        if (this.recognition && this.isListening) {
            this.recognition.stop();
            this.isListening = false;
        }
    },

    // Speak text aloud using browser TTS
    speak: function (text) {
        if (!this.autoSpeak) return;

        // Cancel any ongoing speech
        window.speechSynthesis.cancel();

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.rate = 1.0;
        utterance.pitch = 1.0;
        utterance.volume = 1.0;
        utterance.lang = 'en-US';

        window.speechSynthesis.speak(utterance);
    },

    // Stop any ongoing TTS playback
    stopSpeaking: function () {
        window.speechSynthesis.cancel();
    },

    // Toggle auto-speak on/off
    setAutoSpeak: function (enabled) {
        this.autoSpeak = enabled;
        if (!enabled) {
            window.speechSynthesis.cancel();
        }
    }
};
