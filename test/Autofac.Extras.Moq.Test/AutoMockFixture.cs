// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Core;
using Moq;
using Xunit;

namespace Autofac.Extras.Moq.Test
{
    public class AutoMockFixture
    {
        public interface IInheritFromDisposable : IDisposable
        {
        }

        public interface IServiceA
        {
            void RunA();
        }

        public interface IServiceB
        {
            void RunB();
        }

        [Fact]
        public void AbstractDependencyIsFulfilled()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var component = mock.Create<TestComponentRequiringAbstractClassA>();
                Assert.Equal(
                    mock.Mock<AbstractClassA>().Object,
                    component.InstanceOfAbstractClassA);
            }
        }

        [Fact]
        public void BuildCallbackAllowsOverrides()
        {
            var service = new ServiceA();
            using (var mock = AutoMock.GetLoose(b => b.RegisterInstance(service).As<IServiceA>()))
            {
                var resolved = mock.Create<IServiceA>();
                Assert.Same(service, resolved);
            }
        }

        [Fact]
        public void CanProvideConcreteTypesWithoutDefaultConstructors()
        {
            // Issue #15
            // Dependency chains that have concrete types without default constructors
            // end up failing to be mocked because the DynamicProxy wants a zero-param ctor.
            using (var env = AutoMock.GetLoose())
            {
                // Shouldn't throw on resolve.
                var sut = env.Create<ConsumesConcreteTypeWithoutDefaultConstructor>();
            }
        }

        [Fact]
        public void DefaultConstructorIsLoose()
        {
            using (var mock = AutoMock.GetLoose())
            {
                RunWithSingleExpectationTest(mock);
            }
        }

        [Fact]
        public void DefaultConstructorWorksWithAllTests()
        {
            using (var mock = AutoMock.GetLoose())
            {
                RunTest(mock);
            }
        }

        [Fact]
        public void DisposableStrictMocking()
        {
            using (var mock = AutoMock.GetStrict())
            {
                // Should not throw on dispose of AutoMock.
                mock.Mock<IInheritFromDisposable>().Setup(x => x.Dispose());
                var sut = mock.Create<ConsumesDisposable>();
            }
        }

        [Fact]
        public void DisposableDoesntThrowIfNotSetupInStrictMocking()
        {
            using (var mock = AutoMock.GetStrict())
            {
                // No setup for strict mock on IDisposable
                // Should not throw on dispose of AutoMock.
                var sut = mock.Create<ConsumesDisposable>();
            }
        }

        [Fact]
        public void DisposableDoesThrowIfNotSetupAndDisposedBySutInStrictMocking()
        {
            using (var mock = AutoMock.GetStrict())
            {
                var sut = mock.Create<DisposesDisposable>();

                Assert.Throws<MockException>(() => sut.DisposeDependency());
            }
        }

        [Fact]
        public void DisposableDoesNotThrowIfSetupAndDisposedBySutInStrictMocking()
        {
            using (var mock = AutoMock.GetStrict())
            {
                mock.Mock<IInheritFromDisposable>().Setup(x => x.Dispose());
                var sut = mock.Create<DisposesDisposable>();

                // No throw
                sut.DisposeDependency();
            }
        }

        [Fact]
        public void DisposableDoesNotThrowWhenContainerIsDisposedWhenRegisteredManually()
        {
            var mock = new Mock<IDisposable>(MockBehavior.Strict);
            using (var automock = AutoMock.GetStrict(cfg => cfg.RegisterMock(mock)))
            {
                var sut = automock.Create<ConsumesDisposable>();

                // no throw.
            }
        }

        [Fact]
        public void ConcreteDependencyTypesThatImplementDisposableAreDisposedWhenContainerDisposes()
        {
            RequiresAConcreteDisposableType sut;
            using (var mock = AutoMock.GetStrict())
            {
                sut = mock.Create<RequiresAConcreteDisposableType>();

                Assert.False(sut.DependencyDisposed);
            }

            Assert.True(sut.DependencyDisposed);
        }

        [Fact]
        public void GetFromRepositoryUsesLooseBehaviorSetOnRepository()
        {
            using (var mock = AutoMock.GetFromRepository(new MockRepository(MockBehavior.Loose)))
            {
                RunWithSingleExpectationTest(mock);
            }
        }

        [Fact]
        public void GetFromRepositoryUsesStrictBehaviorSetOnRepository()
        {
            using (var mock = AutoMock.GetFromRepository(new MockRepository(MockBehavior.Strict)))
            {
                Assert.Throws<MockException>(() => RunWithSingleExpectationTest(mock));
            }
        }

        [Fact]
        public void LooseWorksWithUnmetExpectations()
        {
            using (var loose = AutoMock.GetLoose())
            {
                RunWithSingleExpectationTest(loose);
            }
        }

        [Fact]
        public void NormalExpectationsAreNotVerifiedByDefault()
        {
            using (var mock = AutoMock.GetLoose())
            {
                SetUpExpectations(mock);
            }
        }

        [Fact]
        public void ProperInitializationIsPerformed()
        {
            AssertProperties(AutoMock.GetLoose());
            AssertProperties(AutoMock.GetStrict());
        }

        [Fact]
        public void ProvideImplementation()
        {
            var newServiceA = new ServiceA();

            using (var mock = AutoMock.GetLoose(cfg => cfg.RegisterInstance(newServiceA).As<IServiceA>()))
            {
                var serviceA = mock.Create<IServiceA>();
                Assert.NotNull(serviceA);
                Assert.Same(newServiceA, serviceA);
            }
        }

        [Fact]
        public void ProvideInstance()
        {
            var mockA = new Mock<IServiceA>();
            mockA.Setup(x => x.RunA());
            using (var mock = AutoMock.GetLoose(cfg => cfg.RegisterMock(mockA)))
            {
                var component = mock.Create<TestComponent>();

                component.RunAll();
                mockA.VerifyAll();
            }
        }

        [Fact]
        public void ProvideInstanceAndResolve()
        {
            var serviceA = new ServiceA();
            using (var mock = AutoMock.GetLoose(cfg => cfg.RegisterInstance(serviceA).As<IServiceA>()))
            {
                var component = mock.Create<TestComponent>();

                component.RunAll();

                Assert.True(serviceA.WasRun);
            }
        }

        [Fact]
        public void RegularClassDependencyIsFulfilled()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var component = mock.Create<TestComponentRequiringClassA>();
                Assert.Equal(
                    mock.Mock<ClassA>().Object,
                    component.InstanceOfClassA);
            }
        }

        [Fact]
        public void StrictWorksWithAllExpectationsMet()
        {
            using (var strict = AutoMock.GetStrict())
            {
                RunTest(strict);
            }
        }

        [Fact]
        public void UnmetExpectationWithStrictMocksThrowsException()
        {
            using (var mock = AutoMock.GetStrict())
            {
                Assert.Throws<MockException>(() => RunWithSingleExpectationTest(mock));
            }
        }

        [Fact]
        public void UnmetVerifiableExpectationsCauseExceptionByDefault()
        {
            Assert.Throws<MockException>(() =>
                {
                    using (var mock = AutoMock.GetLoose())
                    {
                        SetUpVerifiableExpectations(mock);
                    }
                });
        }

        [Fact]
        public void VerifyAllSetTrue_ExpectationsAreVerified()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.VerifyAll = true;
                RunTest(mock);
            }
        }

        [Fact]
        public void VerifyAllSetTrue_UnmetExpectationsCauseException()
        {
            Assert.Throws<MockException>(() =>
                {
                    using (var mock = AutoMock.GetLoose())
                    {
                        mock.VerifyAll = true;
                        SetUpExpectations(mock);
                    }
                });
        }

        [Fact]
        public void LooseCreate_SuccessfulOnUnregisteredGeneric()
        {
            var mock = AutoMock.GetLoose();
            Assert.NotNull(mock.Create<GenericClassA<ClassA>>());
        }

        [Fact]
        public void MockNotAddedToEnumerableIfTypeIsRegistered()
        {
            var autoMock = AutoMock.GetLoose(c =>
            {
                c.RegisterType<TestA>().As<ITest>();
                c.RegisterType<TestB>().As<ITest>();

                c.RegisterType<TestClassConsumesIEnumerable>();
            });

            var wrapper = autoMock.Create<TestClassConsumesIEnumerable>();

            Assert.Equal(2, wrapper.All.Count());
        }

        [Fact]
        public void MockAddedToEnumerableIfNoTypeIsRegistered()
        {
            var autoMock = AutoMock.GetLoose();

            var wrapper = autoMock.Create<TestClassConsumesIEnumerable>();

            Assert.Single(wrapper.All);
        }

        [Fact]
        public void CreateClassWithParameter()
        {
            using (AutoMock autoMock = AutoMock.GetStrict())
            {
                var obj = autoMock.Create<ClassWithParameters>(new NamedParameter("param1", 10));
                Assert.NotNull(obj);
                Assert.True(obj.InvokedSimpleConstructor);
            }
        }

        [Fact]
        public void MockedClassWithConstructorThrows()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.Mock<IDependency>().Setup(s => s.DoSomethingExternal()).Returns(7);
                Assert.Throws<DependencyResolutionException>(() => mock.Mock<ClassWithDependency>());
            }
        }

        [Fact]
        public void CreateNonGenericSameAsCreateGeneric()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var generic = mock.Create<ServiceA>();
                var nonGeneric = mock.Create(typeof(ServiceA));

                Assert.Same(generic, nonGeneric);
            }
        }

        [Fact]
        public void ResolveInChildScope()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.Mock<IDependency>().Setup(o => o.DoSomethingExternal()).Returns(1);
                var rootScope = mock.Container;
                var childScope = rootScope.BeginLifetimeScope();

                var resolvedObject = childScope.Resolve<IDependency>();

                Assert.Equal(1, resolvedObject.DoSomethingExternal());
            }
        }

        public class ClassWithDependency
        {
            private readonly IDependency _dependency;

            public ClassWithDependency(IDependency dependency)
            {
                _dependency = dependency;
            }

            public int DoSomeThing() => 0;
        }

        public class Dependency : IDependency
        {
            public int DoSomethingExternal() => 0;
        }

        public interface IDependency
        {
            int DoSomethingExternal();
        }

        public class ClassWithParameters
        {
            public bool InvokedSimpleConstructor { get; }

            public ClassWithParameters(int param1)
                : this(param1, TimeSpan.Zero)
            {
                InvokedSimpleConstructor = true;
            }

            public ClassWithParameters(int param1, TimeSpan param2)
            {
            }
        }

        public interface ITest
        {
        }

        public class TestA : ITest
        {
        }

        public class TestB : ITest
        {
        }

        public class TestClassConsumesIEnumerable
        {
            public IEnumerable<ITest> All { get; }

            public TestClassConsumesIEnumerable(IEnumerable<ITest> all)
            {
                All = all;
            }
        }

        public class GenericClassA<T>
        {
            private readonly ClassA _dependency;

            public GenericClassA(ClassA dependency)
            {
                _dependency = dependency;
            }
        }

        private static void AssertProperties(AutoMock mock)
        {
            Assert.NotNull(mock.MockRepository);
        }

        private static void RunTest(AutoMock mock)
        {
            SetUpExpectations(mock);

            var component = mock.Create<TestComponent>();
            component.RunAll();
        }

        private static void RunWithSingleExpectationTest(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB());

            var component = mock.Create<TestComponent>();
            component.RunAll();
        }

        private static void SetUpExpectations(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB());
            mock.Mock<IServiceA>().Setup(x => x.RunA());
        }

        private static void SetUpVerifiableExpectations(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB()).Verifiable();
            mock.Mock<IServiceA>().Setup(x => x.RunA()).Verifiable();
        }

        public abstract class AbstractClassA
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class ClassA : AbstractClassA
        {
        }

        public class ConcreteTypeWithoutDefaultConstructor
        {
            private readonly ClassA _dependency;

            public ConcreteTypeWithoutDefaultConstructor(ClassA dependency)
            {
                _dependency = dependency;
            }
        }

        public class ConsumesDisposable
        {
            private readonly IInheritFromDisposable _disposable;

            public ConsumesDisposable(IInheritFromDisposable disposable)
            {
                _disposable = disposable;
            }
        }

        public class DisposesDisposable : IDisposable
        {
            private readonly IInheritFromDisposable _disposable;

            public DisposesDisposable(IInheritFromDisposable disposable)
            {
                _disposable = disposable;
            }

            public bool Disposed { get; private set; } = false;

            public void Dispose()
            {
                Disposed = true;
            }

            public void DisposeDependency()
            {
                _disposable.Dispose();
            }
        }

        public class RequiresAConcreteDisposableType
        {
            private readonly DisposesDisposable _disposesDisposable;

            public RequiresAConcreteDisposableType(DisposesDisposable disposesDisposable)
            {
                _disposesDisposable = disposesDisposable;
            }

            public bool DependencyDisposed => _disposesDisposable.Disposed;
        }

        public class ConsumesConcreteTypeWithoutDefaultConstructor
        {
            private readonly ConcreteTypeWithoutDefaultConstructor _dependency;

            public ConsumesConcreteTypeWithoutDefaultConstructor(ConcreteTypeWithoutDefaultConstructor dependency)
            {
                // Issue #15
                // The dependency chain here is important because the test involves verifying
                // that mocking can handle downstream concrete types that don't have default dependencies.
                _dependency = dependency;
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public class ServiceA : IServiceA
        {
            public bool WasRun { get; private set; }

            public void RunA()
            {
                WasRun = true;
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponent
        {
            private readonly IServiceA _serviceA;

            private readonly IServiceB _serviceB;

            public TestComponent(IServiceA serviceA, IServiceB serviceB)
            {
                _serviceA = serviceA;
                _serviceB = serviceB;
            }

            public void RunAll()
            {
                _serviceA.RunA();
                _serviceB.RunB();
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponentRequiringAbstractClassA
        {
            public TestComponentRequiringAbstractClassA(AbstractClassA abstractClassA)
            {
                InstanceOfAbstractClassA = abstractClassA;
            }

            public AbstractClassA InstanceOfAbstractClassA { get; }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponentRequiringClassA
        {
            public TestComponentRequiringClassA(ClassA classA)
            {
                InstanceOfClassA = classA;
            }

            public ClassA InstanceOfClassA { get; }
        }
    }
}
