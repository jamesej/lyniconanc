using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Lynicon.Attributes;
using Lynicon.Utility;

namespace Lynicon.Models
{
    public class PolymorphicModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType.GetCustomAttribute<UsePolymorphicBindingAttribute>() != null)
            {
                return new PolymorphicModelBinder();
            }

            return null;
        }
    }
    public class PolymorphicModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var typeValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName + ".ModelType");
            if (typeValue == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            var type = Type.GetType(typeValue.FirstValue, true);
            await BindChangedType(bindingContext, type);
        }

        public async Task BindChangedType(ModelBindingContext bindingContext, Type type)
        {
            var servProv = bindingContext.ActionContext.HttpContext.RequestServices;
            var metadataProvider = servProv.GetRequiredService<IModelMetadataProvider>();
            var modelMetadata = metadataProvider.GetMetadataForType(type);
            bindingContext.ModelMetadata = modelMetadata;

            var modelBinderFactory = servProv.GetRequiredService<IModelBinderFactory>();
            var factoryContext = new ModelBinderFactoryContext()
            {
                Metadata = modelMetadata,
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = modelMetadata.BinderModelName,
                    BinderType = modelMetadata.BinderType,
                    BindingSource = modelMetadata.BindingSource,
                    PropertyFilterProvider = modelMetadata.PropertyFilterProvider,
                },

                // We're using the model metadata as the cache token here so that TryUpdateModelAsync calls
                // for the same model type can share a binder. This won't overlap with normal model binding
                // operations because they use the ParameterDescriptor for the token.
                CacheToken = modelMetadata
            };

            var underlyingBinder = modelBinderFactory.CreateBinder(factoryContext);

            await underlyingBinder.BindModelAsync(bindingContext);
        }

    }
}
