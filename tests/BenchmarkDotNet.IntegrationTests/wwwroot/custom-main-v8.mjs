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
        this.onerror = null;
        this.onclose = null;
    }

    send(msg) {
        // Send message to parent via stdout
        console.log(msg);
    }

    async sendSignal(msg) {
        // Send signal message to parent via stdout
        console.log(msg);

        // Wait for acknowledgment file
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

        for (let attempt = 0; attempt < maxAttempts; attempt++) {
            try {
                // Try to read the acknowledgment file
                const ackContent = read(ackFile);
                if (ackContent !== undefined && ackContent !== null && ackContent !== "") {
                    return ackContent.trim();
                }
            } catch (e) {
                // File doesn't exist yet, continue polling
            }

            await sleep(sleepMs);
        }

        throw new Error(`Timeout waiting for acknowledgment file: ${ackFile}`);
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

await dotnet.run();
