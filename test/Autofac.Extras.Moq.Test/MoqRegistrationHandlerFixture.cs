using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;
using Moq;
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
        public void RegistrationForCreatedType_IsHandled()
        {
            var createdServiceTypes = new List<Type> { typeof(TestConcreteClass) };
            var handler = new MoqRegistrationHandler(createdServiceTypes);
            var registrations = handler.RegistrationsFor(new TypedService(typeof(TestConcreteClass)), s => Enumerable.Empty<IComponentRegistration>());

            Assert.NotEmpty(registrations);

            createdServiceTypes.Add(typeof(TestAbstractClass));
            registrations = handler.RegistrationsFor(new TypedService(typeof(TestAbstractClass)), s => Enumerable.Empty<IComponentRegistration>());

            Assert.NotEmpty(registrations);
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
        public void RegistrationForSealedConcreteClass_IsHandled()
        {
            var registrations = GetRegistrations<TestSealedConcreteClass>();

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
            var registrations = GetRegistrations<ITestInterface[]>();

            Assert.NotEmpty(registrations);
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

        [Fact]
        public void AlreadyRegistered_NotHandled()
        {
            var registrations = GetRegistrations<TestConcreteClass>(s => new[]
            {
                new Mock<IComponentRegistration>().Object,
            });

            Assert.Empty(registrations);
        }

        private IEnumerable<IComponentRegistration> GetRegistrations<T>(Func<Service, IEnumerable<IComponentRegistration>> regAccessor = null)
        {
            regAccessor ??= s => Enumerable.Empty<IComponentRegistration>();

            return this._systemUnderTest.RegistrationsFor(new TypedService(typeof(T)), regAccessor);
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
