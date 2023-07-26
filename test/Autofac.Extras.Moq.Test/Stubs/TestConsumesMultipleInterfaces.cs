// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.Moq.Test.Stubs;

public sealed class TestConsumesMultipleInterfaces
{
    private readonly ITestInterfaceOne _serviceA;

    private readonly ITestInterfaceTwo _serviceB;

    public TestConsumesMultipleInterfaces(ITestInterfaceOne serviceA, ITestInterfaceTwo serviceB)
    {
        _serviceA = serviceA;
        _serviceB = serviceB;
    }

    public void RunAll()
    {
        _serviceA.RunOne();
        _serviceB.RunTwo();
    }
}
