namespace BrandVue
{
    public class ContentSecurityPolicy
    {
        public SecurityPolicyDefinition StyleSources { get; set; } = new SecurityPolicyDefinition();
        public SecurityPolicyDefinition FontSources { get; set; } = new SecurityPolicyDefinition();
        public SecurityPolicyDefinition FormActions { get; set; } = new SecurityPolicyDefinition();
        public SecurityPolicyDefinition FrameAncestors { get; set; } = new SecurityPolicyDefinition();
        public SecurityPolicyDefinition ImageSources { get; set; } = new SecurityPolicyDefinition();
        public SecurityPolicyDefinition ScriptSources { get; set; } = new SecurityPolicyDefinition();
    }

    public class SecurityPolicyDefinition
    {
        public string[] FromCode { get; set; } = new string[0];
        public string[] FromConfig { get; set; } = new string[0];

        public string[] GetJoinedList()
        {
            return FromCode.Concat(FromConfig).ToArray();
        }
    }
}