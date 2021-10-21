// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// Wrapper around <see cref="Autofac"/> and <see cref="Moq"/>.
    /// </summary>
    public class AutoMock : IDisposable
    {
        private bool _disposed;

        private readonly HashSet<Type> _createdServiceTypes = new HashSet<Type>();
        private readonly HashSet<Type> _mockedServiceTypes = new HashSet<Type>();

        private AutoMock(MockBehavior behavior, Action<ContainerBuilder> beforeBuild)
            : this(new MockRepository(behavior), beforeBuild)
        {
        }

        private AutoMock(MockRepository repository, Action<ContainerBuilder> beforeBuild)
        {
            MockRepository = repository;
            var builder = new ContainerBuilder();
            builder.RegisterInstance(MockRepository);

            // The action happens after instance registrations but before source registrations
            // to avoid issues like ContravariantRegistrationSource order challenges. ACTNARS
            // and Moq being last in are least likely to cause ordering conflicts.
            beforeBuild?.Invoke(builder);

            builder.RegisterSource(new MoqRegistrationHandler(_createdServiceTypes, _mockedServiceTypes));

            Container = builder.Build();

            VerifyAll = false;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AutoMock"/> class.
        /// </summary>
        ~AutoMock()
        {
            Dispose(false);
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
        /// <param name="beforeBuild">Callback before container was created, you can add your own components here.</param>
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
        /// <param name="beforeBuild">Callback before container was created, you can add your own components here.</param>
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
        /// <returns>Container initialized for strict behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static AutoMock GetStrict()
        {
            return new AutoMock(MockBehavior.Strict, null);
        }

        /// <summary>
        /// Create new <see cref="AutoMock"/> instance with strict mock behavior.
        /// </summary>
        /// <param name="beforeBuild">Callback before container was created, you can add your own components here.</param>
        /// <seealso cref="MockRepository"/>
        /// <returns>Container initialized for strict behavior.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static AutoMock GetStrict(Action<ContainerBuilder> beforeBuild)
        {
            return new AutoMock(MockBehavior.Strict, beforeBuild);
        }

        /// <summary>
        /// Resolve the specified type in the container (register it if needed).
        /// </summary>
        /// <typeparam name="T">Service.</typeparam>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>The service.</returns>
        public T Create<T>(params Parameter[] parameters)
        {
            return Create<T>(false, parameters);
        }

        /// <summary>
        /// Resolve the specified type in the container (register it if needed).
        /// </summary>
        /// <param name="serviceType">Type of service.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>The service.</returns>
        public object Create(Type serviceType, params Parameter[] parameters)
        {
            return Create(false, serviceType, parameters);
        }

        /// <summary>
        /// Verifies mocks and disposes internal container.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finds (creating if needed) the actual mock for the provided type.
        /// </summary>
        /// <typeparam name="T">Type to mock.</typeparam>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>A mock of type <typeparamref name="T"/>.</returns>
        public Mock<T> Mock<T>(params Parameter[] parameters)
            where T : class
        {
            var obj = (IMocked<T>)Create<T>(true, parameters);
            return obj.Mock;
        }

        private object Create(bool isMock, Type serviceType, params Parameter[] parameters)
        {
            if (isMock)
            {
                _mockedServiceTypes.Add(serviceType);
            }
            else
            {
                _createdServiceTypes.Add(serviceType);
            }

            return Container.Resolve(serviceType, parameters);
        }

        private T Create<T>(bool isMock, params Parameter[] parameters)
        {
            return (T)Create(isMock, typeof(T), parameters);
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
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // We can only verify things with the mock
                    // repository if it hasn't already been garbage
                    // collected during finalization.
                    try
                    {
                        if (VerifyAll)
                        {
                            MockRepository.VerifyAll();
                        }
                        else
                        {
                            MockRepository.Verify();
                        }
                    }
                    finally
                    {
                        Container.Dispose();
                    }
                }

                _disposed = true;
            }
        }
    }
}
