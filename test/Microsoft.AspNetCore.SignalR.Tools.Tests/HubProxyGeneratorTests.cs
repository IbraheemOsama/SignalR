// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.SignalR.Redis;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tools.Tests
{
    public class HubProxyGeneratorTests
    {
        [Theory]
        [MemberData(nameof(AssembliesWithoutHubs))]
        public void GenerateProxiesDoesNotWriteAnythingIfNoHubsDiscovered(Assembly assembly)
        {
            var mockProxyWriter = new Mock<IHubProxyWriter>(MockBehavior.Strict);
            var proxyGenerator = new HubProxyGenerator(assembly.Location, mockProxyWriter.Object);
            proxyGenerator.GenerateHubProxies();
            mockProxyWriter.VerifyAll();
        }

        public static IEnumerable<object[]> AssembliesWithoutHubs
            => new []
            {
                // This assembly does not depend on Microsoft.AspNetCore.SignalR
                new object[] { typeof(object).Assembly },
                // Should not discover hub base types
                new object[] { typeof(Hub<>).Assembly },
                // This assembly depends on Microsoft.AspNetCore.SignalR but does not have any hubs
                new object[] { typeof(RedisOptions).Assembly }
            };

        [Fact]
        public void GenerateProxiesDiscoversHubs()
        {
            var mockProxyWriter = new Mock<IHubProxyWriter>();
            var proxyGenerator = new HubProxyGenerator(GetType().Assembly.Location, mockProxyWriter.Object);
            proxyGenerator.GenerateHubProxies();

            mockProxyWriter.Verify(p => p.WriteStartProxies(), Times.Once());
            mockProxyWriter.Verify(p => p.WriteStartProxy(nameof(TestHub), typeof(TestHub).Namespace), Times.Once());
            mockProxyWriter.Verify(p => p.WriteEndProxy(), Times.Once());
            mockProxyWriter.Verify(p => p.WriteEndProxies(), Times.Once());
        }
    }

    public class TestHub : Hub
    {

    }
}
