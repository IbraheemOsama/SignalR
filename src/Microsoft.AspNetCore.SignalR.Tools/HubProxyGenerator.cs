// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Mono.Cecil;
using System.IO;

namespace Microsoft.AspNetCore.SignalR.Tools
{
    public class HubProxyGenerator
    {
        private readonly TypeDefinition _hubTypeDefinition;

        private readonly ModuleDefinition _module;
        private readonly IHubProxyWriter _proxyWriter;

        public HubProxyGenerator(string path, IHubProxyWriter proxyWriter)
        {
            var parameters = new ReaderParameters
            {
                AssemblyResolver = new SameFolderAssemblyResolver(Path.GetDirectoryName(path))
            };

            _module = ModuleDefinition.ReadModule(path, parameters);
            _proxyWriter = proxyWriter;

            _hubTypeDefinition = _module.ImportReference(typeof(Hub<>)).Resolve();
        }

        public void GenerateHubProxies()
        {
            // _hubTypeDefinition will be null if the assembly containing the `Hub<>` type is not referenced
            if (_hubTypeDefinition == null || _hubTypeDefinition.Module.Assembly.FullName == _module.Assembly.FullName)
            {
                return;
            }

            var startedWriting = false;
            foreach (var type in _module.GetTypes().Where(IsHub))
            {
                if (!startedWriting)
                {
                    _proxyWriter.WriteStartProxies();
                    startedWriting = true;
                }

                WriteHubProxy(type);
            }

            if (startedWriting)
            {
                _proxyWriter.WriteEndProxies();
            }
        }

        public void WriteHubProxy(TypeDefinition type)
        {
            _proxyWriter.WriteStartProxy(type.Name, type.Namespace);
            WriteHubMethods(type);
            _proxyWriter.WriteEndProxy();
        }

        private void WriteHubMethods(TypeDefinition type)
        {
            foreach (var methodDefinition in type.Methods.Where(IsHubMethod))
            {
                WriteHubMethod(methodDefinition);
            }
        }

        private void WriteHubMethod(MethodDefinition methodDefinition)
        {
            _proxyWriter.WriteStartMethod(methodDefinition.Name, methodDefinition.ReturnType.Name,
                methodDefinition.ReturnType.Namespace);
            foreach (var parameterDefinition in methodDefinition.Parameters)
            {
                _proxyWriter.WriteParameter(parameterDefinition.Name, parameterDefinition.ParameterType.Name,
                    parameterDefinition.ParameterType.Namespace);
            }
            _proxyWriter.WriteEndMethod();
        }

        private bool IsHub(TypeDefinition type)
        {
            if (!type.IsPublic || type.IsAbstract || type.IsSpecialName)
            {
                return false;
            }

            while (type != null)
            {
                if (IsHubType(type))
                {
                    return true;
                }

                // BaseType can be null for interfaces
                type = type.BaseType?.Resolve();
            }

            return false;
        }

        private bool IsHubMethod(MethodDefinition method)
        {
            if (method.IsSpecialName)
            {
                return false;
            }

            return true;
        }

        private bool IsHubType(TypeDefinition type)
        {
            return type.Namespace == _hubTypeDefinition.Namespace && type.Name == _hubTypeDefinition.Name
                && type.Module.Assembly.FullName == _hubTypeDefinition.Module.Assembly.FullName;
        }

        private class SameFolderAssemblyResolver : IAssemblyResolver
        {
            private readonly string _directory;

            public SameFolderAssemblyResolver(string directory)
            {
                _directory = directory;
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                return Resolve(name, null);
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                if (parameters == null)
                {
                    parameters = new ReaderParameters();
                }
                if (parameters.AssemblyResolver == null)
                {
                    parameters.AssemblyResolver = this;
                }

                var nameWithoutExtension = Path.Combine(_directory, name.Name);

                if (File.Exists(nameWithoutExtension + ".dll"))
                {
                    // TODO: handle exe?
                    return ModuleDefinition.ReadModule(nameWithoutExtension + ".dll", parameters).Assembly;
                }

                return null;
            }

            public void Dispose()
            {
            }
        }
    }
}
