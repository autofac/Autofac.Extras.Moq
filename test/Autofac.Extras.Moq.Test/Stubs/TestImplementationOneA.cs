// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.Moq.Test.Stubs;

public class TestImplementationOneA : ITestInterfaceOne
{
    public bool WasRun { get; private set; }

    public int DoWork() => 0;

    public void RunOne()
    {
        WasRun = true;
    }
}
