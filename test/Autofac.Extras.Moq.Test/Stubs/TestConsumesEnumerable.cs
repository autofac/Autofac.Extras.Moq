// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.Moq.Test.Stubs;

public class TestConsumesEnumerable
{
    public IEnumerable<ITestInterfaceOne> All { get; }

    public TestConsumesEnumerable(IEnumerable<ITestInterfaceOne> all)
    {
        All = all;
    }
}
