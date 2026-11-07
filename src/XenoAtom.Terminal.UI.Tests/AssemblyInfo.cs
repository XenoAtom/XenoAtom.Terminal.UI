// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Test classes run in parallel; classes that use process-wide state (the global Terminal instance) opt out with [DoNotParallelize].
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel, Workers = 0)]

