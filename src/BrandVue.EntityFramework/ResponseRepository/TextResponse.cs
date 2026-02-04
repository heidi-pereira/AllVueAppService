using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.ResponseRepository
{
    [Keyless]
    public class TextResponse
    {
        public long DataId { get; set; }
        public int ResponseId { get; set; }
        public string VarCode { get; set; }
        public int Ch1 { get; set; }
        public string Text { get; set; }
    }
}