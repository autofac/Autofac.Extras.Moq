using System.Collections.Generic;
using Autofac.Core;
using Autofac.Extras.Moq;
using NUnit.Framework;

namespace Autofac.Extras.Tests.Moq
{
    class MoqRegistrationHandlerTests
    {
        private class TestConcreteClass {}
        private interface ITestInterface {}
        private abstract class TestAbstractClass {}
        private interface ITestGenericInterface<T> {}
        private interface ISomethingStartable : IStartable {}

        private MoqRegistrationHandler _systemUnderTest;
        //
        [SetUp]
        public void SetUp()
        {
            _systemUnderTest = new MoqRegistrationHandler();
        }

        [Test]
        public void RegistrationsForInterface_IsHandled()
        {
            var registrations = GetRegistrations<ITestInterface>();

            Assert.IsNotEmpty(registrations);
        }

        private IEnumerable<IComponentRegistration> GetRegistrations<T>()
        {
            return _systemUnderTest.RegistrationsFor(
                new TypedService(typeof (T)),
                registrationAccessor: null);
        }

        [Test]
        public void RegistrationsForAbstractClass_IsHandled()
        {
            var registrations = GetRegistrations<TestAbstractClass>();

            Assert.IsNotEmpty(registrations);
        }

        [Test]
        public void RegistrationsForGenericType_IsHandled()
        {
            var registrations = GetRegistrations<ITestGenericInterface<TestConcreteClass>>();

            Assert.IsNotEmpty(registrations);
        }

        [Test]
        public void RegistrationForNonTypedService_IsNotHandled()
        {
            var registrations = _systemUnderTest.RegistrationsFor(
                new KeyedService(serviceKey: "key", serviceType: typeof (string)),
                registrationAccessor: null);

            Assert.IsEmpty(registrations);
        }

        [Test]
        public void RegistrationForConcreteClass_IsNotHandled()
        {
            var registrations = GetRegistrations<TestConcreteClass>();

            Assert.IsEmpty(registrations);
        }

        [Test]
        public void RegistrationsForIEnumerable_IsNotHandled()
        {
            var registrations = GetRegistrations<IEnumerable<ITestInterface>>();

            Assert.IsEmpty(registrations);
        }

        [Test]
        public void RegistrationsForArrayType_IsNotHandled()
        {
            var registrations = GetRegistrations<ITestInterface[]>();

            Assert.IsEmpty(registrations);
        }

        [Test]
        public void RegistrationsForIStartableType_IsNotHandled()
        {
            var registrations = GetRegistrations<ISomethingStartable>();

            Assert.IsEmpty(registrations);
        }
    }
}
