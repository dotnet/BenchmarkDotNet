// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

console.log("hello from custom-main-node.mjs");

import { dotnet } from "./_framework/dotnet.js";

// Node.js argument parsing
const argv = globalThis.process.argv ?? [];
const sep = argv.indexOf("--");
const args = sep >= 0 ? argv.slice(sep + 1) : argv.slice(2);

let ipcPort = null;
for (let i = 0; i < args.length; i++) {
    if (args[i] === "--ipcPort" && args[i + 1]) {
        ipcPort = parseInt(args[i + 1], 10);
    }
}

class WebSocketTransport {
    constructor(url) {
        this.url = url;
        this.ws = null;
        this.onmessage = null;
        this.onerror = null;
        this.onclose = null;
    }

    async initialize() {
        // Create WebSocket instance (Node.js)
        const { WebSocket } = await import("ws");
        this.ws = new WebSocket(this.url);

        // Setup event handlers (Node.js API)
        return new Promise((resolve, reject) => {
            const handleOpen = () => {
                console.log("IPC WebSocket connected");
                resolve();
            };

            const handleError = (err) => {
                console.error("IPC WebSocket error:", err);
                this.onerror?.(err);
                reject(err);
            };

            const handleClose = (code, reason) => {
                console.log("IPC WebSocket closed");
                this.onclose?.({ code, reason });
            };

            const handleMessage = (data) => {
                const message = data.toString();
                // Forward message to handler
                this.onmessage?.(message);
            };

            this.ws.on("open", handleOpen);
            this.ws.on("error", handleError);
            this.ws.on("close", handleClose);
            this.ws.on("message", handleMessage);
        });
    }

    send(msg) {
        if (this.ws && this.ws.readyState === 1) {
            this.ws.send(msg);
        } else {
            console.warn("Tried to send before WebSocket was open:", msg);
        }
    }

    sendSignal(msg) {
        this.send(msg);
    }

    close() {
        this.ws?.close();
    }
}

let transport = null;

const wsUrl = `ws://localhost:${ipcPort}/child`;
console.log("[CUSTOM-TEMPLATE-WEBSOCKET] Using WebSocket IPC:", wsUrl);
transport = new WebSocketTransport(wsUrl);

const { setModuleImports, getAssemblyExports } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...args)
    .create();

const exports = await getAssemblyExports("BenchmarkDotNet");
const jsHostExports = exports.BenchmarkDotNet.Engines.JsHost;

setModuleImports("ipc", {
    sendToParent: msg => {
        transport.send(msg);
    },
    sendSignalToParent: msg => {
        transport.sendSignal(msg);
    }
});

// Set up message handler - forward all messages to C#
transport.onmessage = (message) => {
    jsHostExports.ReceiveMessage(message);
};

await transport.initialize();

try {
    await dotnet.run();
} finally {
    transport?.close();
}
