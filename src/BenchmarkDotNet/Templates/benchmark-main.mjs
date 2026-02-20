// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

// Get command line arguments: Node.js uses process.argv, v8 uses arguments/scriptArgs
const args = typeof process !== 'undefined' ? process.argv.slice(2) : (typeof arguments !== 'undefined' ? [...arguments] : (typeof scriptArgs !== 'undefined' ? scriptArgs : []));

await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...args)
    .run()
