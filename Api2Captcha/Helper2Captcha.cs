using System.Net.Http;
using System.Threading.Tasks;

namespace Api2Captcha
{
  public class Helper2Captcha
  {
    /// <summary>
    /// Gets the account balance
    /// </summary>
    /// <param name="apiKey"></param>
    /// <returns></returns>
    public static async Task<double> Balance(string apiKey)
    {
      string balanceUrl = $"http://2captcha.com/res.php?action=getbalance&key={apiKey}";
      using (HttpClient client = new HttpClient())
      {
        HttpResponseMessage response = await client.GetAsync(balanceUrl);
        string strResponse = await response.Content.ReadAsStringAsync();
        double balance;
        if (double.TryParse(strResponse, out balance) == false) return 0;
        return balance;
      }
    }
  }
}
