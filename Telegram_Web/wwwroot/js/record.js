let mediaRecorder;
let audioChunks = [];
let dotNetHelper;

// This function is called from Blazor to initialize the recorder
window.initializeRecorder = (dotNetRef) => {
    dotNetHelper = dotNetRef;
};

// Start recording audio
window.startRecording = async () => {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        mediaRecorder = new MediaRecorder(stream);
        audioChunks = [];

        mediaRecorder.ondataavailable = event => {
            audioChunks.push(event.data);
        };

        mediaRecorder.onstop = () => {
            console.log("Recording stopped. Processing audio...");
            const audioBlob = new Blob(audioChunks, { type: 'audio/ogg; codecs=opus' });
            console.log("Blob created, size:", audioBlob.size);

            const reader = new FileReader();
            reader.readAsDataURL(audioBlob);
            reader.onloadend = () => {
                const base64Audio = reader.result.split(',')[1];
                console.log("Base64 audio ready, length:", base64Audio.length);

                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnRecordingComplete', base64Audio)
                        .then(() => console.log("DotNet method invoked successfully."))
                        .catch(err => console.error("Error calling DotNet method:", err));
                } else {
                    console.warn("DotNet helper is not initialized!");
                }
            };
        };


        mediaRecorder.start();
        return true;
    } catch (err) {
        console.error("Error starting recording: ", err);
        return false;
    }
};

// Stop recording audio
window.stopRecording = () => {
    if (mediaRecorder && mediaRecorder.state === "recording") {
        mediaRecorder.stop();
    }
};