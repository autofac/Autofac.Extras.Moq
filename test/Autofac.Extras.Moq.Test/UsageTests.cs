using System;
using Moq;
using Xunit;

namespace Autofac.Extras.Moq.Test
{
    public class UsageTests
    {
        [Fact]
        public void AutoMocking_SomeClass()
        {
            using (var am = AutoMock.GetStrict())
            {
                am.VerifyAll = true;
                var mock = am.Mock<SomeClass>();
                mock.Setup(sc => sc.SomeMethod());
                mock.Object.SomeMethod();
            }
        }

        [Fact]
        public void AutoMocking_SomeBaseClassWithVirtualMethod()
        {
            using (var am = AutoMock.GetStrict())
            {
                am.VerifyAll = true;
                var mock = am.Mock<SomeBaseClassWithVirtualMethod>();
                mock.Setup(sc => sc.SomeMethod());
                mock.Object.SomeMethod();
            }
        }

        [Fact]
        public void AutoMocking_SomeClassWithVirtualMethod()
        {
            using (var am = AutoMock.GetStrict())
            {
                am.VerifyAll = true;
                var mock = am.Mock<SomeClassWithVirtualMethod>();
                mock.Setup(sc => sc.SomeMethod());
                mock.Object.SomeMethod();
            }
        }

        [Fact]
        public void AutoMocking_SomeAbstractClass()
        {
            using (var am = AutoMock.GetStrict())
            {
                am.VerifyAll = true;
                var mock = am.Mock<SomeAbstractClass>();
                mock.Setup(sc => sc.SomeMethod());
                mock.Object.SomeMethod();
            }
        }

        [Fact]
        public void Mocking_SomeClass()
        {
            var mock = new Mock<SomeClass>();
            mock.Setup(sc => sc.SomeMethod());
            mock.Object.SomeMethod();
            mock.VerifyAll();
        }

        [Fact]
        public void Mocking_SomeAbstractClass()
        {
            var mock = new Mock<SomeAbstractClass>();
            mock.Setup(sc => sc.SomeMethod());
            mock.Object.SomeMethod();
            mock.VerifyAll();
        }

        [Fact]
        public void Mocking_SomeBaseClassWithVirtualMethod()
        {
            var mock = new Mock<SomeBaseClassWithVirtualMethod>();
            mock.Setup(sc => sc.SomeMethod());
            mock.Object.SomeMethod();
            mock.VerifyAll();
        }

        [Fact]
        public void Mocking_SomeClassWithVirtualMethod()
        {
            var mock = new Mock<SomeClassWithVirtualMethod>();
            mock.Setup(sc => sc.SomeMethod());
            mock.Object.SomeMethod();
            mock.VerifyAll();
        }
    }

    public abstract class SomeAbstractClass
    {
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        public virtual void SomeMethod()
        {
            throw new NotImplementedException();
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class SomeClass : SomeAbstractClass
    {
    }

    public class SomeBaseClassWithVirtualMethod
    {
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        public virtual void SomeMethod()
        {
            throw new NotImplementedException();
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class SomeClassWithVirtualMethod : SomeBaseClassWithVirtualMethod
    {
    }
}
