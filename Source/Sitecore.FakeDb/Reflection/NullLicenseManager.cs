namespace Sitecore.FakeDb.Reflection
{
  using System.Reflection;
  using Sitecore.SecurityModel.License;

  public static class NullLicenseManager
  {
    public static void Activate()
    {
      MethodBase originalMethod = typeof(LicenseManager).GetMethod("DemandRuntime", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(bool), typeof(bool) }, new[] { new ParameterModifier(2) });
      if (originalMethod == null)
      {
        return;
      }

      MethodBase newMethod = typeof(NullLicenseManager).GetMethod("DemandRuntime", BindingFlags.Static | BindingFlags.Public);

      MethodUtil.ReplaceMethod(newMethod, originalMethod);
    }

    public static void DemandRuntime(bool acceptExpress, bool forceUpdate)
    {
    }
  }
}