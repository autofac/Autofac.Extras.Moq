using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Autofac.Extras.Moq.Test
{
    public class AutoMockFixture
    {
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
        public void DefaultConstructorIsLoose()
        {
            using (var mock = AutoMock.GetLoose())
            {
                RunWithSingleSetupationTest(mock);
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
        public void GetFromRepositoryUsesLooseBehaviorSetOnRepository()
        {
            using (var mock = AutoMock.GetFromRepository(new MockRepository(MockBehavior.Loose)))
            {
                RunWithSingleSetupationTest(mock);
            }
        }

        [Fact]
        public void GetFromRepositoryUsesStrictBehaviorSetOnRepository()
        {
            using (var mock = AutoMock.GetFromRepository(new MockRepository(MockBehavior.Strict)))
            {
                Assert.Throws<MockException>(() => RunWithSingleSetupationTest(mock));
            }
        }

        [Fact]
        public void LooseWorksWithUnmetSetupations()
        {
            using (var loose = AutoMock.GetLoose())
            {
                RunWithSingleSetupationTest(loose);
            }
        }

        [Fact]
        public void NormalSetupationsAreNotVerifiedByDefault()
        {
            using (var mock = AutoMock.GetLoose())
            {
                SetUpSetupations(mock);
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
            using (var mock = AutoMock.GetLoose())
            {
                var serviceA = mock.Provide<IServiceA, ServiceA>();

                Assert.NotNull(serviceA);
                Assert.False(serviceA is IMocked<IServiceA>);
            }
        }

        [Fact]
        public void ProvideInstance()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var mockA = new Mock<IServiceA>();
                mockA.Setup(x => x.RunA());
                mock.Provide(mockA.Object);

                var component = mock.Create<TestComponent>();
                component.RunAll();

                mockA.VerifyAll();
            }
        }

        [Fact]
        public void ProvideCollection()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var mockC = new Mock<IServiceC>();
                mockC.Setup(x => x.RunC());
                mock.Provide(mockC.Object);

                var component = mock.Create<TestComponent>();
                component.RunAll();

                mockC.VerifyAll();
            }
        }

        [Fact]
        public void DoesNotAddMockToCollectionWhenServiceIsRegistered()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var builder = new ContainerBuilder();
                builder.RegisterType<ServiceC>().As<IServiceC>();
                builder.Update(mock.Container);

                var component = mock.Create<TestComponent>();

                Assert.Equal(1, component.ServiceCs.Count());
                Assert.IsAssignableFrom<ServiceC>(component.ServiceCs.First());
            }
        }

        [Fact]
        public void StrictWorksWithAllSetupationsMet()
        {
            using (var strict = AutoMock.GetStrict())
            {
                RunTest(strict);
            }
        }

        [Fact]
        public void UnmetSetupationWithStrictMocksThrowsException()
        {
            using (var mock = AutoMock.GetStrict())
            {
                Assert.Throws<MockException>(() => RunWithSingleSetupationTest(mock));
            }
        }

        [Fact]
        public void UnmetVerifiableSetupationsCauseExceptionByDefault()
        {
            Assert.Throws<MockException>(() =>
                {
                    using (var mock = AutoMock.GetLoose())
                    {
                        SetUpVerifableSetupations(mock);
                    }
                });
        }

        [Fact]
        public void VerifyAllSetTrue_SetupationsAreVerified()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.VerifyAll = true;
                RunTest(mock);
            }
        }

        [Fact]
        public void VerifyAllSetTrue_UnmetSetupationsCauseException()
        {
            Assert.Throws<MockException>(() =>
                {
                    using (var mock = AutoMock.GetLoose())
                    {
                        mock.VerifyAll = true;
                        SetUpSetupations(mock);
                    }
                });
        }

        private static void AssertProperties(AutoMock mock)
        {
            Assert.NotNull(mock.Container);
            Assert.NotNull(mock.MockRepository);
        }

        private static void RunTest(AutoMock mock)
        {
            SetUpSetupations(mock);

            var component = mock.Create<TestComponent>();
            component.RunAll();
        }

        private static void RunWithSingleSetupationTest(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB());

            var component = mock.Create<TestComponent>();
            component.RunAll();
        }

        private static void SetUpSetupations(AutoMock mock)
        {
            mock.Mock<IServiceC>().Setup(x => x.RunC());
            mock.Mock<IServiceB>().Setup(x => x.RunB());
            mock.Mock<IServiceA>().Setup(x => x.RunA());
        }

        private static void SetUpVerifableSetupations(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB()).Verifiable();
            mock.Mock<IServiceA>().Setup(x => x.RunA()).Verifiable();
        }

        public interface IServiceA
        {
            void RunA();
        }

        public interface IServiceB
        {
            void RunB();
        }

        public interface IServiceC
        {
            void RunC();
        }

        public abstract class AbstractClassA
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class ClassA : AbstractClassA
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public class ServiceA : IServiceA
        {
            public void RunA()
            {
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public class ServiceC : IServiceC
        {
            public void RunC()
            {
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponent
        {
            private readonly IServiceA _serviceA;

            private readonly IServiceB _serviceB;

            private readonly IEnumerable<IServiceC> _serviceCs;

            public TestComponent(IServiceA serviceA, IServiceB serviceB, IEnumerable<IServiceC> serviceCs)
            {
                this._serviceA = serviceA;
                this._serviceB = serviceB;
                this._serviceCs = serviceCs;
            }

            public void RunAll()
            {
                this._serviceA.RunA();
                this._serviceB.RunB();

                foreach (var serviceC in _serviceCs)
                {
                    serviceC.RunC();
                }
            }

            public IEnumerable<IServiceC> ServiceCs => _serviceCs;
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponentRequiringAbstractClassA
        {
            public TestComponentRequiringAbstractClassA(AbstractClassA abstractClassA)
            {
                this.InstanceOfAbstractClassA = abstractClassA;
            }

            public AbstractClassA InstanceOfAbstractClassA { get; }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponentRequiringClassA
        {
            public TestComponentRequiringClassA(ClassA classA)
            {
                this.InstanceOfClassA = classA;
            }

            public ClassA InstanceOfClassA { get; }
        }
    }
}
