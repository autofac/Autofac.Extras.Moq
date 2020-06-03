// This software is part of the Autofac IoC container
// Copyright (c) 2020 Autofac Contributors
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
