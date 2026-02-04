namespace BrandVue.Middleware
{
    public readonly struct RequestScope
    {
        public RequestScope(string productName, string subProduct, string organization,
            RequestResource resource)
        {
            Organization = organization;
            ProductName = productName.ToLowerInvariant();
            SubProduct = string.IsNullOrWhiteSpace(subProduct) ? null : subProduct.ToLowerInvariant();
            Resource = resource;
        }

        /// <summary>
        /// eatingout, barometer, charities
        /// </summary>
        public string ProductName { get; }

        public string SubProduct { get; }

        /// <summary>
        /// e.g. wagamamausa, carluccios, wgsn
        /// </summary>
        public string Organization { get; }

        /// <summary>
        /// Ui, API or Docs (Docs soon to be removed)
        /// </summary>
        public RequestResource Resource { get; }

        public override string ToString()
        {
            return $"{ProductName} - {SubProduct ?? "default"} - {Organization} - {Resource}";
        }
    }
}