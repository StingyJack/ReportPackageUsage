namespace ReportPackageUsage
{
    public static class FsUtils
    {
       
        public static string GetFsSafeIdentifier(string searchCriteria)
        {
            return searchCriteria.Equals("*") ? "all" : searchCriteria;
        }
    }
}