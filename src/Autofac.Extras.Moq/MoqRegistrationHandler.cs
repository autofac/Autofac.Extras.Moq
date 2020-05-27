// This software is part of the Autofac IoC container
// Copyright (c) 2007-2008 Autofac Contributors
// https://autofac.org
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
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;
using Moq;

namespace Autofac.Extras.Moq
{
    /// <summary>
    /// Resolves unknown interfaces and mocks using the <see cref="MockRepository"/> from the scope.
    /// </summary>
    internal class MoqRegistrationHandler : IRegistrationSource
    {
        private readonly ISet<Type> _createdServiceTypes;
        private readonly ISet<Type> _mockedServiceTypes;

        private readonly MethodInfo _createMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoqRegistrationHandler"/> class.
        /// </summary>
        /// <param name="createdServiceTypes">A set of root services that have been created.</param>
        /// <param name="mockedServiceTypes">A set of mocks that have been explicitly configured.</param>
        public MoqRegistrationHandler(ISet<Type> createdServiceTypes, ISet<Type> mockedServiceTypes)
        {
            this._createdServiceTypes = createdServiceTypes;
            this._mockedServiceTypes = mockedServiceTypes;

            // This is MockRepository.Create<T>() with zero parameters. This is important because
            // it limits what can be auto-mocked.
            var factoryType = typeof(MockRepository);
            this._createMethod = factoryType.GetMethod(nameof(MockRepository.Create), Array.Empty<Type>());
        }

        /// <summary>
        /// Gets a value indicating whether the registrations provided by
        /// this source are 1:1 adapters on top of other components (i.e. like Meta, Func or Owned).
        /// </summary>
        /// <value>
        /// Always returns <see langword="false" />.
        /// </value>
        public bool IsAdapterForIndividualComponents => false;

        /// <summary>
        /// Retrieve a registration for an unregistered service, to be used
        /// by the container.
        /// </summary>
        /// <param name="service">The service that was requested.</param>
        /// <param name="registrationAccessor">Not used; required by the interface.</param>
        /// <returns>
        /// Registrations for the service.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="service" /> is <see langword="null" />.
        /// </exception>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Registry handles disposal")]
        public IEnumerable<IComponentRegistration> RegistrationsFor(
            Service service,
            Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var typedService = service as TypedService;

            IComponentRegistration result;

            // Manually registered, don't do ourselves.
            if (typedService == null || registrationAccessor(service).Any())
            {
                result = null;
            }
            else if (ServiceManuallyCreated(typedService))
            {
                result = RegistrationBuilder.ForType(typedService.ServiceType)
                                            .InstancePerLifetimeScope()
                                            .CreateRegistration();
            }
            else if (ShouldMockService(typedService))
            {
                // If a mock has been explicitly requested, then always try it.
                // This will ensure mocking exceptions get properly thrown.
                if (_mockedServiceTypes.Contains(typedService.ServiceType) || ServiceCompatibleWithMockRepositoryCreate(typedService))
                {
                    result = RegistrationBuilder.ForDelegate((c, p) => this.CreateMock(c, typedService))
                                             .As(service)
                                             .InstancePerLifetimeScope()
                                             .ExternallyOwned()
                                             .CreateRegistration();
                }
                else if (ServiceCompatibleWithAutomaticDirectRegistration(typedService))
                {
                    // Issue #15 - Incompatible mocks need to be registered using the type.
                    // Their constructor dependencies will then be mocked.
                    result = RegistrationBuilder.ForType(typedService.ServiceType)
                                            .InstancePerLifetimeScope()
                                            .ExternallyOwned()
                                            .CreateRegistration();
                }
                else
                {
                    result = null;
                }
            }
            else
            {
                result = null;
            }

            if (result is null)
            {
                return Enumerable.Empty<IComponentRegistration>();
            }

            return new[] { result };
        }

        private static bool IsIEnumerable(IServiceWithType typedService)
        {
            // We handle most generics, but we don't handle IEnumerable because that has special
            // meaning in Autofac
            return typedService.ServiceType.GetTypeInfo().IsGenericType &&
                   typedService.ServiceType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        private static bool IsIStartable(IServiceWithType typedService)
        {
            return typeof(IStartable).IsAssignableFrom(typedService.ServiceType);
        }

        private static bool IsInsideAutofac(IServiceWithType typedService)
        {
            return typeof(IRegistrationSource).Assembly == typedService.ServiceType.Assembly;
        }

        private static bool ServiceCompatibleWithAutomaticDirectRegistration(IServiceWithType typedService)
        {
            var serviceType = typedService.ServiceType;

            return serviceType.IsClass &&
                   serviceType != typeof(string) &&
                   !serviceType.IsSubclassOf(typeof(Delegate)) &&
                   !serviceType.IsAbstract &&
                   !serviceType.IsGenericTypeDefinition;
        }

        private static bool ServiceCompatibleWithMockRepositoryCreate(IServiceWithType typedService)
        {
            var serverTypeInfo = typedService.ServiceType.GetTypeInfo();

            // Issue #15: Ensure there's a zero-parameter ctor or the DynamicProxy under Moq fails.
            return serverTypeInfo.IsInterface
                || serverTypeInfo.IsAbstract
                || (serverTypeInfo.IsClass &&
                    !serverTypeInfo.IsSealed &&
                    typedService.ServiceType.GetConstructors().Any(c => c.GetParameters().Length == 0));
        }

        private bool ShouldMockService(IServiceWithType typedService)
        {
            return !IsIEnumerable(typedService) &&
                   !IsIStartable(typedService) &&
                   !IsInsideAutofac(typedService) &&
                   !IsLazy(typedService) &&
                   !IsOwned(typedService) &&
                   !IsMeta(typedService);
        }

        private bool ServiceManuallyCreated(IServiceWithType typedService)
        {
            return this._createdServiceTypes.Contains(typedService.ServiceType);
        }

        private static bool IsLazy(IServiceWithType typedService)
        {
            // We handle most generics, but we don't handle Lazy because that has special
            // meaning in Autofac
            var typeInfo = typedService.ServiceType.GetTypeInfo();
            return typeInfo.IsGenericType &&
                   typeInfo.GetGenericTypeDefinition() == typeof(Lazy<>);
        }

        private static bool IsOwned(IServiceWithType typedService)
        {
            // We handle most generics, but we don't handle Owned because that has special
            // meaning in Autofac
            var typeInfo = typedService.ServiceType.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Owned<>);
        }

        private static bool IsMeta(IServiceWithType typedService)
        {
            // We handle most generics, but we don't handle Meta because that has special
            // meaning in Autofac
            var typeInfo = typedService.ServiceType.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Meta<>);
        }

        /// <summary>
        /// Creates a mock object.
        /// </summary>
        /// <param name="context">The component context.</param>
        /// <param name="typedService">The typed service.</param>
        /// <returns>
        /// The mock object from the repository.
        /// </returns>
        private object CreateMock(IComponentContext context, TypedService typedService)
        {
            try
            {
                var specificCreateMethod = this._createMethod.MakeGenericMethod(new[] { typedService.ServiceType });
                var mock = (Mock)specificCreateMethod.Invoke(context.Resolve<MockRepository>(), null);
                return mock.Object;
            }
            catch (TargetInvocationException ex)
            {
                // Expose the inner exception as if it was directly thrown.
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                // Won't get here, but the compiler doesn't know that.
                throw ex.InnerException;
            }
        }
    }
}
