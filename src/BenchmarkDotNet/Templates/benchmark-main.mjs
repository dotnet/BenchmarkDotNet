// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

import { dotnet } from './_framework/dotnet.js'

// Get command line arguments: Node.js uses process.argv, v8 uses arguments/scriptArgs
const args = typeof process !== 'undefined' ? process.argv.slice(2) : (typeof arguments !== 'undefined' ? [...arguments] : (typeof scriptArgs !== 'undefined' ? scriptArgs : []));

await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...args)
    .run()
