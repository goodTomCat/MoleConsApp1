//using System.ComponentModel.DataAnnotations.Schema;

namespace SharedMoleRes.Server.Surrogates
{
    public class AccessibilityInfoSur
    {
        public UserFormSurrogate UserForm { get; set; }
        public int UserFormId { get; set; }
        public int Id { get; set; }


        public bool IsPublicProfile { get; set; }
        public string ConstUsers { get; set; }
        public string TempUsers { get; set; }
        public int MaxConstUsers { get; set; }
        public int MaxTempUsers { get; set; }
    }
}
