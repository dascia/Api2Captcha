using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Api2Captcha
{
  public class CaptchaSolver
  {
    #region Fields

    private const int TIMEOUT = 120000;

    private string _apiKey;
    private bool _useProxy;
    private int _proxyPort;
    private string _proxyHost;
    private HttpClient _client;
    private ProxyType _proxyType;
    private int _requestInterval;
    private int _initialRequestDelay;
    private const string SERVER_REQUEST_URL = "http://2captcha.com/in.php?";
    private const string SERVER_ANSWER_URL = "http://2captcha.com/res.php?";

    #endregion

    #region Properties

    public string ApiKey
    {
      get { return _apiKey; }
      set { _apiKey = value; }
    }

    /// <summary>
    /// Waiting time before send the first request to check if the captcha
    /// is solved
    /// </summary>
    public int InitialRequestDelay
    {
      get { return _initialRequestDelay; }
      set { _initialRequestDelay = value; }
    }

    /// <summary>
    /// Waiting time to send the first request to 2Captcha servers
    /// asking for the caotcha solution
    /// </summary>
    public int RequestInterval
    {
      get { return _requestInterval; }
      set { _requestInterval = value; }
    }

    #endregion

    #region Events

    /// <summary>
    /// Fires the CaptchaSolved event
    /// </summary>
    /// <param name="response"></param>
    private void RaiseCaptchaSolved(CaptchaResponse response)
    {
      CaptchaSolved?.Invoke(this, response);
    }

    public event EventHandler<CaptchaResponse> CaptchaSolved;

    #endregion

    /// <summary>
    /// Ctor
    /// </summary>
    public CaptchaSolver(string apiKey)
    {
      _apiKey = apiKey;
      _useProxy = false;
      _initialRequestDelay = 10000;
      _requestInterval = 10000;
      _client = new HttpClient();
    }

    /// <summary>
    /// Proxy Ctor
    /// </summary>
    public CaptchaSolver(string apiKey, string proxyHost, int proxyPort, ProxyType proxyType = ProxyType.HTTP)
    {
      _apiKey = apiKey;
      _useProxy = true;
      _proxyHost = proxyHost;
      _proxyPort = proxyPort;
      _proxyType = proxyType;
      _initialRequestDelay = 10000;
      _requestInterval = 10000;
      WebProxy proxy = new WebProxy(proxyHost, proxyPort);
      HttpClientHandler handler = new HttpClientHandler();
      handler.Proxy = proxy;
      handler.UseProxy = true;
      _client = new HttpClient(handler);
    }

    /// <summary>
    /// Send the captcha data to the 2captcha servers
    /// </summary>
    /// <param name="googleKey">google site key</param>
    /// <param name="captchaURL">the captcha url</param>
    /// <param name="useCaptchaProxy">
    /// Indicate to 2Captcha to use the proxy server to solve the recaptcha.
    /// This will only works if a proxy has been defined in the object.
    /// </param>
    /// <returns></returns>
    private async Task<CaptchaResponse> SendCaptcha(string googleKey, string captchaURL, bool useCaptchaProxy = false)
    {
      //http://2captcha.com/in.php?key=YOUR_CAPTCHA_KEY&method=userrecaptcha&googlekey=%googlekey%&pageurl=%URL%
      string query = string.Empty;
      List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
      queryParams.Add(new KeyValuePair<string, string>("key", _apiKey));
      queryParams.Add(new KeyValuePair<string, string>("method", "userrecaptcha"));
      queryParams.Add(new KeyValuePair<string, string>("googlekey", googleKey));
      queryParams.Add(new KeyValuePair<string, string>("pageurl", captchaURL));
      if (useCaptchaProxy && _useProxy)
      {
        queryParams.Add(new KeyValuePair<string, string>("proxy",$"{_proxyHost}:{_proxyPort}"));
        queryParams.Add(new KeyValuePair<string, string>("proxytype", $"{_proxyType}"));
      }
      FormUrlEncodedContent content = new FormUrlEncodedContent(queryParams);
      query = SERVER_REQUEST_URL + await content.ReadAsStringAsync();
      CaptchaResponse captchaResponse = await ParseHttpResponse(await _client.GetAsync(query));
      return captchaResponse;
    }

    /// <summary>
    /// Check if the 2Captcha servers has a solution to the captcha submitted previously
    /// </summary>
    /// <param name="captchaId">Previously captcha submitted Id</param>
    /// <returns></returns>
    private async Task<CaptchaResponse> GetCaptchaSolution(string captchaId)
    {
      // http://2captcha.com/res.php?key=YOUR_CAPTCHA_KEY&action=get&id=Captcha_ID
      string query = string.Empty;
      List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
      queryParams.Add(new KeyValuePair<string, string>("key", _apiKey));
      queryParams.Add(new KeyValuePair<string, string>("action", "get"));
      queryParams.Add(new KeyValuePair<string, string>("id", captchaId));
      FormUrlEncodedContent content = new FormUrlEncodedContent(queryParams);
      query = SERVER_ANSWER_URL + await content.ReadAsStringAsync();
      CaptchaResponse captchaResponse = await ParseHttpResponse(await _client.GetAsync(query));
      return captchaResponse;
    }

    /// <summary>
    /// Solve the captcha entered
    /// </summary>
    /// <param name="googleKey"></param>
    /// <param name="captchaURL"></param>
    /// <param name="useCaptchaProxy"></param>
    /// <returns></returns>
    public async Task<CaptchaResponse> SolveCaptcha(string googleKey, string captchaURL, bool useCaptchaProxy = false)
    {
      CaptchaResponse response = await (SendCaptcha(googleKey, captchaURL, useCaptchaProxy));
      if (response.Response == Response.OK)
      {
        await Task.Delay(_initialRequestDelay);
        int timer = 0;
        do
        {
          await Task.Delay(_requestInterval);
          timer += _requestInterval;
          if (timer > TIMEOUT)
          {
            response.Response = Response.TIMEOUT;
            break;
          }
          response = await (GetCaptchaSolution(response.Value));
        }
        while (response.Response == Response.CAPCHA_NOT_READY);
      }
      RaiseCaptchaSolved(response);
      return response;
    }

    /// <summary>
    /// Parse the response from 2Captcha Servers to a CaptchaResponse
    /// </summary>
    /// <param name="httpResponse"></param>
    /// <returns></returns>
    private static async Task<CaptchaResponse> ParseHttpResponse(HttpResponseMessage httpResponse)
    {
      try
      {
        if (!httpResponse.IsSuccessStatusCode) 
          throw new CaptchaException($"{httpResponse.ReasonPhrase}");
        CaptchaResponse captchaResponse;
        string strResponse = await httpResponse.Content.ReadAsStringAsync();
        if (strResponse.Substring(0, 2) == "OK") captchaResponse = new CaptchaResponse(Response.OK, strResponse.Substring(3));
        else captchaResponse = new CaptchaResponse((Response)Enum.Parse(typeof(Response), strResponse), string.Empty);
        return captchaResponse;
      }
      catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is ArgumentException || ex is ArgumentNullException)
      {
        throw new CaptchaException("A problem ocurred parsing the response from 2Captcha", ex);
      }
    }
  }
}
