using System;
using System.Collections.Generic;
using Autofac.Core;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;
using Xunit;

namespace Autofac.Extras.Moq.Test
{
    public class MoqRegistrationHandlerFixture
    {
        private readonly MoqRegistrationHandler _systemUnderTest;

        public MoqRegistrationHandlerFixture()
        {
            this._systemUnderTest = new MoqRegistrationHandler(new List<Type>());
        }

        private interface ISomethingStartable : IStartable
        {
        }

        private interface ITestGenericInterface<T>
        {
        }

        private interface ITestInterface
        {
        }

        [Fact]
        public void RegistrationForConcreteClass_IsHandled()
        {
            var registrations = GetRegistrations<TestConcreteClass>();

            Assert.NotEmpty(registrations);
        }

        [Fact]
        public void RegistrationForCreatedType_IsNotHandled()
        {
            var createdServiceTypes = new List<Type> { typeof(TestConcreteClass) };
            var handler = new MoqRegistrationHandler(createdServiceTypes);
            var registrations = handler.RegistrationsFor(new TypedService(typeof(TestConcreteClass)), null);

            Assert.Empty(registrations);

            createdServiceTypes.Add(typeof(TestAbstractClass));
            registrations = handler.RegistrationsFor(new TypedService(typeof(TestAbstractClass)), null);

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationForNonTypedService_IsNotHandled()
        {
            var registrations = this._systemUnderTest.RegistrationsFor(
                new KeyedService("key", typeof(string)),
                null);

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationForSealedConcreteClass_IsNotHandled()
        {
            var registrations = GetRegistrations<TestSealedConcreteClass>();

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationsForAbstractClass_IsHandled()
        {
            var registrations = GetRegistrations<TestAbstractClass>();

            Assert.NotEmpty(registrations);
        }

        [Fact]
        public void RegistrationsForArrayType_IsNotHandled()
        {
            var registrations = GetRegistrations<ITestInterface[]>();

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationsForGenericType_IsHandled()
        {
            var registrations = GetRegistrations<ITestGenericInterface<TestConcreteClass>>();

            Assert.NotEmpty(registrations);
        }

        [Fact]
        public void RegistrationsForIEnumerable_IsNotHandled()
        {
            var registrations = GetRegistrations<IEnumerable<ITestInterface>>();

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationsForInterface_IsHandled()
        {
            var registrations = GetRegistrations<ITestInterface>();

            Assert.NotEmpty(registrations);
        }

        [Fact]
        public void RegistrationsForIStartableType_IsNotHandled()
        {
            var registrations = GetRegistrations<ISomethingStartable>();

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationsForLazy_IsNotHandled()
        {
            var registrations = GetRegistrations<Lazy<ITestInterface>>();

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationsForMeta_IsNotHandled()
        {
            var registrations = GetRegistrations<Meta<ITestInterface>>();

            Assert.Empty(registrations);
        }

        [Fact]
        public void RegistrationsForOwned_IsNotHandled()
        {
            var registrations = GetRegistrations<Owned<ITestInterface>>();

            Assert.Empty(registrations);
        }

        private IEnumerable<IComponentRegistration> GetRegistrations<T>()
        {
            return this._systemUnderTest.RegistrationsFor(new TypedService(typeof(T)), null);
        }

        private abstract class TestAbstractClass
        {
        }

        private class TestConcreteClass
        {
        }

        private sealed class TestSealedConcreteClass
        {
        }
    }
}
