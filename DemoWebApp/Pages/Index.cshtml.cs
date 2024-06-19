using DocDiffStd;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DemoWebApp.Pages;

public class IndexModel : PageModel
{
    public IEnumerable<Fragment> Changes = Array.Empty<Fragment>();

    public void OnGet()
    {
        var leftDoc = System.IO.File.ReadAllText("Examples/DeOfficiis.txt");
        var rightDoc = System.IO.File.ReadAllText("Examples/DeOfficiis_Altered.txt");

        Changes = new Differences(leftDoc, rightDoc, Differences.PerWord);
    }
}