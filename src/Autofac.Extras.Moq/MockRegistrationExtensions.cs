// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Builder;
using Moq;

namespace Autofac.Extras.Moq
{
    /// <summary>
    /// Provides mocking registration extensions.
    /// </summary>
    public static class MockRegistrationExtensions
    {
        /// <summary>
        /// Register a mock by explicitly providing a Mock instance for the service being mocked.
        /// </summary>
        /// <typeparam name="TMocked">The type of service.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="mock">The mock.</param>
        /// <returns>The registration builder.</returns>
        public static IRegistrationBuilder<TMocked, SimpleActivatorData, SingleRegistrationStyle> RegisterMock<TMocked>(this ContainerBuilder builder, Mock<TMocked> mock)
            where TMocked : class
        {
            if (mock is null)
            {
                throw new System.ArgumentNullException(nameof(mock));
            }

            return builder.RegisterInstance(mock.Object).As<TMocked>().ExternallyOwned();
        }
    }
}
