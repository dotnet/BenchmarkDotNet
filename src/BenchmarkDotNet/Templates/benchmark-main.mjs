// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This implementation assumes this is running in Node

const args = process.argv.slice(2);

let ipcPort = null;
for (let i = 0; i < args.length; i++) {
    if (args[i] === "--ipcPort" && args[i + 1]) {
        ipcPort = parseInt(args[i + 1], 10);
        break;
    }
}

const ipcEnabled = Number.isFinite(ipcPort);

class WebSocketTransport {
    constructor(url) {
        this.url = url;
        this.ws = null;
        this.onmessage = null;
        this.onerror = null;
        this.onclose = null;

        this._openPromise = this.#init();
    }

    async #init() {
        const { WebSocket } = await import("ws");
        this.ws = new WebSocket(this.url);

        return new Promise((resolve, reject) => {
            this.ws.on("open", () => {
                console.log("IPC WebSocket connected");
                resolve();
            });

            this.ws.on("message", data => {
                const msg = data.toString();
                this.onmessage?.(msg);
            });

            this.ws.on("error", err => {
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

    close() {
        this.ws?.close();
    }
}

let transport = null;

if (ipcEnabled) {
    const wsUrl = `ws://localhost:${ipcPort}/child`;
    transport = new WebSocketTransport(wsUrl);
    await transport.waitUntilOpen();
} else {
    console.log("IPC disabled (no --ipcPort provided)");
}

import { dotnet } from "./_framework/dotnet.js";

const { setModuleImports, getAssemblyExports } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...args)
    .create();

if (ipcEnabled) {
    setModuleImports("ipc", {
        sendToParent: msg => {
            transport.send(msg);
        }
    });

    const exports = await getAssemblyExports("BenchmarkDotNet");
    transport.onmessage = msg => {
        exports.BenchmarkDotNet.Engines.WebSocketHost.ReceiveMessage(msg);
    };
}

await dotnet.run();
