// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;
using Moq;

namespace Autofac.Extras.Moq;

/// <summary>
/// Resolves unknown interfaces and mocks using the <see cref="MockRepository"/> from the scope.
/// </summary>
internal class MoqRegistrationHandler : IRegistrationSource
{
    private readonly ISet<Type> _createdServiceTypes;
    private readonly ISet<Type> _mockedServiceTypes;

    /// <summary>
    /// This is <see cref="MockFactory.Create{T}()"/> with zero parameters. This
    /// is important because it limits what can be auto-mocked. (MockFactory got
    /// renamed to MockRepository but the method reference is still internally
    /// on MockFactory.)
    /// </summary>
    private readonly MethodInfo _createMethod = typeof(MockRepository).GetMethod(nameof(MockRepository.Create), Array.Empty<Type>()) ?? throw new NotSupportedException("Unable to bind to Create method.");

    /// <summary>
    /// This is <see cref="MockFactory.Create{T}(object[])"/> which forwards
    /// constructor arguments to the mocked type. It is used when the caller
    /// supplies parameters (issue #42) so abstract/class mocks can be created
    /// using a constructor that takes arguments.
    /// </summary>
    private readonly MethodInfo _createWithArgsMethod = typeof(MockRepository).GetMethod(nameof(MockRepository.Create), new[] { typeof(object[]) }) ?? throw new NotSupportedException("Unable to bind to Create method with constructor arguments.");

    /// <summary>
    /// Initializes a new instance of the <see cref="MoqRegistrationHandler"/> class.
    /// </summary>
    /// <param name="createdServiceTypes">A set of root services that have been created.</param>
    /// <param name="mockedServiceTypes">A set of mocks that have been explicitly configured.</param>
    public MoqRegistrationHandler(ISet<Type> createdServiceTypes, ISet<Type> mockedServiceTypes)
    {
        _createdServiceTypes = createdServiceTypes;
        _mockedServiceTypes = mockedServiceTypes;
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
        Func<Service, IEnumerable<ServiceRegistration>>? registrationAccessor)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        var typedService = service as TypedService;

        IComponentRegistration? result;

