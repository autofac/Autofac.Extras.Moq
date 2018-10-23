// This software is part of the Autofac IoC container
// Copyright (c) 2013 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features.ResolveAnything;
using Moq;

namespace Autofac.Extras.Moq
{
    /// <summary>
    /// Wrapper around <see cref="Autofac"/> and <see cref="Moq"/>
    /// </summary>
    public class AutoMock : IDisposable
    {
        private bool _disposed;

        private readonly List<Type> _createdServiceTypes = new List<Type>();

        private AutoMock(MockBehavior behavior, Action<ContainerBuilder> beforeBuild)
            : this(new MockRepository(behavior), beforeBuild)
        {
        }

        private AutoMock(MockRepository repository, Action<ContainerBuilder> beforeBuild)
        {
            this.MockRepository = repository;
            var builder = new ContainerBuilder();
            builder.RegisterInstance(this.MockRepository);
            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            builder.RegisterSource(new MoqRegistrationHandler(_createdServiceTypes));
            beforeBuild?.Invoke(builder);
            this.Container = builder.Build();
            this.VerifyAll = false;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AutoMock"/> class.
        /// </summary>
        ~AutoMock()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the <see cref="IContainer"/> that handles the component resolution.
        /// </summary>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Gets the <see cref="MockRepository"/> instance responsible for expectations and mocks.
        /// </summary>
        public MockRepository MockRepository { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether all mocks are verified.
        /// </summary>
        /// <value>
        /// <see langword="true" /> to verify all mocks; <see langword="false" />
        /// (default) to verify only mocks marked Verifiable.
        /// </value>
        public bool VerifyAll { get; set; }

        /// <summary>
        /// Create new <see cref="AutoMock"/> instance that will create mocks with behavior defined by a repository.
        /// </summary>
        /// <param name="repository">The repository that defines the behavior. </param>
        /// <returns>
        /// An <see cref="AutoMock"/> based on the provided <see cref="MockRepository"/>.
        /// </returns>
        public static AutoMock GetFromRepository(MockRepository repository)
        {
            return new AutoMock(repository, null);
        }

        /// <summary>
        /// Create new <see cref="AutoMock"/> instance that will create mocks with behavior defined by a repository.
        /// </summary>
        /// <param name="repository">The repository that defines the behavior. </param>
        /// <param name="beforeBuild">Callback before container was created, you can add your own components here</param>
        /// <returns>
        /// An <see cref="AutoMock"/> based on the provided <see cref="MockRepository"/>.
        /// </returns>
        public static AutoMock GetFromRepository(MockRepository repository, Action<ContainerBuilder> beforeBuild)
        {
            return new AutoMock(repository, beforeBuild);
        }

        /// <summary>
        /// Create new <see cref="AutoMock"/> instance with loose mock behavior.
        /// </summary>
        /// <returns>Container initialized for loose behavior.</returns>
        /// <seealso cref="MockRepository"/>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static AutoMock GetLoose()
        {
            return new AutoMock(MockBehavior.Loose, null);
        }

        /// <summary>
        /// Create new <see cref="AutoMock"/> instance with loose mock behavior.
        /// </summary>
        /// <param name="beforeBuild">Callback before container was created, you can add your own components here</param>
        /// <returns>Container initialized for loose behavior.</returns>
        /// <seealso cref="MockRepository"/>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static AutoMock GetLoose(Action<ContainerBuilder> beforeBuild)
        {
            return new AutoMock(MockBehavior.Loose, beforeBuild);
        }

        /// <summary>
        /// Create new <see cref="AutoMock"/> instance with strict mock behavior.
        /// </summary>
        /// <seealso cref="MockRepository"/>
        /// <returns>Container initialized for loose behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static AutoMock GetStrict()
        {
            return new AutoMock(MockBehavior.Strict, null);
        }

        /// <summary>
        /// Create new <see cref="AutoMock"/> instance with strict mock behavior.
        /// </summary>
        /// <param name="beforeBuild">Callback before container was created, you can add your own components here</param>
        /// <seealso cref="MockRepository"/>
        /// <returns>Container initialized for loose behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static AutoMock GetStrict(Action<ContainerBuilder> beforeBuild)
        {
            return new AutoMock(MockBehavior.Strict, beforeBuild);
        }

        /// <summary>
        /// Resolve the specified type in the container (register it if needed)
        /// </summary>
        /// <typeparam name="T">Service</typeparam>
        /// <param name="parameters">Optional parameters</param>
        /// <returns>The service.</returns>
        public T Create<T>(params Parameter[] parameters)
        {
            return Create<T>(false, parameters);
        }

        /// <summary>
        /// Verifies mocks and disposes internal container.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finds (creating if needed) the actual mock for the provided type
        /// </summary>
        /// <typeparam name="T">Type to mock</typeparam>
        /// <param name="parameters">Optional parameters</param>
        /// <returns>A mock of type <typeparamref name="T"/>.</returns>
        public Mock<T> Mock<T>(params Parameter[] parameters)
            where T : class
        {
            var obj = (IMocked<T>)Create<T>(true, parameters);
            return obj.Mock;
        }

        /// <summary>
        /// Resolve the specified type in the container (register it if needed)
        /// </summary>
        /// <typeparam name="TService">The type of service being provided.</typeparam>
        /// <typeparam name="TImplementation">The implementation of the service.</typeparam>
        /// <param name="parameters">Optional parameters</param>
        /// <returns>The service.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The component registry is responsible for registration disposal.")]
        public TService Provide<TService, TImplementation>(params Parameter[] parameters)
        {
            this.Container.ComponentRegistry.Register(
                            RegistrationBuilder.ForType<TImplementation>().As<TService>().InstancePerLifetimeScope().CreateRegistration());

            return this.Container.Resolve<TService>(parameters);
        }

        /// <summary>
        /// Resolve the specified type in the container (register specified instance if needed)
        /// </summary>
        /// <typeparam name="TService">The type of service being provided.</typeparam>
        /// <param name="instance">The instance to provide.</param>
        /// <returns>The instance resolved from container.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The component registry is responsible for registration disposal.")]
        public TService Provide<TService>(TService instance)
            where TService : class
        {
            this.Container.ComponentRegistry.Register(
                            RegistrationBuilder.ForDelegate((c, p) => instance).InstancePerLifetimeScope().CreateRegistration());

            return this.Container.Resolve<TService>();
        }

        private T Create<T>(bool isMock, params Parameter[] parameters)
        {
            if (!isMock && !_createdServiceTypes.Contains(typeof(T)))
                _createdServiceTypes.Add(typeof(T));

            return this.Container.Resolve<T>(parameters);
        }

        /// <summary>
        /// Handles disposal of managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to dispose of managed resources (during a manual execution
        /// of <see cref="Autofac.Extras.Moq.AutoMock.Dispose()"/>); or
        /// <see langword="false" /> if this is getting run as part of finalization where
        /// managed resources may have already been cleaned up.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // We can only verify things with the mock
                    // repository if it hasn't already been garbage
                    // collected during finalization.
                    try
                    {
                        if (this.VerifyAll)
                        {
                            this.MockRepository.VerifyAll();
                        }
                        else
                        {
                            this.MockRepository.Verify();
                        }
                    }
                    finally
                    {
                        this.Container.Dispose();
                    }
                }

                this._disposed = true;
            }
        }
    }
}
