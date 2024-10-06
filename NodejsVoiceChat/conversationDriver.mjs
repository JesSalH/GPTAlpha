// FILE: conversationDriver.mjs
import ws from './voiceChat.mjs';
import { handleOpen, handleMessage } from './voiceChatHandlers.mjs';
import { sendAudio } from './sendAudio.mjs';

// Reuse the existing handlers
ws.on("open", handleOpen);

let initialResponseReceived = false;

ws.on("message", function incoming(message) {
    handleMessage(message);

    // Call sendAudio function only once after receiving the initial response
    if (!initialResponseReceived) {
        initialResponseReceived = true;
        sendAudio();
    }
});


//lis
ws.on('message', data => {
    try {
        const event = JSON.parse(data);
        
        // Check if the event ID has already been processed
        if (!processedEventIds.has(event.event_id)) {
            processedEventIds.add(event.event_id);
            console.log(event);
        }
    } catch (e) {
        console.error(e);
    }
});