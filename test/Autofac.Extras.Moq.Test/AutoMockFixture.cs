// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Autofac.Extras.Moq.Test.Stubs;

namespace Autofac.Extras.Moq.Test;

public class AutoMockFixture
{
    [Fact]
    public void AbstractDependencyIsFulfilled()
    {
        using (var mock = AutoMock.GetLoose())
        {
            var component = mock.Create<TestConsumesAbstractClass>();
            Assert.Equal(
                mock.Mock<TestAbstractClass>().Object,
                component.InstanceOfAbstractClass);
        }
    }

    [Fact]
    public void BuildCallbackAllowsOverrides()
    {
        var service = new TestImplementationOneA();
        using (var mock = AutoMock.GetLoose(b => b.RegisterInstance(service).As<ITestInterfaceOne>()))
        {
            var resolved = mock.Create<ITestInterfaceOne>();
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
            mock.Mock<ITestDisposable>().Setup(x => x.Dispose());
            var sut = mock.Create<ConsumesDisposable>();
        }
    }

    [Fact]
    public void DisposableDoesNotThrowIfNotSetupInStrictMocking()
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
            mock.Mock<ITestDisposable>().Setup(x => x.Dispose());
            var sut = mock.Create<DisposesDisposable>();

            // No throw
            sut.DisposeDependency();
        }
    }

    [Fact]
    public void DisposableDoesNotThrowWhenContainerIsDisposedWhenRegisteredManually()
    {
        var mock = new Mock<IDisposable>(MockBehavior.Strict);
        using (var autoMock = AutoMock.GetStrict(cfg => cfg.RegisterMock(mock)))
        {
            var sut = autoMock.Create<ConsumesDisposable>();

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
        Assert.NotNull(AutoMock.GetLoose().MockRepository);
        Assert.NotNull(AutoMock.GetStrict().MockRepository);
    }

    [Fact]
    public void ProvideImplementation()
    {
        var newServiceA = new TestImplementationOneA();

        using (var mock = AutoMock.GetLoose(cfg => cfg.RegisterInstance(newServiceA).As<ITestInterfaceOne>()))
        {
            var serviceA = mock.Create<ITestInterfaceOne>();
            Assert.NotNull(serviceA);
            Assert.Same(newServiceA, serviceA);
        }
    }

    [Fact]
    public void ProvideInstance()
    {
        var mockA = new Mock<ITestInterfaceOne>();
        mockA.Setup(x => x.RunOne());
        using (var mock = AutoMock.GetLoose(cfg => cfg.RegisterMock(mockA)))
        {
            var component = mock.Create<TestConsumesMultipleInterfaces>();

            component.RunAll();
            mockA.VerifyAll();
        }
    }

    [Fact]
    public void ProvideInstanceAndResolve()
    {
        var serviceA = new TestImplementationOneA();
        using (var mock = AutoMock.GetLoose(cfg => cfg.RegisterInstance(serviceA).As<ITestInterfaceOne>()))
        {
            var component = mock.Create<TestConsumesMultipleInterfaces>();

            component.RunAll();

            Assert.True(serviceA.WasRun);
        }
    }

    [Fact]
    public void RegularClassDependencyIsFulfilled()
    {
        using (var mock = AutoMock.GetLoose())
        {
            var component = mock.Create<TestConsumesConcreteClass>();
            Assert.Equal(
                mock.Mock<TestImplementationOneA>().Object,
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
        Assert.NotNull(mock.Create<TestGenericClass<TestImplementationOneA>>());
    }

    [Fact]
    public void MockNotAddedToEnumerableIfTypeIsRegistered()
    {
        var autoMock = AutoMock.GetLoose(c =>
        {
            c.RegisterType<TestImplementationOneA>().As<ITestInterfaceOne>();
            c.RegisterType<TestImplementationOneB>().As<ITestInterfaceOne>();

            c.RegisterType<TestConsumesEnumerable>();
        });

        var wrapper = autoMock.Create<TestConsumesEnumerable>();

        Assert.Equal(2, wrapper.All.Count());
    }

    [Fact]
    public void MockAddedToEnumerableIfNoTypeIsRegistered()
    {
        var autoMock = AutoMock.GetLoose();

        var wrapper = autoMock.Create<TestConsumesEnumerable>();

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
            mock.Mock<ITestInterfaceOne>().Setup(s => s.DoWork()).Returns(7);
            Assert.Throws<DependencyResolutionException>(() => mock.Mock<TestConsumesInterface>());
        }
    }

    [Fact]
    public void CreateNonGenericSameAsCreateGeneric()
    {
        using (var mock = AutoMock.GetLoose())
        {
            var generic = mock.Create<TestImplementationOneA>();
            var nonGeneric = mock.Create(typeof(TestImplementationOneA));

            Assert.Same(generic, nonGeneric);
        }
    }

    [Fact]
    public void ResolveInChildScope()
    {
        using (var mock = AutoMock.GetLoose())
        {
            mock.Mock<ITestInterfaceOne>().Setup(o => o.DoWork()).Returns(1);
            var rootScope = mock.Container;
            var childScope = rootScope.BeginLifetimeScope();

            var resolvedObject = childScope.Resolve<ITestInterfaceOne>();

            Assert.Equal(1, resolvedObject.DoWork());
        }
    }

    private static void RunTest(AutoMock mock)
    {
        SetUpExpectations(mock);

        var component = mock.Create<TestConsumesMultipleInterfaces>();
        component.RunAll();
    }

    private static void RunWithSingleExpectationTest(AutoMock mock)
    {
        mock.Mock<ITestInterfaceTwo>().Setup(x => x.RunTwo());

        var component = mock.Create<TestConsumesMultipleInterfaces>();
        component.RunAll();
    }

    private static void SetUpExpectations(AutoMock mock)
    {
        mock.Mock<ITestInterfaceTwo>().Setup(x => x.RunTwo());
        mock.Mock<ITestInterfaceOne>().Setup(x => x.RunOne());
    }

    private static void SetUpVerifiableExpectations(AutoMock mock)
    {
        mock.Mock<ITestInterfaceTwo>().Setup(x => x.RunTwo()).Verifiable();
        mock.Mock<ITestInterfaceOne>().Setup(x => x.RunOne()).Verifiable();
    }

    internal class ClassWithParameters
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

    internal class ConsumesDisposable
    {
        private readonly ITestDisposable _disposable;

        public ConsumesDisposable(ITestDisposable disposable)
        {
            _disposable = disposable;
        }
    }

    internal class DisposesDisposable : IDisposable
    {
        private readonly ITestDisposable _disposable;

        public DisposesDisposable(ITestDisposable disposable)
        {
            _disposable = disposable;
        }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            // Intentionally does not dispose of the dependency. Part of the
            // testing is to verify what happens when the dependency is not
            // disposed.
            Disposed = true;
        }

        public void DisposeDependency()
        {
            _disposable.Dispose();
        }
    }

    internal class RequiresAConcreteDisposableType
    {
        private readonly DisposesDisposable _disposesDisposable;

        public RequiresAConcreteDisposableType(DisposesDisposable disposesDisposable)
        {
            _disposesDisposable = disposesDisposable;
        }

        public bool DependencyDisposed => _disposesDisposable.Disposed;
    }

    internal class ConsumesConcreteTypeWithoutDefaultConstructor
    {
        private readonly TestConsumesConcreteClass _dependency;

        public ConsumesConcreteTypeWithoutDefaultConstructor(TestConsumesConcreteClass dependency)
        {
            // Issue #15
            // The dependency chain here is important because the test involves verifying
            // that mocking can handle downstream concrete types that don't have default dependencies.
            _dependency = dependency;
        }
    }
}
