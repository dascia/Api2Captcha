namespace Api2Captcha
{
  public class CaptchaResponse
  {
    #region Fields

    private Response _response;
    private string _value;

    #endregion

    #region Properties
    
    /// <summary>
    /// 2Captcha server response value
    /// </summary>
    public string Value
    {
      get { return _value; }
    }

    /// <summary>
    /// 2Captcha server response type
    /// </summary>
    public Response Response
    {
      get { return _response; }
      internal set { _response = value; }
    }
    
    #endregion

    /// <summary>
    /// Builds an empty response object
    /// </summary>
    internal CaptchaResponse()
    {
      _value = string.Empty;
      _response = Response.NONE;
    }

    /// <summary>
    /// Build a response with the 2captcha response
    /// </summary>
    /// <param name="responseType"></param>
    /// <param name="responseValue"></param>
    internal CaptchaResponse(Response responseType, string responseValue)
    {
      _response = responseType;
      _value = responseValue;
    }
  }

  /// <summary>
  /// Response type received from the 2Captcha servers
  /// </summary>
  public enum Response {
    OK,
    ERROR_WRONG_USER_KEY,
    ERROR_KEY_DOES_NOT_EXIST,
    ERROR_ZERO_BALANCE,
    ERROR_NO_SLOT_AVAILABLE,
    ERROR_IP_NOT_ALLOWED,
    IP_BANNED,
    ERROR_CAPTCHAIMAGE_BLOCKED,
    CAPCHA_NOT_READY,
    ERROR_WRONG_ID_FORMAT,
    ERROR_CAPTCHA_UNSOLVABLE,
    ERROR_WRONG_CAPTCHA_ID,
    ERROR_BAD_DUPLICATES,
    REPORT_NOT_RECORDED,
    TIMEOUT,
    NONE
  }
}
