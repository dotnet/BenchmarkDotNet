// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from "./_framework/dotnet.js";

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
    // v8
    if (globalThis.arguments !== undefined)
        return globalThis.arguments;

    // spdermonkey
    if (globalThis.scriptArgs !== undefined)
        return globalThis.scriptArgs;

    // Node / Bun
    if (globalThis.process !== undefined) {
        const argv = globalThis.process.argv ?? [];
        const sep = argv.indexOf("--");
        return sep >= 0 ? argv.slice(sep + 1) : argv.slice(2);
    }

    throw new Error("Unable to determine application arguments for the current runtime.");
}

const args = getAppArgs();

await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...args)
    .run();
