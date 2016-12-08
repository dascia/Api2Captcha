using System;

namespace Api2Captcha
{

  [Serializable]
  public class CaptchaException : Exception
  {
    public CaptchaException() { }
    public CaptchaException(string message) : base(message) { }
    public CaptchaException(string message, Exception inner) : base(message, inner) { }
    protected CaptchaException(
    System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
  }
}
