using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    /// <summary>
    /// A model binder which uses a 'fake' field key (e.g. name attribute of html input) which is the path
    /// for the property plus '.ModelType' which is the name of the underlying type of the data
    /// allowing for creating of values of a different underlying type to the declared type of the
    /// property holding them.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypeDiscriminatingModelBinder<T> : IModelBinder where T : class
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var typeValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName + ".ModelType");
            if (typeValue.Values.Count == 0)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            var type = Type.GetType(
                (string)typeValue.ConvertTo(typeof(string)),
                true
                );
            if (!typeof(T).IsAssignableFrom(type))
            {
                throw new InvalidOperationException("Bad Type");
            }

            var model = Activator.CreateInstance(type);
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
