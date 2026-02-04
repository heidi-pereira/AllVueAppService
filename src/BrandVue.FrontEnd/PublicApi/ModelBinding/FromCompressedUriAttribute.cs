using System.IO;
using BrandVue.Controllers.Api;
using BrandVue.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.ModelBinding
{
    /// <summary>
    /// You must also use <see cref="TrickNSwag.CompressedGetOrPostAttribute" /> on the associated method
    /// </summary>
    public sealed class FromCompressedUriAttribute : TrickNSwag.FromBodyAttribute
    {
        public FromCompressedUriAttribute() : base(typeof(CompressedModelBinder))
        {
        }

        public class CompressedModelBinder : IModelBinder
        {
            private readonly ILogger<CompressedModelBinder> _logger;

            public CompressedModelBinder(ILogger<CompressedModelBinder> logger)
            {
                _logger = logger;
            }

            public async Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }

                string model = null;
                switch (bindingContext.HttpContext.Request.Method)
                {
                    case "GET":
                        {
                            string compressedModel =
                                bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

                            if (compressedModel != null)
                            {
                                model = new LZString().decompressFromBase64(compressedModel);
                            }

                            break;
                        }
                    case "POST":
                        using (var sr = new StreamReader(bindingContext.HttpContext.Request.Body))
                        {
                            model = await sr.ReadToEndAsync();
                        }
                        break;
                }

                if (model == null)
                {
                    bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                        "No compressed model received");
                    bindingContext.Result = ModelBindingResult.Failed();
                    return;
                }

                try
                {
                    var objectModel = JsonConvert.DeserializeObject(model, bindingContext.ModelType);
                    bindingContext.Result = ModelBindingResult.Success(objectModel);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to decompress {Model} to <{ModelType}>", model,
                        bindingContext.ModelType);
                    bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                        "Failed to decompress the model");
                }

            }
        }
    }
}