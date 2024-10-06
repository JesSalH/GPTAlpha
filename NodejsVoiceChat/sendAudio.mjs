// FILE: sendAudio.mjs
import fs from 'fs';
import decodeAudio from 'audio-decode';
import ws from './voiceChat.mjs';

// Converts Float32Array of audio data to PCM16 ArrayBuffer
function floatTo16BitPCM(float32Array) {
    const buffer = new ArrayBuffer(float32Array.length * 2);
    const view = new DataView(buffer);
    let offset = 0;
    for (let i = 0; i < float32Array.length; i++, offset += 2) {
        let s = Math.max(-1, Math.min(1, float32Array[i]));
        view.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7fff, true);
    }
    return buffer;
}

// Converts a Float32Array to base64-encoded PCM16 data
function base64EncodeAudio(float32Array) {
    const arrayBuffer = floatTo16BitPCM(float32Array);
    let binary = '';
    let bytes = new Uint8Array(arrayBuffer);
    const chunkSize = 0x8000; // 32KB chunk size
    for (let i = 0; i < bytes.length; i += chunkSize) {
        let chunk = bytes.subarray(i, i + chunkSize);
        binary += String.fromCharCode.apply(null, chunk);
    }
    return btoa(binary);
}

export async function sendAudio() {
    try {
        const myAudio = fs.readFileSync('D:/DEV/recordings/madrid.wav');
        const audioBuffer = await decodeAudio(myAudio);
        const channelData = audioBuffer.getChannelData(0); // only accepts mono
        const base64AudioData = base64EncodeAudio(channelData);

        const event = {
            type: 'conversation.item.create',
            item: {
                type: 'message',
                role: 'user',
                content: [
                    {
                        type: 'input_audio',
                        audio: base64AudioData
                    }
                ]
            }
        };

        ws.send(JSON.stringify(event));
        ws.send(JSON.stringify({type: 'response.create'}));
    } catch (error) {
        console.error("Error reading or processing the audio file:", error.message);
    }
}