using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("skysepehru.Core.PropertyRegistry.Tests.Editor")]

// Lets NSubstitute (Castle DynamicProxy) generate mocks of the package's internal subsystem
// interfaces (IPropertyStore, IDirtyPropagator, ...) into its dynamic proxy assembly.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
