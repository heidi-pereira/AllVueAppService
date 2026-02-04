using System;

namespace UserManagement.BackEnd.WebApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireInternalTokenAttribute : Attribute { }
}