        // Manually registered, don't do ourselves.
        if (typedService == null || (registrationAccessor is not null && registrationAccessor(service).Any()))
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
                result = RegistrationBuilder.ForDelegate((c, p) => CreateMock(c, typedService, p))
                                         .As(service)
                                         .SingleInstance()
                                         .ExternallyOwned()
                                         .CreateRegistration();
            }
            else if (ServiceCompatibleWithAutomaticDirectRegistration(typedService))
            {
                // Issue #15 - Incompatible mocks need to be registered using the type.
                // Their constructor dependencies will then be mocked.
                result = RegistrationBuilder.ForType(typedService.ServiceType)
                                        .InstancePerLifetimeScope()
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

    private static bool IsIEnumerable(TypedService typedService)
    {
        // We handle most generics, but we don't handle IEnumerable because that has special
        // meaning in Autofac
        return typedService.ServiceType.GetTypeInfo().IsGenericType &&
               typedService.ServiceType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>);
    }

    private static bool IsIStartable(TypedService typedService)
    {
        return typeof(IStartable).IsAssignableFrom(typedService.ServiceType);
    }

    private static bool IsInsideAutofac(TypedService typedService)
    {
        return typeof(IRegistrationSource).Assembly == typedService.ServiceType.Assembly;
    }

    private static bool ServiceCompatibleWithAutomaticDirectRegistration(TypedService typedService)
    {
        var serviceType = typedService.ServiceType;

        return serviceType.IsClass &&
               serviceType != typeof(string) &&
               !serviceType.IsSubclassOf(typeof(Delegate)) &&
               !serviceType.IsAbstract &&
               !serviceType.IsGenericTypeDefinition;
    }

    private static bool ServiceCompatibleWithMockRepositoryCreate(TypedService typedService)
    {
        var serverTypeInfo = typedService.ServiceType.GetTypeInfo();

        // Issue #15: Ensure there's a zero-parameter ctor or the DynamicProxy under Moq fails.
        return serverTypeInfo.IsInterface
            || serverTypeInfo.IsAbstract
            || (serverTypeInfo.IsClass &&
                !serverTypeInfo.IsSealed &&
                typedService.ServiceType.GetConstructors().Any(c => c.GetParameters().Length == 0));
    }

    private static bool ShouldMockService(TypedService typedService)
    {
        return !IsIEnumerable(typedService) &&
               !IsIStartable(typedService) &&
               !IsInsideAutofac(typedService) &&
               !IsLazy(typedService) &&
               !IsOwned(typedService) &&
               !IsMeta(typedService);
    }

    private static bool IsLazy(TypedService typedService)
    {
        // We handle most generics, but we don't handle Lazy because that has special
        // meaning in Autofac
        var typeInfo = typedService.ServiceType.GetTypeInfo();
        return typeInfo.IsGenericType &&
               typeInfo.GetGenericTypeDefinition() == typeof(Lazy<>);
    }

    private static bool IsOwned(TypedService typedService)
    {
        // We handle most generics, but we don't handle Owned because that has special
        // meaning in Autofac
        var typeInfo = typedService.ServiceType.GetTypeInfo();
        return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Owned<>);
    }

    private static bool IsMeta(TypedService typedService)
    {
        // We handle most generics, but we don't handle Meta because that has special
        // meaning in Autofac
        var typeInfo = typedService.ServiceType.GetTypeInfo();
        return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Meta<>);
    }

    /// <summary>
    /// Maps the supplied Autofac parameters onto the constructor arguments of
    /// the type being mocked. Supports <see cref="TypedParameter"/>,
    /// <see cref="NamedParameter"/> and <see cref="PositionalParameter"/> (and
    /// any other <see cref="Parameter"/>) by delegating to the same
    /// <see cref="Parameter.CanSupplyValue"/> logic Autofac uses for normal
    /// constructor injection.
    /// </summary>
    /// <param name="serviceType">The type being mocked.</param>
    /// <param name="parameters">The parameters supplied by the caller.</param>
    /// <param name="context">The component context used to resolve values.</param>
    /// <returns>
    /// The constructor arguments in positional order, or an empty array if no
    /// constructor can be fully satisfied by the supplied parameters.
    /// </returns>
    private static object?[] BuildConstructorArguments(Type serviceType, Parameter[] parameters, IComponentContext context)
    {
        // Include non-public constructors: abstract classes commonly expose a
        // protected constructor, and Moq/Castle can use it.
        var constructors = serviceType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object?[]? bestMatch = null;

        foreach (var constructor in constructors)
        {
            var constructorParameters = constructor.GetParameters();

            // Prefer the constructor with the most parameters that can be fully
            // satisfied by the supplied values.
            if (constructorParameters.Length > (bestMatch?.Length ?? 0) &&
                TryBuildArguments(constructorParameters, parameters, context, out var arguments))
            {
                bestMatch = arguments;
            }
        }

        return bestMatch ?? Array.Empty<object?>();
    }

    /// <summary>
    /// Attempts to build the positional argument list for a single constructor
    /// from the supplied parameters.
    /// </summary>
    /// <param name="constructorParameters">The constructor's parameters.</param>
    /// <param name="parameters">The parameters supplied by the caller.</param>
    /// <param name="context">The component context used to resolve values.</param>
    /// <param name="arguments">The resolved positional arguments, if successful.</param>
    /// <returns>
    /// <see langword="true" /> if every constructor parameter could be supplied
    /// a value; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryBuildArguments(ParameterInfo[] constructorParameters, Parameter[] parameters, IComponentContext context, out object?[] arguments)
    {
        arguments = new object?[constructorParameters.Length];

        for (var i = 0; i < constructorParameters.Length; i++)
        {
            if (!TrySupplyValue(constructorParameters[i], parameters, context, out arguments[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Finds the first supplied parameter able to provide a value for the given
    /// constructor parameter.
    /// </summary>
    /// <param name="constructorParameter">The constructor parameter to satisfy.</param>
    /// <param name="parameters">The parameters supplied by the caller.</param>
    /// <param name="context">The component context used to resolve values.</param>
    /// <param name="value">The resolved value, if one was supplied.</param>
    /// <returns>
    /// <see langword="true" /> if a value was supplied; otherwise <see langword="false" />.
    /// </returns>
    private static bool TrySupplyValue(ParameterInfo constructorParameter, Parameter[] parameters, IComponentContext context, out object? value)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.CanSupplyValue(constructorParameter, context, out var valueProvider))
            {
                value = valueProvider();
                return true;
            }
        }

        value = null;
        return false;
    }

    private bool ServiceManuallyCreated(TypedService typedService)
    {
        return _createdServiceTypes.Contains(typedService.ServiceType);
    }

    /// <summary>
    /// Creates a mock object.
    /// </summary>
    /// <param name="context">The component context.</param>
    /// <param name="typedService">The typed service.</param>
    /// <param name="parameters">
    /// The parameters supplied when the mock was requested. When present, they
    /// are forwarded as constructor arguments to the mocked type (issue #42).
    /// </param>
    /// <returns>
    /// The mock object from the repository.
    /// </returns>
    private object CreateMock(IComponentContext context, TypedService typedService, IEnumerable<Parameter> parameters)
    {
        try
        {
            var repository = context.Resolve<MockRepository>();
            var parameterArray = parameters as Parameter[] ?? parameters.ToArray();
            var constructorArguments = parameterArray.Length == 0
                ? Array.Empty<object?>()
                : BuildConstructorArguments(typedService.ServiceType, parameterArray, context);

            Mock mock;
            if (constructorArguments.Length == 0)
            {
                // No usable constructor arguments: use the parameterless Create<T>()
                // so behavior is identical to a mock requested without parameters.
                var specificCreateMethod = _createMethod.MakeGenericMethod(typedService.ServiceType);
                mock = (Mock)specificCreateMethod.Invoke(repository, null)!;
            }
            else
            {
                // Forward the constructor arguments to Moq's Create<T>(object[]).
                var specificCreateMethod = _createWithArgsMethod.MakeGenericMethod(typedService.ServiceType);
                mock = (Mock)specificCreateMethod.Invoke(repository, new object[] { constructorArguments })!;
            }

            return mock.Object;
        }
        catch (TargetInvocationException ex)
        {
            // Expose the inner exception as if it was directly thrown.
            ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

            // Won't get here, but the compiler doesn't know that.
            throw ex.InnerException;
        }
    }
}
