// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from "./_framework/dotnet.js";

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
