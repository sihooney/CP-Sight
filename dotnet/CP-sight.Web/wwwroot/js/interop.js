// CP-sight JavaScript Interop

window.cpSight = {
    // ========================================
    // FILE DOWNLOAD
    // ========================================
    
    /**
     * Download a byte array as a file
     * @param {string} fileName - Name of the file to download
     * @param {string} base64Data - Base64-encoded file content
     * @param {string} contentType - MIME type of the file
     */
    downloadFile: function (fileName, base64Data, contentType) {
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });
        const url = URL.createObjectURL(blob);
        
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    // ========================================
    // WEBCAM / LIVE VIDEO FEED
    // ========================================

    _videoStream: null,
    _captureInterval: null,

    /**
     * Start webcam capture and render to a video element
     * @param {string} videoElementId - ID of the <video> element
     * @returns {boolean} true if started successfully
     */
    startWebcam: async function (videoElementId) {
        try {
            const video = document.getElementById(videoElementId);
            if (!video) return false;
            
            const stream = await navigator.mediaDevices.getUserMedia({
                video: { width: { ideal: 640 }, height: { ideal: 480 }, facingMode: 'environment' },
                audio: false
            });
            
            video.srcObject = stream;
            await video.play();
            cpSight._videoStream = stream;
            return true;
        } catch (err) {
            console.error('Webcam access failed:', err);
            return false;
        }
    },

    /**
     * Stop the webcam stream
     */
    stopWebcam: function () {
        if (cpSight._captureInterval) {
            clearInterval(cpSight._captureInterval);
            cpSight._captureInterval = null;
        }
        if (cpSight._videoStream) {
            cpSight._videoStream.getTracks().forEach(t => t.stop());
            cpSight._videoStream = null;
        }
    },

    /**
     * Capture a single frame from the video element as a base64 JPEG
     * @param {string} videoElementId - ID of the <video> element
     * @param {string} canvasElementId - ID of a hidden <canvas> for capture
     * @returns {string|null} base64-encoded JPEG data (without prefix)
     */
    captureFrame: function (videoElementId, canvasElementId) {
        const video = document.getElementById(videoElementId);
        const canvas = document.getElementById(canvasElementId);
        if (!video || !canvas || video.readyState < 2) return null;

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        const ctx = canvas.getContext('2d');
        ctx.drawImage(video, 0, 0);
        
        // Return base64 JPEG without the data:image/jpeg;base64, prefix
        const dataUrl = canvas.toDataURL('image/jpeg', 0.85);
        return dataUrl.split(',')[1];
    },

    /**
     * Start periodic frame capture and send to .NET
     * @param {string} videoElementId
     * @param {string} canvasElementId
     * @param {object} dotNetRef - .NET object reference for callback
     * @param {number} intervalMs - Capture interval in ms
     */
    startFrameCapture: function (videoElementId, canvasElementId, dotNetRef, intervalMs) {
        if (cpSight._captureInterval) {
            clearInterval(cpSight._captureInterval);
        }
        
        cpSight._captureInterval = setInterval(async () => {
            const frame = cpSight.captureFrame(videoElementId, canvasElementId);
            if (frame) {
                try {
                    await dotNetRef.invokeMethodAsync('OnFrameCaptured', frame);
                } catch (err) {
                    console.error('Frame callback failed:', err);
                }
            }
        }, intervalMs);
    },

    /**
     * Stop periodic frame capture
     */
    stopFrameCapture: function () {
        if (cpSight._captureInterval) {
            clearInterval(cpSight._captureInterval);
            cpSight._captureInterval = null;
        }
    }
};
