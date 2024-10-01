# Autofac.Extras.Moq

Moq auto mocking integration for [Autofac](https://github.com/autofac/Autofac).

[![Build status](https://ci.appveyor.com/api/projects/status/8c7natm3bsmn7ebx?svg=true)](https://ci.appveyor.com/project/Autofac/autofac-extras-moq) [![codecov](https://codecov.io/gh/Autofac/Autofac.Extras.Moq/branch/develop/graph/badge.svg)](https://app.codecov.io/gh/autofac/Autofac.Extras.Moq) [![NuGet](https://img.shields.io/nuget/v/Autofac.Extras.Moq.svg)](https://nuget.org/packages/Autofac.Extras.Moq)

Please file issues and pull requests for this package in this repository rather than in the Autofac core repo.

- [Documentation](https://autofac.readthedocs.io/en/latest/integration/moq.html)
- [NuGet](https://www.nuget.org/packages/Autofac.Extras.Moq)
- [Contributing](https://autofac.readthedocs.io/en/latest/contributors.html)
- [Open in Visual Studio Code](https://open.vscode.dev/autofac/Autofac.Extras.Moq)

> :warning: **LOOKING FOR AN OWNER!** This package is largely in maintenance mode - if you'd like to help the community out and pull it out of maintenance mode, [come drop us a line!](https://github.com/autofac/Autofac.Extras.Moq/issues/50)

## Quick Start

Given you have a system under test and a dependency:

```c#
public class SystemUnderTest
{
  public SystemUnderTest(IDependency dependency)
  {
  }
}

public interface IDependency
{
}
```

When writing your unit test, use the `Autofac.Extras.Moq.AutoMock` class to instantiate the system under test. Doing this will automatically inject a mock dependency into the constructor for you. At the time you create the `AutoMock` factory, you can specify default mock behavior:

- `AutoMock.GetLoose()` - creates automatic mocks using loose mocking behavior.
- `AutoMock.GetStrict()` - creates automatic mocks using strict mocking behavior.
- `AutoMock.GetFromRepository(repo)` - creates mocks based on an existing configured repository.

```c#
[Test]
public void Test()
{
  using (var mock = AutoMock.GetLoose())
  {
    // The AutoMock class will inject a mock IDependency
    // into the SystemUnderTest constructor
    var sut = mock.Create<SystemUnderTest>();
  }
}
```

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac) or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).
