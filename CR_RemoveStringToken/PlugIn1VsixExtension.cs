using System.ComponentModel.Composition;
using DevExpress.CodeRush.Common;

namespace CR_RemoveStringToken
{
    [Export(typeof(IVsixPluginExtension))]
    public class CR_RemoveStringTokenExtension : IVsixPluginExtension { }
}