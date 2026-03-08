// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

console.log("hello from custom-main-v8.mjs");

import { dotnet } from "./_framework/dotnet.js";

const args = arguments;

let ipcDir = null;
for (let i = 0; i < args.length; i++) {
    if (args[i] === "--ipcDir" && args[i + 1]) {
        ipcDir = args[i + 1];
    }
}

class FileStdOutTransport {
    constructor(ipcDirectory) {
        this.ipcDirectory = ipcDirectory;
        this.ackCounter = 0;
        this.onmessage = null;
        this.onerror = null;
        this.onclose = null;
        this.cancellationCheckInterval = null;
    }

    async initialize() {
        // File-based transport is immediately ready
        // Start cancellation checking
        if (this.onmessage && typeof setInterval !== "undefined" && !this.cancellationCheckInterval) {
            this.cancellationCheckInterval = setInterval(() => {
                this.checkCancellation();
            }, 10);
        }
    }

    checkCancellation() {
        const cancelFile = `${this.ipcDirectory}/cancel.txt`;
        try {
            const content = read(cancelFile);
            if (content !== undefined && content !== null) {
                this.onmessage?.("CANCEL");
                return true;
            }
        } catch (e) {
            // File doesn't exist - not cancelled
        }
        return false;
    }

    send(msg) {
        // Send message to parent via stdout
        console.log(msg);
        this.checkCancellation();
    }

    sendSignal(msg) {
        // Send signal message to parent via stdout
        console.log(msg);

        // Wait for acknowledgment file in background
        const ackFile = `${this.ipcDirectory}/ack-${this.ackCounter++}.txt`;
        const maxAttempts = 6000; // Poll for up to 60 seconds
        const sleepMs = 10;

        // Busy wait for v8/d8 (no setTimeout support)
        const sleep = (ms) => {
            const start = Date.now();
            while (Date.now() - start < ms) {
                // Busy wait
            }
            return Promise.resolve();
        };

        // Poll for acknowledgment asynchronously and forward through onmessage
        (async () => {
            for (let attempt = 0; attempt < maxAttempts; attempt++) {
                // Check for cancellation while waiting
                if (this.checkCancellation()) {
                    return; // Exit early if cancelled
                }

                try {
                    // Try to read the acknowledgment file
                    const ackContent = read(ackFile);
                    if (ackContent !== undefined && ackContent !== null && ackContent !== "") {
                        // Forward acknowledgment through onmessage handler
                        this.onmessage?.(ackContent.trim());
                        return;
                    }
                } catch (e) {
                    // File doesn't exist yet, continue polling
                }

                await sleep(sleepMs);
            }

            console.error(`Timeout waiting for acknowledgment file: ${ackFile}`);
        })();
    }

    close() {
        if (this.cancellationCheckInterval) {
            clearInterval(this.cancellationCheckInterval);
            this.cancellationCheckInterval = null;
        }
    }
}

let transport = null;

console.log("Using file-based IPC:", ipcDir);
transport = new FileStdOutTransport(ipcDir);

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
