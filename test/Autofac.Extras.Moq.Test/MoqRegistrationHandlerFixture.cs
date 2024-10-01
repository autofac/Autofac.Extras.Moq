// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Autofac.Extras.Moq.Test.Stubs;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;

namespace Autofac.Extras.Moq.Test;

public class MoqRegistrationHandlerFixture
{
    private readonly MoqRegistrationHandler _systemUnderTest;

    public MoqRegistrationHandlerFixture()
    {
        _systemUnderTest = new MoqRegistrationHandler(new HashSet<Type>(), new HashSet<Type>());
    }

    [Fact]
    public void RegistrationForConcreteClass_IsHandled()
    {
        var registrations = GetRegistrations<TestImplementationOneA>();

        Assert.NotEmpty(registrations);
    }

    [Fact]
    public void RegistrationForCreatedType_IsHandled()
    {
        var createdServiceTypes = new HashSet<Type> { typeof(TestImplementationOneA) };
        var handler = new MoqRegistrationHandler(createdServiceTypes, new HashSet<Type>());
        var registrations = handler.RegistrationsFor(new TypedService(typeof(TestImplementationOneA)), s => Enumerable.Empty<ServiceRegistration>());

        Assert.NotEmpty(registrations);

        createdServiceTypes.Add(typeof(TestImplementationOneB));
        registrations = handler.RegistrationsFor(new TypedService(typeof(TestImplementationOneB)), s => Enumerable.Empty<ServiceRegistration>());

        Assert.NotEmpty(registrations);
    }

    [Fact]
    public void RegistrationForNonTypedService_IsNotHandled()
    {
        var registrations = _systemUnderTest.RegistrationsFor(
            new KeyedService("key", typeof(string)),
            null);

        Assert.Empty(registrations);
    }

    [Fact]
    public void RegistrationForNonClass_IsNotHandled()
    {
        var registrations = GetRegistrations<int>();

        Assert.Empty(registrations);
    }

    [Fact]
    public void RegistrationForString_IsNotHandled()
    {
        var registrations = GetRegistrations<string>();

        Assert.Empty(registrations);
    }

    [Fact]
    public void RegistrationForSealedConcreteClass_IsHandled()
    {
        var registrations = GetRegistrations<TestSealedClass>();

        Assert.NotEmpty(registrations);
    }

    [Fact]
    public void RegistrationsForAbstractClass_IsHandled()
    {
        var registrations = GetRegistrations<TestAbstractClass>();

        Assert.NotEmpty(registrations);
    }

    [Fact]
    public void RegistrationsForArrayType_IsHandled()
    {
        var registrations = GetRegistrations<ITestInterfaceOne[]>();

        Assert.NotEmpty(registrations);
    }

    [Fact]
    public void RegistrationsForGenericType_IsHandled()
    {
        var registrations = GetRegistrations<ITestGenericInterface<TestImplementationOneA>>();

        Assert.NotEmpty(registrations);
    }

    [Fact]
    public void RegistrationsForIEnumerable_IsNotHandled()
    {
        var registrations = GetRegistrations<IEnumerable<ITestInterfaceOne>>();

        Assert.Empty(registrations);
    }

    [Fact]
    public void RegistrationsForInterface_IsHandled()
    {
        var registrations = GetRegistrations<ITestInterfaceOne>();

        Assert.NotEmpty(registrations);
    }

    [Fact]
    public void RegistrationsForIStartableType_IsNotHandled()
    {
        var registrations = GetRegistrations<ITestStartable>();

        Assert.Empty(registrations);
    }

    [Fact]
    public void RegistrationsForLazy_IsNotHandled()
    {
        var registrations = GetRegistrations<Lazy<ITestInterfaceOne>>();

        Assert.Empty(registrations);
    }

    [Fact]
    public void RegistrationsForMeta_IsNotHandled()
    {
        var registrations = GetRegistrations<Meta<ITestInterfaceOne>>();

        Assert.Empty(registrations);
    }

    [Fact]
    public void RegistrationsForOwned_IsNotHandled()
    {
        var registrations = GetRegistrations<Owned<ITestInterfaceOne>>();

        Assert.Empty(registrations);
    }

    [Fact]
    public void AlreadyRegistered_NotHandled()
    {
        var registrations = GetRegistrations<TestImplementationOneA>(s => new[]
        {
            new ServiceRegistration(new Mock<IResolvePipeline>().Object, new Mock<IComponentRegistration>().Object),
        });

        Assert.Empty(registrations);
    }

    private IEnumerable<IComponentRegistration> GetRegistrations<T>(Func<Service, IEnumerable<ServiceRegistration>> regAccessor = null)
    {
        regAccessor ??= s => Enumerable.Empty<ServiceRegistration>();

        return _systemUnderTest.RegistrationsFor(new TypedService(typeof(T)), regAccessor);
    }
}
