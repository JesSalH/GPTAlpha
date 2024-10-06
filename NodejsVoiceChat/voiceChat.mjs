// FILE: voiceChat.mjs
import WebSocket from "ws";

const url = "wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01";

const ws = new WebSocket(url, {
    headers: {
        "Authorization": "Bearer " + "sk-R14EWS5WNg0G_nnEOP8zBw2ysssHNfutS-UhGxAboaT3BlbkFJbHLxuDwnL4LwazYMPrxrUnJOS8pdFi-mN6NIfuG7EA",
        "OpenAI-Beta": "realtime=v1",
    },
});

export default ws;