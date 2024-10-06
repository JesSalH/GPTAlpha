// FILE: voiceChatHandlers.mjs
import ws from './voiceChat.mjs';

export function handleOpen() {
    console.log("Connected to server.");
    ws.send(JSON.stringify({
        type: "response.create",
        response: {
            modalities: ["text"],
            instructions: "Please assist the user. The user is a very important king. He should be addressed as 'Your Majesty', 'Your Highness', 'Oh Great One' and other similar King salutation.",
        }
    }));
}

export function handleMessage(message) {
    console.log(JSON.parse(message.toString()));
}