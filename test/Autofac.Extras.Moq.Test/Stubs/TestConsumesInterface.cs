// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.Moq.Test.Stubs;

public class TestConsumesInterface
{
    public TestConsumesInterface(ITestInterfaceOne dependency)
    {
        Dependency = dependency;
    }

    public ITestInterfaceOne Dependency { get; }
}
