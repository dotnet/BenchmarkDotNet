// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from "./_framework/dotnet.js";

// Capture v8 arguments at module scope before they're potentially modified
const v8args = typeof arguments !== "undefined" ? arguments : undefined;

const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
const ENVIRONMENT_IS_WEB_WORKER = typeof importScripts == "function";
const ENVIRONMENT_IS_WEB = typeof window == "object" || (ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_NODE);

if (!ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WEB && typeof globalThis.crypto === 'undefined') {
    // **NOTE** this is a simple insecure polyfill for JS shells (like v8/d8) that lack Web Crypto API.
    // /dev/random doesn't work on js shells, so define our own.
    globalThis.crypto = {
        getRandomValues: function (buffer) {
            for (let i = 0; i < buffer.length; i++)
                buffer[i] = (Math.random() * 256) | 0;
        }
    }
}

function getAppArgs() {
    // Node / Bun
    if (ENVIRONMENT_IS_NODE) {
        const argv = globalThis.process.argv ?? [];
        const sep = argv.indexOf("--");
        return sep >= 0 ? argv.slice(sep + 1) : argv.slice(2);
    }

    // v8/d8
    if (v8args !== undefined)
        return Array.from(v8args);

    // SpiderMonkey
    if (typeof scriptArgs !== "undefined")
        return Array.from(scriptArgs);

    // Windows Script Host / Chakra
    if (typeof WScript !== "undefined" && WScript.Arguments)
        return Array.from(WScript.Arguments);

    throw new Error("Unable to determine application arguments for the current runtime.");
}

const args = getAppArgs();

// Handle WebSocket capability probe
if (args.includes("--getSupportsWebSocket")) {
    let supportsWebSocket = false;

    // First check for native WebSocket support (browsers, some modern engines)
    if (typeof WebSocket !== "undefined") {
        supportsWebSocket = true;
    } else {
        // Try to import ws module (Node.js)
        try {
            await import("ws");
            supportsWebSocket = true;
        } catch (e) {
            // Neither native WebSocket nor ws module available
        }
    }

    console.log(`supportsWebSocket: ${supportsWebSocket}`);
    if (typeof process !== "undefined" && process.exit) {
        process.exit(0);
    } else if (typeof quit !== "undefined") {
        quit(0);
    } else if (typeof WScript !== "undefined") {
        WScript.Quit(0);
    }
    // Fallback: just return (script will end naturally)
    throw new Error("Exit after WebSocket probe");
}

let ipcPort = null;
let ipcDir = null;
for (let i = 0; i < args.length; i++) {
    if (args[i] === "--ipcPort" && args[i + 1]) {
        ipcPort = parseInt(args[i + 1], 10);
    } else if (args[i] === "--ipcDir" && args[i + 1]) {
        ipcDir = args[i + 1];
    }
}

const ipcEnabled = Number.isFinite(ipcPort) || ipcDir !== null;

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
        // Try native WebSocket first, then try importing ws module
        if (typeof WebSocket !== "undefined") {
            this.ws = new WebSocket(this.url);
        } else {
            const { WebSocket } = await import("ws");
            this.ws = new WebSocket(this.url);
        }

        // Detect API style by checking for addEventListener method
        const useEventListeners = typeof this.ws.addEventListener === "function";

        // Setup event handlers with normalized API
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

            const handleClose = (codeOrEvent, reason) => {
                console.log("IPC WebSocket closed");
                const closeInfo = useEventListeners
                    ? { code: codeOrEvent.code, reason: codeOrEvent.reason }
                    : { code: codeOrEvent, reason };
                this.onclose?.(closeInfo);
            };

            if (useEventListeners) {
                this.ws.addEventListener("open", handleOpen);
                this.ws.addEventListener("error", handleError);
                this.ws.addEventListener("close", handleClose);
            } else {
                this.ws.on("open", handleOpen);
                this.ws.on("error", handleError);
                this.ws.on("close", handleClose);
            }
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

            const useEventListeners = typeof this.ws.addEventListener === "function";

            const handleMessage = (dataOrEvent) => {
                clearTimeout(timeout);
                const ack = useEventListeners
                    ? (typeof dataOrEvent.data === "string" ? dataOrEvent.data : dataOrEvent.data.toString())
                    : dataOrEvent.toString();

                if (useEventListeners) {
                    this.ws.removeEventListener("message", handleMessage);
                } else {
                    this.ws.off("message", handleMessage);
                }

                resolve(ack);
            };

            if (useEventListeners) {
                this.ws.addEventListener("message", handleMessage);
            } else {
                this.ws.on("message", handleMessage);
            }

            this.send(msg);
        });
    }

    close() {
        this.ws?.close();
    }
}

class FileStdOutTransport {
    constructor(ipcDirectory) {
        this.ipcDirectory = ipcDirectory;
        this.ackCounter = 0;
        this.onmessage = null;
        this.onerror = null;
        this.onclose = null;
        this._openPromise = Promise.resolve();
    }

    async waitUntilOpen() {
        return this._openPromise;
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

        // Helper to sleep - works across different JS engines
        const sleep = (ms) => {
            if (typeof setTimeout !== "undefined") {
                // Node.js, browsers, some shells
                return new Promise(resolve => setTimeout(resolve, ms));
            } else if (typeof WScript !== "undefined" && WScript.Sleep) {
                // Windows Script Host
                WScript.Sleep(ms);
                return Promise.resolve();
            } else {
                // Fallback: busy wait (not ideal, but works everywhere)
                const start = Date.now();
                while (Date.now() - start < ms) {
                    // Busy wait
                }
                return Promise.resolve();
            }
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

    close() {
        // Nothing to close for file-based IPC
    }
}

let transport = null;

if (ipcEnabled) {
    try {
        if (ipcPort !== null) {
            // WebSocket-based IPC (typically for Node.js, Bun, browsers)
            const wsUrl = `ws://localhost:${ipcPort}/child`;
            console.log("Using WebSocket IPC:", wsUrl);
            transport = new WebSocketTransport(wsUrl);
            await transport.waitUntilOpen();
        } else if (ipcDir !== null) {
            // File-based IPC (typically for shell engines: v8/d8, SpiderMonkey, WSH/Chakra)
            console.log("Using file-based IPC:", ipcDir);
            transport = new FileStdOutTransport(ipcDir);
            await transport.waitUntilOpen();
        }
    } catch (error) {
        console.error("Failed to initialize IPC:", error);
        throw error;
    }
} else {
    console.log("IPC disabled (no --ipcPort or --ipcDir provided)");
}

const { setModuleImports, getAssemblyExports } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...args)
    .create();

if (ipcEnabled && transport) {
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
}

try {
    await dotnet.run();
} finally {
    transport?.close();
}
