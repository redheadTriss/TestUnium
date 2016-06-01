﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Ninject;
using Ninject.Activation;
using Ninject.Activation.Caching;
using Ninject.Parameters;
using Ninject.Planning;
using Ninject.Planning.Bindings;
using TestUnium.Instantiation.Customization;
using TestUnium.Instantiation.Sessioning;
using TestUnium.Instantiation.Stepping.Modules;
using TestUnium.Instantiation.Stepping.Steps;

namespace TestUnium.Instantiation.Stepping
{
    [StepRunner(typeof(StepRunnerBase))]
    public class BasicStepModuleRegistrationStrategy : IStepModuleRegistrationStrategy
    {
        public void RegisterStepModule<TStepModule>(IKernel kernel, Boolean makeReusable = false) where TStepModule : IStepModule
        {
            RegisterStepModules(kernel, makeReusable, typeof(TStepModule));
        }

        public void RegisterStepModules(IKernel kernel, params Type[] moduleTypes)
        {
          RegisterStepModules(kernel, false, moduleTypes);
        }

        public void RegisterStepModules(IKernel kernel, Boolean makeReusable, params Type[] moduleTypes)
        {
            foreach (var moduleType in moduleTypes)
            {
                if (!typeof(IStepModule).IsAssignableFrom(moduleType))
                    throw new IncorrectInheritanceException(new[] { moduleType.Name }, new[] { nameof(IStepModule) });
                if (makeReusable || moduleType.GetCustomAttribute<ReusableAttribute>() != null)
                {
                    kernel.Bind<IStepModule>().To(moduleType).InSingletonScope();
                    return;
                }
                kernel.Bind<IStepModule>().To(moduleType);
            }
        }

        public void UnregisterStepModule<T>(IKernel kernel) where T : IStepModule
        {
            UnregisterStepModules(kernel, typeof(T));
        }

        public void UnregisterStepModules(IKernel kernel, params Type[] moduleTypes)
        {
            foreach (var moduleType in moduleTypes)
            {
                IBinding targetBinding = null;
                kernel.GetBindings(typeof(IStepModule))
                    .ToList()
                    .ForEach(
                        binding =>
                        {
                            if (binding.Target != BindingTarget.Type || binding.Target == BindingTarget.Self) return;
                            var req = kernel.CreateRequest(moduleType, metadata => true, new IParameter[0], true, false);
                            var cache = kernel.Components.Get<ICache>();
                            var planner = kernel.Components.Get<IPlanner>();
                            var pipeline = kernel.Components.Get<IPipeline>();
                            var provider = binding.GetProvider(new Context(kernel, req, binding, cache, planner, pipeline));
                            if (provider.Type == moduleType)
                            {
                                targetBinding = binding;
                            }
                        });
                if (targetBinding != null)
                {
                    kernel.RemoveBinding(targetBinding);
                }
            }
        }
    }
}
