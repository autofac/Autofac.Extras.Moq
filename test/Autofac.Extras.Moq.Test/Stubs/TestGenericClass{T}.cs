// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.Moq.Test.Stubs;

public class TestGenericClass<T>
{
    private readonly T _dependency;

    public TestGenericClass(T dependency)
    {
        _dependency = dependency;
    }
}
