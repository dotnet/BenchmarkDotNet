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
        this.onerror = null;
        this.onclose = null;

        this._openPromise = this.#init();
    }

    async #init() {
        // Create WebSocket instance (Node.js)
        const { WebSocket } = await import("ws");
        this.ws = new WebSocket(this.url);

        // Setup event handlers (Node.js API)
        return new Promise((resolve, reject) => {
            this.ws.on("open", () => {
                console.log("IPC WebSocket connected");
                resolve();
            });

            this.ws.on("error", (err) => {
                console.error("IPC WebSocket error:", err);
                this.onerror?.(err);
                reject(err);
            });

            this.ws.on("close", (code, reason) => {
                console.log("IPC WebSocket closed");
                this.onclose?.({ code, reason });
            });
        });
    }

    async waitUntilOpen() {
        return this._openPromise;
    }

    send(msg) {
        if (this.ws && this.ws.readyState === 1) {
            this.ws.send(msg);
        } else {
            console.warn("Tried to send before WebSocket was open:", msg);
        }
    }

    async sendSignal(msg) {
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => {
                reject(new Error('Timeout waiting for signal acknowledgment'));
            }, 60000);

            const handleMessage = (data) => {
                clearTimeout(timeout);
                this.ws.off("message", handleMessage);
                resolve(data.toString());
            };

            this.ws.on("message", handleMessage);
            this.send(msg);
        });
    }

    close() {
        this.ws?.close();
    }
}

let transport = null;

const wsUrl = `ws://localhost:${ipcPort}/child`;
console.log("[CUSTOM-TEMPLATE-WEBSOCKET] Using WebSocket IPC:", wsUrl);
transport = new WebSocketTransport(wsUrl);
await transport.waitUntilOpen();

const { setModuleImports, getAssemblyExports } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...args)
    .create();

const exports = await getAssemblyExports("BenchmarkDotNet");

setModuleImports("ipc", {
    sendToParent: msg => {
        transport.send(msg);
    },
    sendSignalToParent: msg => {
        transport.sendSignal(msg).then(ack => {
            exports.BenchmarkDotNet.Engines.JsHost.OnSignalAcknowledged(ack);
        }).catch(err => {
            console.error("Signal acknowledgment error:", err);
        });
    }
});

try {
    await dotnet.run();
} finally {
    transport?.close();
}
