// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.Moq.Test.Stubs;

public sealed class TestConsumesAbstractClass
{
    public TestConsumesAbstractClass(TestAbstractClass abstractClass)
    {
        InstanceOfAbstractClass = abstractClass;
    }

    public TestAbstractClass InstanceOfAbstractClass { get; }
}
